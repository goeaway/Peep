using AutoMapper;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peep.Core.API.Providers;

namespace Peep.API.Models.Mappings
{
    public class JobProfile : Profile
    {
        public JobProfile(INowProvider nowProvider)
        {
            CreateMap<Job, GetCrawlResponseDto>()
                .ForMember(
                    dto => dto.Data,
                    opt =>
                        opt.MapFrom(cj =>
                            cj.JobData
                                .GroupBy(g => g.Source)
                                .ToDictionary(
                                    k => new Uri(k.Key),
                                    v => v.Select(value => value.Value))
                        ))
                .ForMember(
                    dto => dto.Errors,
                    opt => 
                        opt.MapFrom(j => j.JobErrors.Select(je => je.Message)))
                .ForMember(
                    dto => dto.DataCount,
                    opt => 
                        opt.MapFrom(j =>
                            j.JobData.Count
                        ))
                .ForMember(
                    dto => dto.Duration,
                    opt => 
                        opt.MapFrom(j => GetDuration(j, nowProvider)));
        }

        private static TimeSpan GetDuration(Job job, INowProvider nowProvider)
        {
            // if job is still queued, just return 0 timespan
            // if job has completed
            return job.State == JobState.Queued
                ? TimeSpan.Zero
                : (job.DateCompleted ?? nowProvider.Now) - job.DateStarted.GetValueOrDefault();  
        }
    }
}
