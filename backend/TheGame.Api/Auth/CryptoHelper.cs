using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public interface ICryptoHelper
{
  Result<T> DecryptPayload<T>(string encryptedValue, string secret, string keyInfo);
  Result<string> EncryptPayload(object payload, string secret, string keyInfo);
}

public sealed class CryptoHelper : ICryptoHelper
{
  private const int _aesGsmNonceLen = 12;    // AES‑GCM nonce
  private const int _tagSizeInBytes = 16;    // 128‑bit tag

  public static ReadOnlySpan<byte> DeriveHkdfKey(string secret, string info, ushort outputLength) =>
    HKDF.DeriveKey(
      hashAlgorithmName: HashAlgorithmName.SHA256,
      ikm: Encoding.UTF8.GetBytes(secret),
      outputLength: outputLength,
      salt: null,
      info: Encoding.UTF8.GetBytes(info));

  public Result<string> EncryptPayload(object payload, string secret, string keyInfo)
  {
    try
    {
      var json = JsonSerializer.Serialize(payload);
      var jsonBytes = Encoding.UTF8.GetBytes(json);

      Span<byte> aesNonce = stackalloc byte[_aesGsmNonceLen];
      RandomNumberGenerator.Fill(aesNonce);

      Span<byte> cipherText = stackalloc byte[jsonBytes.Length];
      Span<byte> tag = stackalloc byte[_tagSizeInBytes];

      using var gcm = new AesGcm(DeriveHkdfKey(secret, keyInfo, 32), _tagSizeInBytes);
      gcm.Encrypt(aesNonce, jsonBytes, cipherText, tag);

      var packed = new byte[_aesGsmNonceLen + _tagSizeInBytes + cipherText.Length];
      aesNonce.CopyTo(packed);
      tag.CopyTo(packed.AsSpan()[_aesGsmNonceLen..]);
      cipherText.CopyTo(packed.AsSpan()[(_aesGsmNonceLen + _tagSizeInBytes)..]);

      return Base64Url.EncodeToString(packed);
    }
    catch (Exception ex)
    {
      return new Failure($"Failed to encrypt payload. Got {ex.GetType().Name}: {ex.Message}");
    }
  }

  public Result<T> DecryptPayload<T>(string encryptedValue, string secret, string keyInfo)
  {
    try
    {
      var packed = Base64Url.DecodeFromChars(encryptedValue).AsSpan();

      var aesNonce = packed.Slice(0, _aesGsmNonceLen);
      var tag = packed.Slice(_aesGsmNonceLen, _tagSizeInBytes);
      var ciptherText = packed.Slice(_aesGsmNonceLen + _tagSizeInBytes);

      Span<byte> plainText = stackalloc byte[ciptherText.Length];
      using var gcm = new AesGcm(DeriveHkdfKey(secret, keyInfo, 32), _tagSizeInBytes);
      gcm.Decrypt(aesNonce, ciptherText, tag, plainText);

      var payload = JsonSerializer.Deserialize<T>(plainText);
      if (payload is null)
      {
        return new Failure("Failed to decrypt payload");
      }
      return payload;
    }
    catch (Exception ex)
    {
      return new Failure($"Failed to decrypt payload. Got {ex.GetType().Name}: {ex.Message}");
    }
  }
}
