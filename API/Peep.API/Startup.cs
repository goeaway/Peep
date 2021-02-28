using System;
using System.Linq;
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
using Peep.API.Models.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Peep.API.Persistence;
using Microsoft.EntityFrameworkCore;
using Peep.API.Models.Entities;
using Peep.API.Application.Services;
using Peep.Core.API.Exceptions;
using Peep.Core.API.Behaviours;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Queuing;
using Peep.Core.Infrastructure.Filtering;
using MassTransit.AspNetCoreIntegration;
using MassTransit;
using Peep.API.Application.Providers;
using Peep.Core.API;
using Peep.Core.API.Providers;
using Peep.Data;

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

            services.AddMassTransit(options => 
            {
                options.UsingRabbitMq((ctx, cfg) => 
                {
                    cfg.Host(messagingOptions.Hostname, "/", h =>
                    {
                        h.Username(messagingOptions.Username);
                        h.Password(messagingOptions.Password);
                    });
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
                var nowProvider = scope.ServiceProvider.GetRequiredService<INowProvider>();
                var context = scope.ServiceProvider.GetRequiredService<PeepApiContext>();
                var crawlCancellationTokenProvider = scope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var filterManager = scope.ServiceProvider.GetRequiredService<ICrawlFilterManager>();
                var queueManager = scope.ServiceProvider.GetRequiredService<ICrawlQueueManager>();
                var dataManager = scope.ServiceProvider.GetRequiredService<ICrawlDataSinkManager<ExtractedData>>();
                var errorManager = scope.ServiceProvider.GetRequiredService<ICrawlDataSinkManager<CrawlErrors>>();
                var queuedJobProvider = scope.ServiceProvider.GetRequiredService<IQueuedJobProvider>();
                
                return new CrawlerManagerService(
                    context, 
                    logger, 
                    nowProvider, 
                    crawlCancellationTokenProvider, 
                    publishEndpoint,
                    filterManager, 
                    queueManager, 
                    dataManager, 
                    errorManager,
                    queuedJobProvider);
            });

            services.AddDbContext<PeepApiContext>(
                options => options.UseInMemoryDatabase("PeepApiDatabase"));

            services.AddTransient<ICrawlDataSinkManager<ExtractedData>, CrawlDataSinkManager>();
            services.AddTransient<ICrawlDataSinkManager<CrawlErrors>, CrawlErrorSinkManager>();
            services.AddTransient<ICrawlQueueManager, CrawlQueueManager>();
            services.AddTransient<ICrawlFilterManager, CrawlFilterManager>();
            services.AddTransient<IQueuedJobProvider, QueuedJobProvider>();
            
            services.AddLogger();
            services.AddCrawlCancellationTokenProvider();
            services.AddNowProvider();
            services.AddAutoMapper(typeof(QueuedJob));
            services.AddRedis(cachingOptions);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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

                var logger = app.ApplicationServices.GetService<ILogger>();

                var errorResponse = new ErrorResponseDTO
                {
                    Message = exception.Message
                };

                if(exception is RequestValidationFailedException)
                {
                    ctx.Response.StatusCode = 400;
                    errorResponse.Message = "Validation error";
                    errorResponse.Errors = (exception as RequestValidationFailedException).Failures.Select(f => f.ErrorMessage);
                    logger.Error("Validation error occurred: {Errors}", string.Join(", ", errorResponse.Errors));
                }
                else if (exception is RequestFailedException)
                {
                    var rfException = exception as RequestFailedException;
                    ctx.Response.StatusCode = (int)rfException.StatusCode;
                    errorResponse.Message = rfException.Message;
                    logger.Error("Request failed ({StatusCode}): {Error}", rfException.StatusCode, rfException.Message);
                }
                else
                {
                    logger.Error(exception, "Error occurred when processing request {uri}", uri);
                }

                await ctx.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
            });
        }
    }
}
