using System;
using System.Collections.Generic;
using System.Linq;

namespace BebooGarden.MiniGames;

/// <summary>
/// The competition center at the corner of every map. Each contest it hosts keeps its own daily
/// allowance, so a day spent racing still leaves the jumping pit open.
/// </summary>
public static class Competition
{
  public const int MAXTRIESPERDAY = 5;

  /// <summary>Tries spent today per contest. Persisted in the save, reset when the day changes.</summary>
  public static Dictionary<CompetitionType, int> TodayTries { get; set; } = [];

  /// <summary>True while any contest is running, whichever one it is.</summary>
  public static bool IsRunning { get; set; }

  public static IEnumerable<CompetitionType> All =>
      Enum.GetValues<CompetitionType>().Where(type => type != CompetitionType.None);

  public static int GetRemainingTriesToday(CompetitionType type) =>
      Math.Max(0, MAXTRIESPERDAY - TodayTries.GetValueOrDefault(type));

  public static bool IsOpen(CompetitionType type) => GetRemainingTriesToday(type) > 0;

  /// <summary>The centre itself is open while any single contest still has a try left.</summary>
  public static bool IsAnyOpen() => All.Any(IsOpen);

  public static int GetRemainingTriesToday() => All.Sum(GetRemainingTriesToday);

  public static void UseATry(CompetitionType type) =>
      TodayTries[type] = TodayTries.GetValueOrDefault(type) + 1;

  /// <summary>A new day wipes the slate for every contest.</summary>
  public static void ResetDay() => TodayTries = [];
}
