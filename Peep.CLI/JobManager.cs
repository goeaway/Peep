using Newtonsoft.Json;
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

        public JobManager(string processDirectory)
        {
            if(processDirectory == null)
            {
                throw new ArgumentNullException(nameof(processDirectory));
            }

            var processDirectoryInfo = new DirectoryInfo(processDirectory);

            if(!processDirectoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory {processDirectory} not found");
            }

            _directoryInfo = new DirectoryInfo(processDirectory);
        }

        public bool TryGetCrawlJob(out CrawlJob job, out FileInfo jobFileInfo)
        {
            job = null;
            jobFileInfo = null;

            // once found, extract json from file and delete it
            var jobDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.BucketDirectory);

            if(!Directory.Exists(jobDirectory))
            {
                Directory.CreateDirectory(jobDirectory);
            }

            var files = new DirectoryInfo(jobDirectory).GetFiles("*.json", SearchOption.TopDirectoryOnly);

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

            var resultsDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.ResultsDirectory);

            if(!Directory.Exists(resultsDirectory))
            {
                Directory.CreateDirectory(resultsDirectory);
            }

            File.WriteAllText(Path.Combine(resultsDirectory, jobFileInfo.Name), JsonConvert.SerializeObject(jobResult, Formatting.Indented));
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

            var errorDirectory = Path.Combine(_directoryInfo.FullName, Consts.Paths.Jobs.ErrorDirectory);

            if (!Directory.Exists(errorDirectory))
            {
                Directory.CreateDirectory(errorDirectory);
            }

            File.WriteAllText(Path.Combine(errorDirectory, jobFileInfo.Name), JsonConvert.SerializeObject(job, Formatting.Indented));
        }
    }
}
