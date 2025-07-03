using Microsoft.Extensions.Hosting;
using TheGame.Domain;

Console.WriteLine("Running EF Migrations");

await Host.CreateDefaultBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddGameServices(
      sp => default!, // we don't need event bus for migrations
      "this connecion will be replaces with efbundle --connection value");
  })
  .Build()
  .RunAsync();

Console.WriteLine("EF Migrations completed");
