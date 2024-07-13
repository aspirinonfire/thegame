using Microsoft.AspNetCore.Antiforgery;

namespace TheGame.Api;

public sealed class AntiforgeryMetadata(bool required) : IAntiforgeryMetadata
{
  public bool RequiresValidation { get; } = required;
}
