﻿using AutoMapper;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.Mappings
{
    public class RunningJobProfile : Profile
    {
        public RunningJobProfile()
        {
            CreateMap<RunningJob, GetCrawlResponseDto>()
                .ForMember(
                    dto => dto.State,
                    opt =>
                        opt.MapFrom(cj => CrawlState.Running));
        }
    }
}
