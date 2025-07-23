using TheGame.Infra;

namespace TheGame.Tests.Infra;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class TheGameConfigTests
{
  private readonly static TheGameInfraConfig _validConfig = new()
  {
    ProjectName = "test project",
    StackName = "test stack",
    AzureNativeVersion = "1",
    AzureAdVersion = "1",
    BackendBlobStorageUrl = "azblob://container?storage_account=testaccount",

    SubscriptionId = "sub",
    ResourceGroupName = "res",

    DbServerName = "dbserver",
    DbName = "dbname",

    AcaEnvName = "env",
    AcaName = "aca",
    GhcrUrl = "https://ghcr.io",
    GhcrUsername = "test username",
    GhcrPat = "pat",
    GameImage = "some/image:latest",

    GoogleClientId = "client id",
    GoogleClientSecret = "secret",
    JwtSecret = "test string long enough to be used as signature secret",
    JwtAudience = "test audience",
    JwtTokenExpirationMin = 10
  };

  [Fact]
  public void WillReturnNoErrorsWhenGameStackConfigIsValid()
  {
    var uutConfig = _validConfig;

    var actualValidationErrors = uutConfig.GetValidationErrors();

    Assert.Empty(actualValidationErrors);
  }

  [Fact]
  public void WillReturnErrorWhenProjectIsMissing()
  {
    var uutConfig = _validConfig with
    {
      ProjectName = null!
    };

    var actualValidationErrors = uutConfig.GetValidationErrors();

    var actualError = Assert.Single(actualValidationErrors);
    Assert.StartsWith("ProjectName", actualError);
  }

  [Fact]
  public void WillReturnErrorWhenMultipleRequiredFieldsAreMissing()
  {
    var uutConfig = _validConfig with
    {
      ProjectName = null!,
      StackName = null!,
    };

    var actualValidationErrors = uutConfig.GetValidationErrors();

    Assert.Collection(actualValidationErrors,
      actualError1 => Assert.StartsWith("ProjectName", actualError1),
      actualError2 => Assert.StartsWith("StackName", actualError2));
  }

  [Fact]
  public void WillReturnErrorWhenBackendIsInvalid()
  {
    var uutConfig = _validConfig with
    {
      BackendBlobStorageUrl = "azblob://<container>?storage_account=<testaccount>"
    };

    var actualValidationErrors = uutConfig.GetValidationErrors();

    var actualError = Assert.Single(actualValidationErrors);
    Assert.StartsWith("BackendBlobStorageUrl", actualError);
  }

  [Fact]
  public void WillReturnErrorWhenJwtSecretIsTooShort()
  {
    var uutConfig = _validConfig with
    {
      JwtSecret = "too short"
    };

    var actualValidationErrors = uutConfig.GetValidationErrors();

    var actualError = Assert.Single(actualValidationErrors);
    Assert.StartsWith("JwtSecret", actualError);
  }
}
