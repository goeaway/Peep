using MediatR;
using Peep.API.Application.Exceptions;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Queries.GetCrawl
{
    public class GetCrawlHandler : IRequestHandler<GetCrawlRequest, GetCrawlResponseDTO>
    {
        private readonly PeepApiContext _context;

        public GetCrawlHandler(PeepApiContext context)
        {
            _context = context;
        }

        public Task<GetCrawlResponseDTO> Handle(GetCrawlRequest request, CancellationToken cancellationToken)
        {
            // check queued table


            // check hosted service for running

            // check completed table

            // check errored table

            // throw that it's not found
            throw new RequestFailedException("Crawl job not found", System.Net.HttpStatusCode.NotFound);
        }
    }
}
