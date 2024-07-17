using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games;

public sealed record Achievement(string AchievementName, int ScoreBonus, Func<HashSet<LicensePlate.PlateKey>, bool> IsUnlocked);

public sealed record GameScoreResult(int NumberOfSpottedPlates, IReadOnlyCollection<string> Achievements, int TotalScore);

public interface IGameScoreCalculator
{
  GameScoreResult CalculateGameScore(IReadOnlyCollection<LicensePlate.PlateKey> Spots);
}

public class GameScoreCalculator : IGameScoreCalculator
{
  public const int CoastToCoastScoreBonus = 100;

  public static readonly ReadOnlyDictionary<StateOrProvince, HashSet<StateOrProvince>> UsStateBorders = new Dictionary<StateOrProvince, HashSet<StateOrProvince>>()
  {
    { StateOrProvince.AL, [StateOrProvince.FL, StateOrProvince.GA, StateOrProvince.TN, StateOrProvince.MS] },
    { StateOrProvince.AK, [ ] },
    { StateOrProvince.AZ, [ StateOrProvince.NM, StateOrProvince.UT, StateOrProvince.NV, StateOrProvince.CA] },
    { StateOrProvince.AR, [ StateOrProvince.LA, StateOrProvince.MS, StateOrProvince.TN, StateOrProvince.MO, StateOrProvince.OK, StateOrProvince.TX] },
    { StateOrProvince.CA, [ StateOrProvince.AZ, StateOrProvince.NV, StateOrProvince.OR] },
    { StateOrProvince.CO, [ StateOrProvince.NM, StateOrProvince.OK, StateOrProvince.KS, StateOrProvince.NE, StateOrProvince.WY, StateOrProvince.UT] },
    { StateOrProvince.CT, [ StateOrProvince.RI, StateOrProvince.MA, StateOrProvince.NY] },
    { StateOrProvince.DE, [ StateOrProvince.NJ, StateOrProvince.PA, StateOrProvince.MD] },
    { StateOrProvince.FL, [ StateOrProvince.GA, StateOrProvince.AL] },
    { StateOrProvince.GA, [ StateOrProvince.SC, StateOrProvince.NC, StateOrProvince.TN, StateOrProvince.AL, StateOrProvince.FL] },
    { StateOrProvince.HI, [ ] },
    { StateOrProvince.ID, [ StateOrProvince.WA, StateOrProvince.OR, StateOrProvince.NV, StateOrProvince.UT, StateOrProvince.WY, StateOrProvince.MT] },
    { StateOrProvince.IL, [ StateOrProvince.WI, StateOrProvince.IA, StateOrProvince.MO, StateOrProvince.KY, StateOrProvince.IN] },
    { StateOrProvince.IN, [ StateOrProvince.IL, StateOrProvince.KY, StateOrProvince.OH, StateOrProvince.MI] },
    { StateOrProvince.IA, [ StateOrProvince.MN, StateOrProvince.SD, StateOrProvince.NE, StateOrProvince.MO, StateOrProvince.IL, StateOrProvince.WI] },
    { StateOrProvince.KS, [ StateOrProvince.OK, StateOrProvince.MO, StateOrProvince.NE, StateOrProvince.CO] },
    { StateOrProvince.KY, [ StateOrProvince.TN, StateOrProvince.VA, StateOrProvince.WV, StateOrProvince.OH, StateOrProvince.IN, StateOrProvince.IL, StateOrProvince.MO] },
    { StateOrProvince.LA, [ StateOrProvince.MS, StateOrProvince.AR, StateOrProvince.TX] },
    { StateOrProvince.ME, [ StateOrProvince.NH] },
    { StateOrProvince.MD, [ StateOrProvince.DE, StateOrProvince.PA, StateOrProvince.WV, StateOrProvince.VA] },
    { StateOrProvince.MA, [ StateOrProvince.NH, StateOrProvince.VT, StateOrProvince.NY, StateOrProvince.CT, StateOrProvince.RI] },
    { StateOrProvince.MI, [ StateOrProvince.WI, StateOrProvince.IN, StateOrProvince.OH] },
    { StateOrProvince.MN, [ StateOrProvince.ND, StateOrProvince.SD, StateOrProvince.IA, StateOrProvince.WI] },
    { StateOrProvince.MS, [ StateOrProvince.AL, StateOrProvince.TN, StateOrProvince.AR, StateOrProvince.LA] },
    { StateOrProvince.MO, [ StateOrProvince.AR, StateOrProvince.TN, StateOrProvince.KY, StateOrProvince.IL, StateOrProvince.IA, StateOrProvince.NE, StateOrProvince.KS, StateOrProvince.OK] },
    { StateOrProvince.MT, [ StateOrProvince.ID, StateOrProvince.WY, StateOrProvince.SD, StateOrProvince.ND] },
    { StateOrProvince.NE, [ StateOrProvince.KS, StateOrProvince.MO, StateOrProvince.IA, StateOrProvince.SD, StateOrProvince.WY, StateOrProvince.CO] },
    { StateOrProvince.NV, [ StateOrProvince.AZ, StateOrProvince.UT, StateOrProvince.ID, StateOrProvince.OR, StateOrProvince.CA] },
    { StateOrProvince.NH, [ StateOrProvince.VT, StateOrProvince.MA, StateOrProvince.ME] },
    { StateOrProvince.NJ, [ StateOrProvince.NY, StateOrProvince.PA, StateOrProvince.DE] },
    { StateOrProvince.NM, [ StateOrProvince.TX, StateOrProvince.OK, StateOrProvince.CO, StateOrProvince.AZ] },
    { StateOrProvince.NY, [ StateOrProvince.PA, StateOrProvince.NJ, StateOrProvince.CT, StateOrProvince.MA, StateOrProvince.VT] },
    { StateOrProvince.NC, [ StateOrProvince.VA, StateOrProvince.TN, StateOrProvince.GA, StateOrProvince.SC] },
    { StateOrProvince.ND, [ StateOrProvince.MT, StateOrProvince.SD, StateOrProvince.MN] },
    { StateOrProvince.OH, [ StateOrProvince.MI, StateOrProvince.IN, StateOrProvince.KY, StateOrProvince.WV, StateOrProvince.PA] },
    { StateOrProvince.OK, [ StateOrProvince.TX, StateOrProvince.AR, StateOrProvince.MO, StateOrProvince.KS, StateOrProvince.CO, StateOrProvince.NM] },
    { StateOrProvince.OR, [ StateOrProvince.CA, StateOrProvince.NV, StateOrProvince.ID, StateOrProvince.WA] },
    { StateOrProvince.PA, [ StateOrProvince.OH, StateOrProvince.WV, StateOrProvince.MD, StateOrProvince.DE, StateOrProvince.NJ, StateOrProvince.NY] },
    { StateOrProvince.RI, [ StateOrProvince.MA, StateOrProvince.CT] },
    { StateOrProvince.SC, [ StateOrProvince.NC, StateOrProvince.GA] },
    { StateOrProvince.SD, [ StateOrProvince.NE, StateOrProvince.IA, StateOrProvince.MN, StateOrProvince.ND, StateOrProvince.MT, StateOrProvince.WY] },
    { StateOrProvince.TN, [ StateOrProvince.AL, StateOrProvince.GA, StateOrProvince.NC, StateOrProvince.VA, StateOrProvince.KY, StateOrProvince.MO, StateOrProvince.AR, StateOrProvince.MS] },
    { StateOrProvince.TX, [ StateOrProvince.LA, StateOrProvince.AR, StateOrProvince.OK, StateOrProvince.NM] },
    { StateOrProvince.UT, [ StateOrProvince.AZ, StateOrProvince.CO, StateOrProvince.WY, StateOrProvince.ID, StateOrProvince.NV] },
    { StateOrProvince.VT, [ StateOrProvince.NY, StateOrProvince.MA, StateOrProvince.NH] },
    { StateOrProvince.VA, [ StateOrProvince.MD, StateOrProvince.WV, StateOrProvince.KY, StateOrProvince.TN, StateOrProvince.NC] },
    { StateOrProvince.WA, [ StateOrProvince.OR, StateOrProvince.ID] },
    { StateOrProvince.WV, [ StateOrProvince.VA, StateOrProvince.MD, StateOrProvince.PA, StateOrProvince.OH, StateOrProvince.KY] },
    { StateOrProvince.WI, [ StateOrProvince.MN, StateOrProvince.IA, StateOrProvince.IL, StateOrProvince.MI] },
    { StateOrProvince.WY, [ StateOrProvince.CO, StateOrProvince.NE, StateOrProvince.SD, StateOrProvince.MT, StateOrProvince.ID, StateOrProvince.UT]}
  }.AsReadOnly();

  private readonly static Achievement _westCoastAchievement = new("West Coast",
    10,
    spottedPlates =>
    {
      var westCoastStates = new HashSet<LicensePlate.PlateKey>([
        new (Country.US, StateOrProvince.CA),
        new (Country.US, StateOrProvince.OR),
        new (Country.US, StateOrProvince.WA)
      ]);

      return !westCoastStates.Except(spottedPlates).Any();
    });

  private readonly static Achievement _eastCoastAchievement = new("East Coast",
    30,
    spottedPlates =>
    {
      var eastCoastStates = new HashSet<LicensePlate.PlateKey>([
        new (Country.US, StateOrProvince.CT),
        new (Country.US, StateOrProvince.DE),
        new (Country.US, StateOrProvince.FL),
        new (Country.US, StateOrProvince.GA),
        new (Country.US, StateOrProvince.ME),
        new (Country.US, StateOrProvince.MD),
        new (Country.US, StateOrProvince.MA),
        new (Country.US, StateOrProvince.NH),
        new (Country.US, StateOrProvince.NJ),
        new (Country.US, StateOrProvince.NY),
        new (Country.US, StateOrProvince.NC),
        new (Country.US, StateOrProvince.RI),
        new (Country.US, StateOrProvince.SC),
        new (Country.US, StateOrProvince.VA)
      ]);

      return !eastCoastStates.Except(spottedPlates).Any();
    });

  private readonly static Achievement _coastToCoastAchievement = new("Coast-to-Coast",
    100,
    spottedPlates =>
    {
      var spotLookup = spottedPlates.Select(spt => spt.StateOrProvince).ToHashSet();

      var westCoastSpots = new[]
        {
          StateOrProvince.CA,
          StateOrProvince.OR,
          StateOrProvince.WA
        }
        .Where(spotLookup.Contains)
        .ToImmutableHashSet();

      var eastCoastSpots = new[]
        {
          StateOrProvince.CT,
          StateOrProvince.DE,
          StateOrProvince.FL,
          StateOrProvince.GA,
          StateOrProvince.ME,
          StateOrProvince.MD,
          StateOrProvince.MA,
          StateOrProvince.NH,
          StateOrProvince.NJ,
          StateOrProvince.NY,
          StateOrProvince.NC,
          StateOrProvince.RI,
          StateOrProvince.SC,
          StateOrProvince.VA
        }
        .Where(spotLookup.Contains)
        .ToImmutableHashSet();

      // if there are no west or east coast states, don't build graph
      if (westCoastSpots.Count == 0 || eastCoastSpots.Count == 0)
      {
        return false;
      }

      var statesToVisit = westCoastSpots.ToHashSet();
      var visitedStates = new HashSet<StateOrProvince>();
      while (statesToVisit.Count > 0)
      {
        var toVisit = statesToVisit.First();
        statesToVisit.Remove(toVisit);

        // state was already visited previously
        if (visitedStates.Contains(toVisit))
        {
          continue;
        }

        // mark as visited
        visitedStates.Add(toVisit);

        // get current borders and and check if there's a connection
        if (!UsStateBorders.TryGetValue(toVisit, out var borders))
        {
          continue;
        }

        foreach (var borderingState in borders)
        {
          // skip checking border if it wasn't spotted or is marked for a visit
          if (visitedStates.Contains(borderingState) ||
            statesToVisit.Contains(borderingState) ||
            !spotLookup.Contains(borderingState))
          {
            continue;
          }

          if (eastCoastSpots.Contains(borderingState))
          {
            // border is spotted and connects to east coast. achievement unlocked.
            return true;
          }
          else
          {
            // border is spotted but does not connect to east coast. mark for visiting.
            statesToVisit.Add(borderingState);
          }
        }
      }

      return false;
    });


  public GameScoreResult CalculateGameScore(IReadOnlyCollection<LicensePlate.PlateKey> Spots)
  {
    var currentScore = Spots.Count;
    var currentAchievements = new List<string>();

    // Add Score bonues for full achievements
    var spottedPlatesLookup = Spots.ToHashSet();

    var achievementsToCheck = new[]
    {
      _westCoastAchievement,
      _eastCoastAchievement,
      _coastToCoastAchievement
    };

    foreach (var achievement in achievementsToCheck)
    {
      var isUnlocked = achievement.IsUnlocked(spottedPlatesLookup);
      if (isUnlocked)
      {
        currentScore += achievement.ScoreBonus;
        currentAchievements.Add(achievement.AchievementName);
      }
    }

    return new GameScoreResult(Spots.Count, currentAchievements.AsReadOnly(), currentScore);
  }
}
