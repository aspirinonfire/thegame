## How to use API
1. Install WSL2 and Docker.
1. Set `docker-compose` as a startup project.
1. Create `.env` file in `docker-compose`. See `.env.sample` for a template. Populate necessary env vars.
1. Run. Once all images have been pulled, VS should open [Game API Swagger UI](https://localhost:8080/swagger/index.html) automatically.
1. Authenticate with [Google](https://localhost:8080/account/login). This should set auth cookie and other APIs can now be executed from swagger.
1. Check API health by visiting [https://localhost:8080/health](https://localhost:8080/health)

## EF Migrations:
> Make sure to set `appsettings.Development.json` `GameDB` connection string before running migrations! For best experience, cd to `TheGame.Domain` project first before running migrations.

#### Add new migration
`dotnet ef migrations add <MigrationName> --verbose --startup-project "../TheGame.Api" --project "." --output-dir "DomainModels/Migrations"`

### Run migration
`dotnet ef database update --startup-project "../TheGame.Api" --project "../TheGame.Api"`
