using CommandLine;
using System;
using System.Threading;

namespace Peep.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program(args);
        }

        private readonly CancellationTokenSource _cancellationTokenSource;

        Program(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async o =>
                {
                    var crawler = new Crawler();

                    // setup monitor
                    await crawler.Crawl(null, _cancellationTokenSource.Token);

                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if(_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }


        }
    }
}
