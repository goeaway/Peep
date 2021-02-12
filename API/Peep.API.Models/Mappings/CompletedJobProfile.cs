using AutoMapper;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.Mappings
{
    public class CompletedJobProfile : Profile
    {
        public CompletedJobProfile()
        {
            CreateMap<CompletedJob, GetCrawlResponseDTO>()
                .ForMember(
                    dto => dto.Data, 
                    opt => 
                        opt.MapFrom(cj => 
                            JsonConvert
                                .DeserializeObject<Dictionary<Uri, IEnumerable<string>>>(cj.DataJson)))
                .ForMember(
                    dto => dto.State,
                    opt => 
                        opt.MapFrom(cj => CrawlState.Complete));
        }
    }
}
