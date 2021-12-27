using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using TheGame.Domain;
using TheGame.Domain.DAL;

namespace TheGame.Api
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
      var connString =  Configuration.GetConnectionString(GameDbContext.ConnectionStringName);
      if (string.IsNullOrEmpty(connString))
      {
        string msg = $"{GameDbContext.ConnectionStringName} connection string is not found! Aborting...";
        throw new ApplicationException(msg);
      }
      // TODO env check
      var isDevelopment = true;
      services.AddGameServices(connString, isDevelopment);

      services.AddControllers();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "thegame.api", Version = "v1" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app,
      IWebHostEnvironment env,
      ILogger<Startup> logger,
      GameDbContext dbContext)
    {
      if (!dbContext.Database.CanConnect())
      {
        string msg = "Could not connect to database! Aborting...";
        logger.LogCritical(msg);
        throw new ApplicationException(msg);
      }

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "thegame.api v1"));
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
