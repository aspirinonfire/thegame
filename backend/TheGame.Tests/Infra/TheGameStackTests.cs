using Microsoft.Data.SqlClient;

namespace TheGame.Tests.Infra;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class TheGameStackTests
{
  [Fact]
  public void EnsureSqlConnectionStringBuilderThrowsOnBadString()
  {
    var actualException = Record.Exception(() => new SqlConnectionStringBuilder("bad string"));

    Assert.NotNull(actualException);
  }
}
