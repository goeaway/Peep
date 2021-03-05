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
using MassTransit;
using Peep.Core.API;
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
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = scope.ServiceProvider.GetRequiredService<PeepApiContext>();
                
                return new CrawlerManagerService(
                    logger,
                    mediator,
                    context);
            });

            services.AddDbContext<PeepApiContext>(
                options => options.UseMySql(
                    Configuration.GetConnectionString("db"),
                    x => x.MigrationsAssembly("Peep.API.Persistence")));

            services.AddTransient<ICrawlDataSinkManager<ExtractedData>, CrawlDataSinkManager>();
            services.AddTransient<ICrawlDataSinkManager<CrawlErrors>, CrawlErrorSinkManager>();
            services.AddTransient<ICrawlQueueManager, CrawlQueueManager>();
            services.AddTransient<ICrawlFilterManager, CrawlFilterManager>();
            
            services.AddLogger(Configuration);
            
            services.AddCrawlCancellationTokenProvider();
            services.AddNowProvider();
            services.AddAutoMapper(typeof(QueuedJob));
            services.AddRedis(cachingOptions);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, PeepApiContext context)
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

                var logger = app.ApplicationServices.GetRequiredService<ILogger>();

                var errorResponse = new ErrorResponseDTO
                {
                    Message = exception.Message
                };

                switch (exception)
                {
                    case RequestValidationFailedException failedException:
                        ctx.Response.StatusCode = 400;
                        errorResponse.Message = "Validation error";
                        errorResponse.Errors = failedException.Failures.Select(f => f.ErrorMessage);
                        logger.Error("Validation error occurred: {Errors}", string.Join(", ", errorResponse.Errors));
                        break;
                    case RequestFailedException rfException:
                        ctx.Response.StatusCode = (int)rfException.StatusCode;
                        errorResponse.Message = rfException.Message;
                        logger.Error("Request failed ({StatusCode}): {Error}", rfException.StatusCode, rfException.Message);
                        break;
                    default:
                        logger.Error(exception, "Error occurred when processing request {uri}", uri);
                        break;
                }

                await ctx.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
            });
        }
    }
}
