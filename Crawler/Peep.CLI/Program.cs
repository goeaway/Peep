using CommandLine;
using Newtonsoft.Json;
using Peep.Core;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.CLI
{
    class Program
    {
        static void Main(string[] args)
        {

            new Program(args);
        }

        Program(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async o =>
                {
                    var loggerConfig = new LoggerConfiguration()
                        .WriteTo
                            .Console()
                        .WriteTo
                            .File(
                                Path.Combine(o.ProcessDirectory, Consts.Paths.Logging.Directory, "log.txt"),
                                rollingInterval: RollingInterval.Day);

                    var logger = loggerConfig.CreateLogger();

                    var cancellationTokenSource = new CancellationTokenSource();
                    Task<CrawlResult> currentTask = null;

                    AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
                    {
                        if (cancellationTokenSource != null && currentTask != null)
                        {
                            cancellationTokenSource.Cancel();
                            logger.Information("Stopping crawler...");

                            currentTask.ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                    };

                    try
                    {
                        logger.Information("Initialising...");
                        var crawler = new Crawler();
                        // setup manager
                        var manager = new JobManager(o.ProcessDirectory, logger);

                        logger.Information("Waiting for jobs in {JobDirectory}", manager.JobDirectory);
                        var notifiedWaiting = true;

                        // create while loop, check for a job from dir monitor
                        while(!cancellationTokenSource.IsCancellationRequested)
                        {
                            if(manager.TryGetCrawlJob(out var job, out var jobFileInfo))
                            {
                                var name = Path.GetFileNameWithoutExtension(jobFileInfo.FullName);
                                logger.Information("Running job {Name}", name);

                                try
                                {
                                    var updateInterval = o.ProgressUpdateIntervalSeconds < -1 ? 0 : o.ProgressUpdateIntervalSeconds;
                                    var updateAction = o.ProgressUpdateIntervalSeconds < 1 ? default(Action<CrawlProgress>) : progress =>
                                            logger.Information(
                                                "Total Crawled: {CrawlCount}\tData Collected: {DataCount}",
                                                progress.CrawlCount, progress.DataCount);

                                    currentTask = crawler.Crawl(job, 
                                        TimeSpan.FromSeconds(updateInterval),
                                        updateAction,
                                        cancellationTokenSource.Token);

                                    var result = await currentTask;
                                    // put the result in a json file in the results directory
                                    manager.SaveResults(result, jobFileInfo);
                                    // log completion and add 
                                    logger.Information("Crawl complete, results saved");
                                    currentTask = null;
                                }
                                catch (Exception e)
                                {
                                    // log error and save job file to error directory
                                    logger.Error(e, "Error ocurred during crawl for job {Name}", name);
                                    manager.SaveError(job, jobFileInfo);
                                }

                                notifiedWaiting = false;
                            }
                            else
                            {
                                if (!notifiedWaiting)
                                {
                                    logger.Information("Waiting for jobs");
                                    notifiedWaiting = true;
                                }

                                await Task.Delay(10000);
                            }
                        }
                    }
                    catch (Exception e) 
                    {
                        logger.Error(e, "Error occurred when setting up crawler");
                    }
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
