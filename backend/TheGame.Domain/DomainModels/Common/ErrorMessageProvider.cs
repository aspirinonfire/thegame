namespace TheGame.Domain.DomainModels.Common;

public static class ErrorMessageProvider
{
  public const string InvalidNewTokenError = "new_refresh_token_value_invalid";
  public const string InvalidNewTokenAgeError = "new_refresh_token_age_invalid";

  public const string PlayerNotFoundError = "player_not_found";
  public const string ActiveGameNotFoundError = "active_game_not_found";

  public const string AlreadyHasActiveGameError = "only_one_active_game_allowed";
  public const string InactiveGameError = "inactive_game";
  public const string FailedToAddSpotError = "failed_to_add_spot";
  public const string InvalidEndedOnDateError = "invalid_ended_on_date";
  public const string UninvitedPlayerError = "invalid_player";

  public const string PlayerAlreadyInvitedError = "player_already_invited";
  public const string InactiveGameInviteError = "player_invite_inactive_game";

  public const string LicensePlateNotFoundError = "license_plate_not_found";
}
