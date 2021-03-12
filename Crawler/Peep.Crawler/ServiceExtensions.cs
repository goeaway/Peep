using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using NpgsqlTypes;
using Peep.Crawler.Application.Options;
using Peep.Factories;
using Serilog.Sinks.PostgreSQL;

namespace Peep.Crawler
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services, IConfiguration configuration)
        {
            const string CONSOLE_OUTPUT_TEMPLATE = 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {JobId} {Message:lj}{NewLine}{Exception}";

            var columnWriters = new Dictionary<string, ColumnWriterBase>
            {
                {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
                {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
                {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
                {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
                {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
                {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
                {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
                {"context", new SinglePropertyColumnWriter("Context", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
            };
            
            var loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Context", Environment.MachineName)
                .WriteTo.Console(outputTemplate: CONSOLE_OUTPUT_TEMPLATE)
                .WriteTo.PostgreSQL(
                    configuration.GetConnectionString("db"),
                    configuration.GetSection("Logging")["TableName"],
                    columnWriters,
                    needAutoCreateTable: true);

            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());
            return services;
        }

        public static IServiceCollection AddCrawler(this IServiceCollection services, CrawlConfigOptions options)
        {
            services.AddTransient<ICrawler, DistributedCrawler>(provider =>
            {
                var crawlerOptions = new CrawlerOptions
                {
                    BrowserAdapterFactory = new PuppeteerSharpBrowserAdapterFactory(options.BrowserPagesCount)
                };
                return new DistributedCrawler(crawlerOptions);
            });
            return services;
        }

        public static IServiceCollection AddCrawlerOptions(
            this IServiceCollection services,
            IConfiguration configuration,
            out CrawlConfigOptions options)
        {
            options = new CrawlConfigOptions();
            configuration.GetSection(CrawlConfigOptions.Key)
                .Bind(options);
            return services.AddSingleton(options);
        }
    }
}
