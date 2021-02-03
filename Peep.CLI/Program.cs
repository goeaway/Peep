using CommandLine;
using Newtonsoft.Json;
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

        private readonly CancellationTokenSource _cancellationTokenSource;

        Program(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

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

                    try
                    {
                        logger.Information("Initialising...");
                        var crawler = new Crawler();
                        // setup manager
                        var manager = new JobManager(o.ProcessDirectory);

                        logger.Information("Waiting for jobs in {ProcessDirectory}", o.ProcessDirectory);
                        var notifiedWaiting = true;

                        // create while loop, check for a job from dir monitor
                        while(!_cancellationTokenSource.IsCancellationRequested)
                        {
                            if(manager.TryGetCrawlJob(out var job, out var jobFileInfo))
                            {
                                var name = Path.GetFileNameWithoutExtension(jobFileInfo.FullName);
                                logger.Information("Running job {Name}", name);

                                try
                                {
                                    var result = await crawler.Crawl(job, 
                                        TimeSpan.FromMinutes(1),
                                        progress => 
                                            logger.Information(
                                                "Total Crawled: {CrawlCount}\tData Collected: {DataCount}", 
                                                progress.CrawlCount, progress.DataCount),
                                        _cancellationTokenSource.Token);
                                    // put the result in a json file in the results directory
                                    manager.SaveResults(result, jobFileInfo);
                                    // log completion and add 
                                    logger.Information("Crawl complete, results saved");
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

                                await Task.Delay(15000);
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

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if(_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            // todo actually wait for the crawler to finish up...
        }
    }
}
