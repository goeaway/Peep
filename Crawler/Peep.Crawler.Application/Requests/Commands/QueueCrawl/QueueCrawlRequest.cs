﻿using MediatR;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<Either<Unit, ErrorResponseDTO>>
    {
        public IdentifiableCrawlJob Job { get; set; }
        
        public QueueCrawlRequest()
        {
        }
    }
}