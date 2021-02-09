using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Peep.API.Application.Behaviours;
using Peep.API.Application.Commands.QueueCrawl;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Peep.API.Application.Exceptions;
using Peep.API.Application;
using Peep.API.Persistence;
using Microsoft.EntityFrameworkCore;

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
            services
                .AddControllers()
                .AddFluentValidation(fv => {
                    fv.RegisterValidatorsFromAssemblyContaining<QueueCrawlValidator>();
                });

            services.AddMediatR(Assembly.GetAssembly(typeof(QueueCrawlRequest)));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            services.AddHostedService<HostedCrawlerService>();

            services.AddDbContext<PeepApiContext>(
                options => options.UseInMemoryDatabase("PeepApiDatabase"));

            services.AddCrawler();
            services.AddLogger();
            //services.AddStackExchangeRedisCache(options =>
            //{
            //    options.
            //});
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
                logger.Error(exception, "Error occurred when processing request {uri}", uri);

                var errorResponse = new ErrorResponseDTO
                {
                    Message = exception.Message
                };

                if(exception is RequestValidationFailedException)
                {
                    ctx.Response.StatusCode = 400;
                    errorResponse.Message = "Validation failed";
                    errorResponse.Errors = (exception as RequestValidationFailedException).Failures.Select(f => f.ErrorMessage);
                }

                await ctx.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
            });
        }
    }
}
