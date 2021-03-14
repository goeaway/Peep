using System.Reflection;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Diagnostics;
using Peep.API.Persistence;
using Microsoft.EntityFrameworkCore;
using Peep.API.Application.Services;
using Peep.Core.API.Behaviours;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Queuing;
using Peep.Core.Infrastructure.Filtering;
using MassTransit;
using Peep.API.Messages;
using Peep.API.Models.Mappings;
using Peep.Core.API;
using Peep.Core.API.Providers;

namespace Peep.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMessagingOptions(Configuration, out var messagingOptions);
            services.AddCachingOptions(Configuration, out var cachingOptions);
            services.AddMonitoringOptions(Configuration, out var monitoringOptions);

            services.AddMassTransitHostedService();
            services.AddMassTransit(options =>
            {
                options.AddConsumer<CrawlerUpConsumer>();
                options.AddConsumer<CrawlerDownConsumer>();
                
                options.AddConsumer<CrawlerJoinedConsumer>();
                options.AddConsumer<CrawlerHeartbeatConsumer>();
                options.AddConsumer<CrawlerLeftConsumer>();
                
                options.AddConsumer<CrawlDataPushedConsumer>();
                options.AddConsumer<CrawlErrorPushedConsumer>();

                options.UsingRabbitMq((ctx, cfg) => 
                {
                    cfg.Host(messagingOptions.Hostname, "/", h =>
                    {
                        h.Username(messagingOptions.Username);
                        h.Password(messagingOptions.Password);
                    });
                    
                    cfg.ReceiveEndpoint(
                        "crawler-up",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlerUpConsumer(
                                    scope.ServiceProvider.GetRequiredService<IMediator>(),
                                    scope.ServiceProvider.GetRequiredService<ILogger>()));
                        }
                    );
                    
                    cfg.ReceiveEndpoint(
                        "crawler-down",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlerDownConsumer(
                                    scope.ServiceProvider.GetRequiredService<IMediator>(),
                                    scope.ServiceProvider.GetRequiredService<ILogger>()));
                        }
                    );
                    
                    cfg.ReceiveEndpoint(
                        "crawler-joined", 
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlerJoinedConsumer(
                                    scope.ServiceProvider.GetRequiredService<IMediator>(),
                                    scope.ServiceProvider.GetRequiredService<ILogger>())
                            );
                        });
                    
                    cfg.ReceiveEndpoint(
                        "crawler-heartbeat",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlerHeartbeatConsumer(
                                    scope.ServiceProvider.GetRequiredService<IMediator>())
                                );
                        });
                    
                    cfg.ReceiveEndpoint(
                        "crawler-left",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlerLeftConsumer(
                                    scope.ServiceProvider.GetRequiredService<IMediator>(),
                                    scope.ServiceProvider.GetRequiredService<ILogger>())
                            );
                        });
                    
                    cfg.ReceiveEndpoint(
                        "crawl-data-pushed",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlDataPushedConsumer(scope.ServiceProvider.GetRequiredService<IMediator>())
                            );
                        }
                    );
                    
                    cfg.ReceiveEndpoint(
                        "crawl-error-pushed",
                        e =>
                        {
                            var scope = ctx.CreateScope();
                            e.Consumer(
                                () => new CrawlErrorPushedConsumer(scope.ServiceProvider.GetRequiredService<IMediator>())
                            );
                        }
                    );
                });
            });

            services
                .AddControllers()
                .AddNewtonsoftJson()
                .AddFluentValidation(fv => {
                    fv.RegisterValidatorsFromAssemblyContaining<QueueCrawlValidator>();
                });

            services.AddMediatR(Assembly.GetAssembly(typeof(QueueCrawlRequest)));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            services.AddHostedService(provider =>
            {
                var scope = provider.CreateScope();
                
                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = scope.ServiceProvider.GetRequiredService<PeepApiContext>();
                var nowProvider = scope.ServiceProvider.GetRequiredService<INowProvider>();
                
                return new CrawlRunnerService(
                    logger,
                    mediator,
                    context,
                    nowProvider);
            });

            services.AddHostedService(provider =>
            {
                var scope = provider.CreateScope();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                return new CrawlerMonitorService(mediator, logger);
            });

            services.AddHostedService(provider =>
            {
                var scope = provider.CreateScope();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                return new JobMonitorService(mediator, logger);
            });
            
            services.AddDbContext<PeepApiContext>(
                options => options.UseNpgsql(
                    Configuration.GetConnectionString("db"),
                    x => x.MigrationsAssembly("Peep.API.Persistence")));

            services.AddTransient<ICrawlQueueManager, CrawlQueueManager>();
            services.AddTransient<ICrawlFilterManager, CrawlFilterManager>();
            
            services.AddLogger(Configuration);
            
            services.AddCrawlCancellationTokenProvider();
            services.AddNowProvider();
            services.AddAutoMapper((ctx, cfg) =>
            {
                cfg.AddProfile(new JobProfile(ctx.GetRequiredService<INowProvider>()));
            }, Assembly.GetExecutingAssembly());
            services.AddRedis(cachingOptions);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();
            
            app.UseHttpsRedirection();
            app.UseExceptionHandler(ExceptionHandler);

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ExceptionHandler(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                var exHandlerPathFeature = ctx.Features.Get<IExceptionHandlerFeature>();
                var exception = exHandlerPathFeature.Error;
                var uri = ctx.Request.Path;

                var logger = app.ApplicationServices.GetRequiredService<ILogger>();

                var errorResponse = new HttpErrorResponse
                {
                    Message = exception.Message
                };

                logger.Error(exception, "Error occurred when processing request {uri}", uri);

                await ctx.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
            });
        }
    }
}
