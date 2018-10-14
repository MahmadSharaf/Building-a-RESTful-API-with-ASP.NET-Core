using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Diagnostics;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {   // Specifying the output format
                setupAction.ReturnHttpNotAcceptable = true; // No default output format if the requested format send is not supported a 406 error is returned
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());
            });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, 
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            //There are a lot of log level: 
            //trace: is the most detailed log messages,
            //debug: is used for messages that have short-term usefulness during development, it contain information about debugging, but it doesnot have any longterm value
            //information: is used to track the general flow of an application. These logs do have some long term value. The warning level should be used for abnormal or unexpected events int the application flow. 
            //             So it may include errors or other conditions that do not cause the application to stop, but do need to be investigated further in the future.
            //Error: should be logged when the current flow of the application must stop due to failure, such as an exception that should be handeled or recovered from.
            //Critical: This should be reserved for unrecoverable apllication or system crashes, or catastrophic failure that requires immediate attention. 
            loggerFactory.AddDebug(LogLevel.Information); //Keeping the default log level "information

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {   
                app.UseExceptionHandler(appBuilder =>   // This creates a global message for a specific status code
                {
                    appBuilder.Run(async context => // This write the request response pipeline
                    {   //from context.Features we can get collection of HTTP features provided by the server and middleware available on this request
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature != null) //if the exception is found we can look at the error property to get the actual exception 
                        {//Now we go the exception, all we need to do now is to log it, and to log something, we need a logger instance. so we will inject it through the loggerFactory
                            var logger = loggerFactory.CreateLogger("Global exception logger");
                            logger.LogError(500, //ID
                                exceptionHandlerFeature.Error,//Exception
                                exceptionHandlerFeature.Error.Message);//Message
                        }
                        // by sending below exception, we will lose it as it is not send to the consumer. So we should know about it
                        context.Response.StatusCode = 500; // Status code number
                        await context.Response.WriteAsync("An unexpected fault happend. Try again later."); // The message for that status code
                        
                    });
                });
            }

            // This is method is used to create mapping, it accepts an action on a mapping configuration as a parameter
            AutoMapper.Mapper.Initialize(cfg =>
            {   //           Source             Destination
                cfg.CreateMap<Entities.Author, Model.AuthorDto>()
                    //Projections transform a source to a destination beyond flattening the object model.
                    // This specified using custom member mapping
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                     $"{src.FirstName} {src.LastName}"))
                    .ForMember(dest => dest.Age, opt => opt.MapFrom(src =>
                     src.DateOfBirth.GetCurrentAge()));
                cfg.CreateMap<Entities.Book, Model.BookDto>();
                cfg.CreateMap< Model.AuthorForCreationDto, Entities.Author>(); //This mapping is used for input so the source is from the request to the repo
                cfg.CreateMap<Model.BookForCreationDto, Entities.Book>();
                cfg.CreateMap<Model.BookForUpdateDto, Entities.Book>();
                cfg.CreateMap<Entities.Book, Model.BookForUpdateDto>();

            });

            libraryContext.EnsureSeedDataForContext();

            app.UseMvc(); 
        }

        private void appBuilder(IApplicationBuilder obj)
        {
            throw new NotImplementedException();
        }
    }
}
