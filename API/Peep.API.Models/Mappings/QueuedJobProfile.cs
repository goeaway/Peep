using AutoMapper;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.Mappings
{
    public class QueuedJobProfile : Profile
    {
        public QueuedJobProfile()
        {
            AllowNullCollections = false;

            CreateMap<QueuedJob, GetCrawlResponseDto>()
                .ForMember(dto => dto.Data, opt => opt.MapFrom(qj => new Dictionary<Uri, IEnumerable<string>>()));
        }
    }
}
