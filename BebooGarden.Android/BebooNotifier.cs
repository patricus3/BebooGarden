using Android.App;
using Android.Content;
using Android.OS;
using BebooGarden.GameCore;
using System;
using System.Linq;

namespace BebooGarden.Droid;

/// <summary>
/// Tells you a beboo would like to see you - once, and only when that is actually true.
///
/// The rules exist because a tamagotchi that nags is a tamagotchi you uninstall:
///   - one notification per absence, scheduled for the moment the beboo would reach the sad floor,
///     not a timer that fires because a timer was due;
///   - never at night, because the beboos are asleep between 22:00 and 08:00 and so are you;
///   - never sooner than <see cref="MinimumGap"/> after the last one;
///   - cancelled the moment you open the game, and not re-armed until you have actually visited.
///
/// <see cref="OfflineProgress.TimeUntilSad"/> does the working out, so the slowed-down idle clock
/// on this platform stretches the notification with it rather than being a second set of numbers
/// that can drift out of step.
/// </summary>
public sealed class BebooNotifier
{
  private const string ChannelId = "beboo-misses-you";
  private const int NotificationId = 1;
  private const int QuietFromHour = 22;
  private const int QuietUntilHour = 8;

  /// <summary>No two notifications closer together than this, whatever else is true.</summary>
  public static readonly TimeSpan MinimumGap = TimeSpan.FromHours(20);

  private readonly Context _context;
  private readonly NotificationManager? _manager;

  public BebooNotifier(Context context)
  {
    _context = context;
    _manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
    CreateChannel();
  }

  private void CreateChannel()
  {
    if (_manager is null) return;
    var channel = new NotificationChannel(
        ChannelId,
        "A beboo misses you",
        // Not High: this must never interrupt anything. It is a beboo, not a phone call.
        NotificationImportance.Default)
    {
      Description = "Sent at most once a day when a beboo has been alone for a long time.",
    };
    channel.EnableVibration(false);
    _manager.CreateNotificationChannel(channel);
  }

  /// <summary>Whether <paramref name="when"/> falls in the hours nobody wants to be told anything.</summary>
  public static bool IsQuietHour(DateTime when)
      => when.Hour >= QuietFromHour || when.Hour < QuietUntilHour;

  /// <summary>
  /// Moves a time out of the quiet hours by pushing it to the next 08:00. A beboo that gets sad at
  /// two in the morning waits until a reasonable hour to mention it.
  /// </summary>
  public static DateTime ShiftOutOfQuietHours(DateTime when)
  {
    if (!IsQuietHour(when)) return when;
    DateTime morning = when.Date.AddHours(QuietUntilHour);
    return when.Hour >= QuietFromHour ? morning.AddDays(1) : morning;
  }

  /// <summary>
  /// Works out whether and when to notify, from the state the game is being closed in. Returns null
  /// when there is nothing worth saying.
  /// </summary>
  public static DateTime? NextNotificationTime(
      int happiestBeboo, DateTime lastPlayed, DateTime? lastNotified, DateTime now)
  {
    TimeSpan? until = OfflineProgress.TimeUntilSad(happiestBeboo, lastPlayed, now);
    if (until is null) return null;

    DateTime when = ShiftOutOfQuietHours(now + until.Value);
    if (lastNotified is DateTime last && when - last < MinimumGap)
      when = ShiftOutOfQuietHours(last + MinimumGap);

    return when;
  }

  /// <summary>Schedules the one notification, replacing any already pending.</summary>
  public void Schedule(DateTime when, string bebooName)
  {
    var alarms = _context.GetSystemService(Context.AlarmService) as AlarmManager;
    if (alarms is null) return;

    long triggerAt = (long)(when.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds;
    PendingIntent? pending = BuildPendingIntent(bebooName);
    if (pending is null) return;

    // Inexact on purpose. This wants to arrive "around then", and an exact alarm would need a
    // special permission on Android 12+ that a pet game has no business asking for.
    alarms.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAt, pending);
  }

  /// <summary>Called when the game is opened: whatever was pending is no longer true.</summary>
  public void CancelPending()
  {
    _manager?.Cancel(NotificationId);
    var alarms = _context.GetSystemService(Context.AlarmService) as AlarmManager;
    PendingIntent? pending = BuildPendingIntent(null, createOnly: false);
    if (alarms is not null && pending is not null) alarms.Cancel(pending);
  }

  private PendingIntent? BuildPendingIntent(string? bebooName, bool createOnly = true)
  {
    var intent = new Intent(_context, typeof(NotificationReceiver));
    if (bebooName is not null) intent.PutExtra(NotificationReceiver.BebooNameExtra, bebooName);
    var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
    if (!createOnly) flags |= PendingIntentFlags.NoCreate;
    return PendingIntent.GetBroadcast(_context, NotificationId, intent, flags);
  }

  /// <summary>Posts the notification itself. Called by the receiver when the alarm fires.</summary>
  public void Post(string bebooName)
  {
    if (_manager is null) return;
    // If the alarm slipped into the quiet hours anyway - the device was asleep, the alarm was
    // deferred - say nothing rather than wake somebody at 3am.
    if (IsQuietHour(DateTime.Now)) return;

    var open = new Intent(_context, typeof(MainActivity));
    open.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
    var pending = PendingIntent.GetActivity(
        _context, 0, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

    var notification = new Notification.Builder(_context, ChannelId)
        .SetContentTitle($"{bebooName} misses you")
        .SetContentText($"{bebooName} has been on its own for a while.")
        .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
        .SetContentIntent(pending)
        .SetAutoCancel(true)
        .Build();

    _manager.Notify(NotificationId, notification);
  }
}

/// <summary>Wakes up when the alarm fires and asks <see cref="BebooNotifier"/> to post.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class NotificationReceiver : BroadcastReceiver
{
  public const string BebooNameExtra = "beboo-name";

  public override void OnReceive(Context? context, Intent? intent)
  {
    if (context is null) return;
    string name = intent?.GetStringExtra(BebooNameExtra) ?? "Your beboo";
    new BebooNotifier(context).Post(name);
  }
}
