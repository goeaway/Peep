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
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter;
using Paramore.Brighter.MessagingGateway.RMQ;
using Peep.API.Application.Requests.Commands.QueueCrawl;

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

            services.AddBrighter(options => {
                var messageStore = new InMemoryMessageStore();
                var rmq = new RmqMessageProducer(new RmqMessagingGatewayConnection
                {
                    AmpqUri = new AmqpUriSpecification(
                        new Uri($"amqp://guest:guest@{messagingOptions.Hostname}:{messagingOptions.Port}")),
                    Exchange = new Exchange("crawler")
                });

                options.BrighterMessaging = new BrighterMessaging(messageStore, rmq);
            });


            services
                .AddControllers()
                .AddNewtonsoftJson()
                .AddFluentValidation(fv => {
                    fv.RegisterValidatorsFromAssemblyContaining<QueueCrawlValidator>();
                });

            services.AddMediatR(Assembly.GetAssembly(typeof(QueueCrawlRequest)));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            services.AddHostedService<CrawlerManagerService>(provider => new CrawlerManagerService(provider));

            services.AddDbContext<PeepApiContext>(
                options => options.UseInMemoryDatabase("PeepApiDatabase"));

            services.AddLogger();
            services.AddCrawlCancellationTokenProvider();
            services.AddNowProvider();
            services.AddAutoMapper(typeof(QueuedJob));
            services.AddDistributedMemoryCache();
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
