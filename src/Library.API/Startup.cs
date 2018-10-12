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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {   
                app.UseExceptionHandler(appBuilder =>   // This creates a global message for a specific status code
                {
                    appBuilder.Run(async context => // This write the request response pipeline
                    {
                        context.Response.StatusCode = 500; // Status code number
                        await context.Response.WriteAsync("An unexpected fault happend. Try again later."); // The message for that status code
                    });
                });
            }

            // This is method is used to create mapping, it accepts an action on a mapping configuration as a parameter
            AutoMapper.Mapper.Initialize(cfg =>
            {   //           Source             Destination
                cfg.CreateMap<Entities.Author, Model.AuthorDTO>()
                    //Projections transform a source to a destination beyond flattening the object model.
                    // This specified using custom member mapping
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                     $"{src.FirstName} {src.LastName}"))
                    .ForMember(dest => dest.Age, opt => opt.MapFrom(src =>
                     src.DateOfBirth.GetCurrentAge()));
                cfg.CreateMap<Entities.Book, Model.BookDTO>();
                cfg.CreateMap< Model.AuthorForCreationDTO, Entities.Author>(); //This mapping is used for input so the source is from the request to the repo
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
