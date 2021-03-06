﻿using MediatR;
using Peep.API.Models.DTOs;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Queries.GetCrawl
{
    public class GetCrawlRequest : IRequest<Either<GetCrawlResponseDto, HttpErrorResponse>>
    {
        public string CrawlId { get; set; }

        public GetCrawlRequest(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}