using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep
{
    public interface ICrawler
    {
        Task<CrawlResult> Crawl(Uri seeds, CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(Uri seeds, CrawlOptions options, CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CrawlOptions options, CancellationToken cancellationToken);
    }
}
