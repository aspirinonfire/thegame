using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheGame.Domain;
using TheGame.Domain.DAL;

namespace TheGame.Api
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services.AddControllers();

      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(cfg =>
      {
        cfg.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Game API", Version = "1" });
      });

      var connString = builder.Configuration.GetConnectionString(GameDbContext.ConnectionStringName) ?? string.Empty;

      builder.Services.AddGameServices(connString, builder.Environment.IsDevelopment());

      builder.Services.AddHealthChecks()
        .AddCheck<ApiInfraHealthCheck>(nameof(ApiInfraHealthCheck));

      builder.Services.AddGameAuthenticationServices(builder.Configuration);

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));
      }

      app.UseHsts();
      app.UseHttpsRedirection();

      app.UseHealthChecks("/health");

      var cookiePolicyOptions = new CookiePolicyOptions
      {
        MinimumSameSitePolicy = SameSiteMode.Lax,
        HttpOnly = HttpOnlyPolicy.Always,
        Secure = CookieSecurePolicy.Always
      };
      app.UseCookiePolicy(cookiePolicyOptions);

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

#pragma warning disable ASP0014 // Suggest using top level route registrations
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers().RequireAuthorization();
      });
#pragma warning restore ASP0014 // Suggest using top level route registrations

      app.Run();
    }
  }
}
