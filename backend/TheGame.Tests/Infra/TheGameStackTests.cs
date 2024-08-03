using Microsoft.Data.SqlClient;
using TheGame.Infra;

namespace TheGame.Tests.Infra;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class TheGameStackTests
{
  [Fact]
  public void WillGenerateValidSqlConnectionString()
  {
    var resources = new ExistingAzureResources("res",
      "id",
      "westus2",
      "testserver",
      "testdb");

    var uutString = TheGameStack.GetSqlConnectionString(resources, "Active Directory Managed Identity");

    var actualException = Record.Exception(() => new SqlConnectionStringBuilder(uutString));

    Assert.Null(actualException);
  }

  [Fact]
  public void EnsureSqlConnectionStringBuilderThrowsOnBadString()
  {
    var actualException = Record.Exception(() => new SqlConnectionStringBuilder("bad string"));

    Assert.NotNull(actualException);
  }
}
