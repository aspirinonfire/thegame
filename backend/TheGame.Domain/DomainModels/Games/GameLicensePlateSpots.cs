using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public sealed record GameLicensePlateSpots(IReadOnlyCollection<(Country country, StateOrProvince stateOrProvince)> Spots, DateTimeOffset SpottedOn, Player SpottedBy);
