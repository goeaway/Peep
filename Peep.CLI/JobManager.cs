using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Peep.CLI
{
    public class JobManager
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly ILogger _logger;

        public string JobDirectory { get; }
        public string ResultsDirectory { get; }
        public string ErrorDirectory { get; }

        public JobManager(string processDirectory, ILogger logger)
        {
            if(processDirectory == null)
            {
                throw new ArgumentNullException(nameof(processDirectory));
            }

            var processDirectoryInfo = new DirectoryInfo(processDirectory);

            if(!processDirectoryInfo.Exists)
            {
                _logger.Information("Creating app directory {ProcessDirectory}", processDirectory);
                Directory.CreateDirectory(processDirectory);
            }

            _directoryInfo = new DirectoryInfo(processDirectory);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));


            JobDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.BucketDirectory);
            if(!Directory.Exists(JobDirectory))
            {
                _logger.Information("Creating job directory {JobDirectory}", JobDirectory);
                Directory.CreateDirectory(JobDirectory);
            }

            ResultsDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.ResultsDirectory);
            if (!Directory.Exists(ResultsDirectory))
            {
                _logger.Information("Creating results directory {ResultsDirectory}", ResultsDirectory);
                Directory.CreateDirectory(ResultsDirectory);
            }

            ErrorDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.ErrorDirectory);
            if (!Directory.Exists(ErrorDirectory))
            {
                _logger.Information("Creating error directory {ErrorDirectory}", ErrorDirectory);
                Directory.CreateDirectory(ErrorDirectory);
            }
        }

        public bool TryGetCrawlJob(out CrawlJob job, out FileInfo jobFileInfo)
        {
            job = null;
            jobFileInfo = null;

            var files = new DirectoryInfo(JobDirectory).GetFiles("*.json", SearchOption.TopDirectoryOnly);

            if(files.Any())
            {
                jobFileInfo = files.OrderBy(f => f.CreationTimeUtc).First();
                job = JsonConvert.DeserializeObject<CrawlJob>(File.ReadAllText(jobFileInfo.FullName));

                File.Delete(jobFileInfo.FullName);
            }
            // return job
            return job != null;
        }

        public void SaveResults(CrawlResult jobResult, FileInfo jobFileInfo)
        {
            if(jobResult == null)
            {
                throw new ArgumentException(nameof(jobResult));
            }

            if(jobFileInfo == null)
            {
                throw new ArgumentNullException(nameof(jobFileInfo));
            }

            File.WriteAllText(Path.Combine(ResultsDirectory, jobFileInfo.Name), JsonConvert.SerializeObject(jobResult, Formatting.Indented));
        }

        public void SaveError(CrawlJob job, FileInfo jobFileInfo)
        {
            if (job == null)
            {
                throw new ArgumentException(nameof(job));
            }

            if (jobFileInfo == null)
            {
                throw new ArgumentNullException(nameof(jobFileInfo));
            }

            File.WriteAllText(Path.Combine(ErrorDirectory, jobFileInfo.Name), JsonConvert.SerializeObject(job, Formatting.Indented));
        }
    }
}
