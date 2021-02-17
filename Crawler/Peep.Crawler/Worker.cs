using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.Crawler.Options;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace Peep.Crawler
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICrawler _crawler;
        private readonly CrawlConfigOptions _crawlOptions;
        private readonly MessagingOptions _messagingOptions;
        private readonly ICrawlFilter _filter;
        private readonly ICrawlQueue _queue;

        private const string CRAWL_EXCHANGE = "crawlExchange";
        private const string QUEUE_ROUTING_KEY = "queue";
        private const string CANCEL_ROUTING_KEY = "cancel";

        public Worker(ILogger logger,
            ICrawler crawler,
            CrawlConfigOptions crawlOptions,
            MessagingOptions messagingOptions,
            ICrawlFilter filter,
            ICrawlQueue queue)
        {
            _logger = logger;
            _crawler = crawler;
            _crawlOptions = crawlOptions;
            _filter = filter;
            _queue = queue;
            _messagingOptions = messagingOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _messagingOptions.Hostname };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(CRAWL_EXCHANGE, ExchangeType.Direct);

            var queue = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue, CRAWL_EXCHANGE, QUEUE_ROUTING_KEY);
            channel.QueueBind(queue, CRAWL_EXCHANGE, CANCEL_ROUTING_KEY);

            var consumer = new EventingBasicConsumer(channel);

            var jobQueue = new Queue<IdentifiableCrawlJob>();
            CancellationTokenSource cancellationTokenSource = null;

            consumer.Received += (model, ea) =>
            {
                // get message out
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                switch(ea.RoutingKey)
                {
                    // if it's a job, queue it up
                    case QUEUE_ROUTING_KEY:
                        jobQueue.Enqueue(JsonConvert.DeserializeObject<IdentifiableCrawlJob>(message));
                        break;
                    // if it's a cancellation, and our source exists, cancel it
                    case CANCEL_ROUTING_KEY:
                        cancellationTokenSource?.Cancel();
                        break;
                }
            };

            channel.BasicConsume(queue, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, start crawling
                while (jobQueue.TryDequeue(out var job)) {
                    cancellationTokenSource = 
                        CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    await RunJob(job, cancellationTokenSource.Token);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunJob(IdentifiableCrawlJob job, CancellationToken cancellationToken)
        {
            try
            {
                var channelReader = _crawler.Crawl(
                    job,
                    TimeSpan.FromMilliseconds(_crawlOptions.ProgressUpdateMilliseconds),
                    _filter,
                    _queue,
                    cancellationToken);

                try
                {
                    // async iterate over channel's results
                    // update the running jobs running totals of the crawl result
                    await foreach (var result in channelReader.ReadAllAsync(cancellationToken))
                    {
                        // send data message back to manager
                    }
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                {
                    // occurs when cancellation occurs, so we can ignore and treat as normal
                }

                // send message to say we're complete
            }
            catch (CrawlerRunException e)
            {
                // send message to say we're complete but error occurred, provide the data in the exception
            }
            catch (Exception e)
            {
                // send message to say we're complete but error occurred
            }
        }
    }
}
