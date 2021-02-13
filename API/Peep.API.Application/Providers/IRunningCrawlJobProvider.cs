using Peep.API.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.API.Application.Providers
{
    public interface IRunningCrawlJobProvider
    {
        Task<RunningJob> GetRunningJob(string id);
        Task SaveJob(RunningJob job);
        Task RemoveJob(string id);
    }
}
