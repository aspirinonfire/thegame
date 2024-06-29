## How to start API
1. Install WSL2 and Docker.
1. Set `docker-compose` as a startup project.
1. Create `.env` file in `docker-compose`. See `.env.sample` for a template. Populate necessary env vars.
1. Run.

### Domain and Infra are located in the same project for few reasons:
1. Leaking abstraction due to the EF Core configuration.
2. Simpler file management.

### TODO:
- [ ]   Commands (CQRS)
- [ ]   Domain event handlers (MediatR)
- [x]   DB (EF Core)
- [x]   Integration tests
- [ ]   Queries (CQRS)
- [ ]   Logging
- [ ]   APIs
--- App Features ---
- [ ]   Google auth backend
- [ ]   Google auth UI
- [ ]   Consume game APIs
- [ ]   Push notis (SignalR)
- [ ]   Offline gaming
- [ ]   Azure deployment
--- Prod ---
- [ ]   Sendgrid for team invites
- [ ]   Azure Container Apps
- [ ]   App Insights


## EF Migrations:
> Make sure to set `appsettings.Development.json` `GameDB` connection string before running migrations!

-- Create initial DB migration script --
`dotnet ef migrations add initial --verbose -s ..\TheGame.Api\ --output-dir DAL\Migrations`

-- Add new migration --
`dotnet ef migrations add <MigrationName> --verbose`

-- Run migration --
`dotnet ef database update -s "TheGame.Api" -p "TheGame.Api"`
