using System;

namespace BebooGarden.GameCore;

/// <summary>
/// What being away costs a beboo.
///
/// The game has always worked off two thresholds: past four hours since you last played, a beboo's
/// happiness drops to <see cref="SadFloor"/>; past eight, it has had all the sleep it needs. That is
/// fine on a desktop, where playing means sitting down for a while.
///
/// It is wrong on a phone. There the game is opened for a minute at the bus stop and closed again,
/// a dozen times a day, and every gap between those is hours long. Left as it stands, a pocket
/// beboo would spend its entire life at the sad floor and the game would nag constantly. So the
/// clock that these thresholds are measured against can be slowed right down per platform.
/// </summary>
public static class OfflineProgress
{
  /// <summary>Happiness a beboo sinks to after a long enough absence. Never below this.</summary>
  public const int SadFloor = 3;

  private const double SadAfterHours = 4;
  private const double RestedAfterHours = 8;

  /// <summary>
  /// How fast time away counts against a beboo, as a multiple of real time.
  ///
  /// 1.0 is the original behaviour and stays the default, so the Windows game is unchanged. The
  /// Android head sets this far lower: at 0.1 a beboo needs forty real hours away to reach the sad
  /// floor rather than four, which is about right for something you dip into all day.
  /// </summary>
  public static double IdleRate { get; set; } = 1.0;

  /// <summary>Real time away, converted to the time the beboo experienced.</summary>
  public static double FeltHours(TimeSpan away) => Math.Max(0, away.TotalHours) * IdleRate;

  /// <summary>Happiness after being left alone, given what it was when you left.</summary>
  public static int HappinessAfter(int happiness, TimeSpan away)
      => FeltHours(away) > SadAfterHours ? SadFloor : happiness;

  /// <summary>Energy after being left alone. Long enough away and a beboo is fully rested.</summary>
  public static float EnergyAfter(float energy, float restedEnergy, TimeSpan away)
      => FeltHours(away) > RestedAfterHours ? restedEnergy : energy;

  /// <summary>
  /// How long from <paramref name="since"/> until a beboo that is happy now would reach the sad
  /// floor, or null if it is already there and nothing more is going to happen.
  ///
  /// This is what a phone schedules its one notification against: the moment the beboo would
  /// actually want you, rather than a timer that fires because a timer was due.
  /// </summary>
  public static TimeSpan? TimeUntilSad(int happiness, DateTime since, DateTime now)
  {
    if (happiness <= SadFloor) return null;
    if (IdleRate <= 0) return null;

    double realHoursNeeded = SadAfterHours / IdleRate;
    DateTime sadAt = since.AddHours(realHoursNeeded);
    return sadAt <= now ? TimeSpan.Zero : sadAt - now;
  }
}
