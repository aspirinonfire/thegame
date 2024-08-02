namespace TheGame.Infra;

public sealed record ExistingAzureResources(string ResourceGroupName,
  string ResourceGroupId,
  string ResourceGroupLocation,
  string AzureSqlServerName,
  string AzureSqlDbName);