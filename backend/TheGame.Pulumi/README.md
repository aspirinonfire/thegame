# The Game IaC with Pulumi

## Setup
### For local state management:
`pulumi login --local`
`mkdir && cd`
`pulumi new csharp`

### For self-managed
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
CREATE USER [aca-thegame-dev] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [aca-thegame-dev];
ALTER ROLE db_datawriter ADD MEMBER [aca-thegame-dev];
ALTER ROLE db_ddladmin ADD MEMBER [aca-thegame-dev];
GO
```

#### Database Migrations
Migrations are executed as Init Containers. For the simplicity sake, API image ships with optional startup arg that
invokes EF migration and exits. This setup also enables shipping both business code and required migrations in a single package.

To manually drop tables without dropping entire database:
```sql
DROP TABLE Games;
DROP TABLE LicensePlates;
DROP TABLE Players;
DROP TABLE PlayerIdentities;
```