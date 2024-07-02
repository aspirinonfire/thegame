using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.PlayerIdentities;

/// <summary>
/// Player identity record.
/// </summary>
/// <remarks>
/// This entity may be better managed by a separate DbContext but for the simplicity sake its all in one place.
/// </remarks>
public partial class PlayerIdentity : IAuditedRecord
{
  public long Id { get; protected set; }

  public virtual Player? Player { get; protected set; }

  public string ProviderName { get; protected set; } = default!;

  public string ProviderIdentityId { get; protected set; } = default!;

  public string RefreshToken { get; protected set; } = default!;

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }
}
