// dev env config
var dbGitIgnoredVolumeDirectory = "mssql_volume";
var dbServerPort = 1433;
var dbExposedPort = 1433;
var uiRelativePath = "../../ui";
var uiDevCommand = "dev";
var uiDevServerPort = 3000;
var uiExposedPort = 3001;

var builder = DistributedApplication.CreateBuilder(args);

// aspire params
var saPwd = builder.AddParameter("DevSqlPassword");
var gameDbName = builder.AddParameter("GameDbName");

var googleClientId = builder.AddParameter("GoogleClientId");
var googleClientSecret = builder.AddParameter("GoogleClientSecret");
var apiJwtSecret = builder.AddParameter("ApiJwtSecret");
var apiJwtAudience = builder.AddParameter("ApiJwtAudience");
var apiJwtExpMinutes = builder.AddParameter("ApiJwtExpirationMin");

// configure and wire up app components
var theGameDevDb = builder
    .AddSqlServer("TheGameSqlServer", saPwd)
    // persistent lifetime to speed up start up during dev
    // note: this container will be visible in docker
    .WithLifetime(ContainerLifetime.Persistent)
    // Data will be stored on the local drive
    .WithDataBindMount(dbGitIgnoredVolumeDirectory)
    // ensure consistent external port when app is not running so we can run ef migrations or use azure data studio
    .WithEndpoint(
        name: "tcp-extenral",
        port: dbExposedPort,
        targetPort: dbServerPort,
        scheme: "tcp",
        // important!
        isProxied: false)
    .AddDatabase("TheGameDevDb", gameDbName.Resource.Value);

var api = builder
  .AddProject<Projects.TheGame_Api>("api")
  .WithExternalHttpEndpoints()
  // app config
  .WithEnvironment("ConnectionStrings__GameDB", theGameDevDb.Resource.ConnectionStringExpression)
  .WithEnvironment("Auth__Google__ClientId", googleClientId)
  .WithEnvironment("Auth__Google__ClientSecret", googleClientSecret)
  .WithEnvironment("Auth__Api__JwtSecret", apiJwtSecret)
  .WithEnvironment("Auth__Api__JwtAudience", apiJwtAudience)
  .WithEnvironment("Auth__Api__JwtTokenExpirationMin", apiJwtExpMinutes.Resource.Value)
  // service dependencies
  .WaitFor(theGameDevDb);

var _ = builder
  .AddNpmApp("ui", uiRelativePath, uiDevCommand)
  .WithHttpEndpoint(port: uiExposedPort, targetPort: uiDevServerPort)
  // app config
  .WithEnvironment("NEXT_PUBLIC_GOOGLE_CLIENT_ID", googleClientId)
  // service dependencies
  .WithReference(api)
  .WaitFor(api);

builder.Build().Run();
