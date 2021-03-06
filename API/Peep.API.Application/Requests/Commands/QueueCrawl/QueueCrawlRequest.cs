﻿using MediatR;
using Peep.API.Models.DTOs;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<Either<QueueCrawlResponseDto, HttpErrorResponse>>
    {
        public StoppableCrawlJob Job { get; set; }

        public QueueCrawlRequest(StoppableCrawlJob job)
        {
            Job = job;
        }
    }
}