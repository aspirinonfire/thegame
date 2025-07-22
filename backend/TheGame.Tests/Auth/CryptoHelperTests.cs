using TheGame.Api.Auth;

namespace TheGame.Tests.Auth;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class CryptoHelperTests
{
  private const string _secret = "this is a very secret value for testing!";
  private const string _keyInfo = "test-key";

  [Fact]
  public void WillEncryptPayload()
  {
    var payloadToEncrypt = new TestPayload("Hello World!");

    var uutService = new CryptoHelper();

    var actualCipherTextResult = uutService.EncryptPayload(payloadToEncrypt, _secret, _keyInfo);

    // note: we cannot compare expected to actual because encryption uses random aes nonce for each invocation
    var actualCipherText = AssertResult.AssertIsSucceessful(actualCipherTextResult);
    Assert.NotEmpty(actualCipherText);
  }

  [Fact]
  public void WillDecryptPayload()
  {
    var cipherTextToDecrypt = "6JK1c-LYZPd5LvTC57Cgbu5VMuZHl3RymAD9NUzHgJuBccZhSCTdFhtibbGNc4E3jICH_TJo";

    var uutService = new CryptoHelper();

    var actualPayloadResult = uutService.DecryptPayload<TestPayload>(cipherTextToDecrypt, _secret, _keyInfo);

    var actualPayload = AssertResult.AssertIsSucceessful(actualPayloadResult);
    Assert.Equal(new TestPayload("Hello World!"), actualPayload);
  }

  private sealed record TestPayload(string Message);
}
