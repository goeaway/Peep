using AutoMapper;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peep.API.Models.Mappings
{
    public class CompletedJobProfile : Profile
    {
        public CompletedJobProfile()
        {
            CreateMap<CompletedJob, GetCrawlResponseDto>()
                .ForMember(
                    dto => dto.Data,
                    opt =>
                        opt.MapFrom(cj =>
                            cj.CompletedJobData
                              .GroupBy(g => g.Source)
                              .ToDictionary(
                                  k => new Uri(k.Key), 
                                  v => v.Select(value => value.Value))
                            ))
                .ForMember(
                    dto => dto.State,
                    opt =>
                        opt.MapFrom(cj => CrawlState.Complete));
        }
    }
}
