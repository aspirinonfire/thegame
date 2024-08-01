# The Game IaC with Pulumi

## Setup
#### local state management:
`pulumi login --local`
`mkdir && cd`
`pulumi new csharp`

#### self-managed remote backend
[Self Managed backend](https://www.pulumi.com/docs/concepts/state/#using-a-self-managed-backend)

### Game Infra setup
#### Requirements
Some of these steps can be automated but for the current setup it is assumed these are done manually:
1. Add Microsoft.App provider to subscription.
1. Create a new Resource Group (eg. `rg-thegame-<env>`) in the subscription.
1. Create a new Service Principal (SP) in Entra (App Registration) to be used by Pulumi. Create and "store" password.
1. Assign `Owner` role to new SP on the resource group. More granular RBAC roles should be explored for Production.
1. Create Azure SQL server and database. Use free serverless tier for dev. Select Entra auth only mode (passwordless). Follow [Passwordless Setup Instructions](https://learn.microsoft.com/en-us/azure/azure-sql/database/azure-sql-dotnet-quickstart?view=azuresql&tabs=visual-studio%2Cpasswordless%2Cservice-connector%2Cportal).

```sql
# Give access to Container App
CREATE USER [aca-thegame-dev] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [aca-thegame-dev];
ALTER ROLE db_datawriter ADD MEMBER [aca-thegame-dev];
ALTER ROLE db_ddladmin ADD MEMBER [aca-thegame-dev];
GO
```

#### Database Migrations
> Migrations will be handled by ef bundle. This setup allows running migrations without needing to connect to database directly to generate sql.
1. Create bundle: `dotnet ef migrations bundle --self-contained -r linux-x64 --project .\TheGame.Domain\ --startup-project .\TheGame.DbMigrator\ --force`
1. Run bundle: `./efbundle --connection <conn_string>`

#### ACA Gotchas
1. Init Containers do not support Managed Identity by default. This breaks passwordless SQL authentication during migrations. See [MSDN](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity?tabs=portal%2Cdotnet#control-managed-identity-availability) and [GH Issue](https://github.com/microsoft/azure-container-apps/issues/807)
1. Init Containers will run every time main container scales from 0 to 1+. When at zero, K8s deletes pod, and at one it re-creates one, causing init container to run again. With scale 0 requirement, init container is not a good solution to run migrations.