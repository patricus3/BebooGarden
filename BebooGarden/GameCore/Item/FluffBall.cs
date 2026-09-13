using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using CrossSpeak;
using FmodAudio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3 = System.Numerics.Vector3;

namespace BebooGarden.GameCore.Item;

/// <summary>
/// A fluffball. They live in the fluff, they drift about very slowly, and whenever one ends up
/// next to anything warm it hugs it: you, a beboo, or another fluffball. That is the whole of it.
/// Nothing here can be won or lost, which is the point of the place.
/// </summary>
internal class FluffBall : Item
{
  private const int HUGCOOLDOWNMS = 6000;

  /// <summary>
  /// How long a fluffball will wait somewhere with nobody in it before going home. Short on purpose:
  /// three minutes of patience meant you never saw it happen.
  /// </summary>
  private const int LONELYTOOLONGMS = 45000;

  private Vector3? _position;
  private Vector3? _drift;
  private DateTime _lastHug = DateTime.MinValue;

  public FluffBall()
  {
    MoveBehaviour = new TimedBehaviour(1400, 2600, true);
    DriftBehaviour = new TimedBehaviour(7000, 15000, true);
    MurmurBehaviour = new TimedBehaviour(5000, 12000, true);
  }

  private TimedBehaviour MoveBehaviour { get; }
  private TimedBehaviour DriftBehaviour { get; }
  private TimedBehaviour MurmurBehaviour { get; }

  public override string Name => BebooText.fluffball_name;
  public override string Description => BebooText.fluffball_description;
  public override bool IsTakable { get; set; } = false;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }

  public override Vector3? Position
  {
    get => _position;
    set => _position = value == null ? null : ClampToOwnMap(value.Value, out _);
  }

  /// <summary>
  /// The name of the beboo this fluffball has decided belongs to it, once one does. A name rather
  /// than the beboo itself because items are written into the save, and holding the creature would
  /// drag the whole of it in there too.
  /// </summary>
  public string? FriendName { get; set; }

  /// <summary>The beboo it is attached to, wherever in the world that beboo currently is.</summary>
  private Beboo? Friend => FriendName == null ? null
      : Map.Maps.Values.SelectMany(map => map.Beboos)
          .FirstOrDefault(beboo => !beboo.Racer && beboo.Name == FriendName);

  private bool ReadyToHug => (DateTime.Now - _lastHug).TotalMilliseconds > HUGCOOLDOWNMS;

  private bool _wasAlone;
  private DateTime _aloneSince = DateTime.MinValue;

  /// <summary>
  /// Watches every fluffball on one map, and is called for every map from Map.Update rather than
  /// only for the one you are standing on. Items are updated on your map alone, so a fluffball left
  /// behind is not running at all: leaving it to notice its own loneliness meant nothing happened
  /// until you came back and stood there, which is exactly when it is not alone any more.
  ///
  /// Nobody to hug means no other fluffball here and no beboo either. You do not count - a hug from
  /// you is exactly what it wants, and a sad murmur across the garden is how it asks.
  /// </summary>
  public static void WatchTheLonely(Map map)
  {
    List<FluffBall> here = [.. map.Items.OfType<FluffBall>()];
    bool company = map.Beboos.Count > 0 || here.Count > 1;
    foreach (FluffBall ball in here)
    {
      if (company) ball._aloneSince = DateTime.MinValue;
      else if (ball._aloneSince == DateTime.MinValue) ball._aloneSince = DateTime.Now;
      else if ((DateTime.Now - ball._aloneSince).TotalMilliseconds >= LONELYTOOLONGMS) ball.GoHome(map);
    }
  }

  /// <summary>
  /// Back to the fluff. It keeps whoever it had chosen: this is going home to wait, not giving up
  /// on them.
  /// </summary>
  private void GoHome(Map from)
  {
    if (from == Map.Fluff) return;
    from.Items.Remove(this);
    // AddItem stamps the new map on it; the fallback has to do the same, or it would go on
    // clamping itself against the map it just left.
    if (!Map.Fluff.AddItem(this, new Vector3(0, 0, 0)))
    {
      Map.Fluff.Items.Add(this);
      OwnerMap = Map.Fluff;
    }
    _aloneSince = DateTime.MinValue;
    _wasAlone = false;
    MurmurBehaviour.MinMS = 5000;
    MurmurBehaviour.MaxMS = 12000;
    // Only worth saying where you could actually have heard it go.
    if (Game1.Instance.Map == from || Game1.Instance.Map == Map.Fluff)
      CrossSpeakManager.Instance.Output(BebooText.fluffball_goeshome);
  }

  /// <summary>
  /// Somewhere a fluffball will not go, whoever is asking. It is a ball of fluff: cold and wet are
  /// the two things it has no answer for.
  /// </summary>
  private static bool TooColdForFluff(Map map) =>
      map.Preset is MapPreset.snowy or MapPreset.snowyrace or MapPreset.underwater;

  /// <summary>
  /// Brings a fluffball along when the beboo it picked changes map. Having chosen somebody it goes
  /// where they go - except into the snow or under the water, where it waits instead.
  /// </summary>
  public static void FollowFriend(Beboo beboo, Map? from, Map to)
  {
    if (from == null || from == to) return;
    foreach (FluffBall ball in from.Items.OfType<FluffBall>()
        .Where(ball => ball.FriendName == beboo.Name).ToList())
    {
      if (TooColdForFluff(to))
      {
        CrossSpeakManager.Instance.Output(
            String.Format(BebooText.fluffball_staysbehind, beboo.Name));
        continue;
      }
      from.Items.Remove(ball);
      // Where the beboo itself arrives, and a spot both maps are certain to have.
      if (!to.AddItem(ball, new Vector3(0, 0, 0)))
      {
        to.Items.Add(ball);
        ball.OwnerMap = to;
      }
      CrossSpeakManager.Instance.Output(String.Format(BebooText.fluffball_follows, beboo.Name));
    }
  }

  /// <summary>Hugging one back.</summary>
  public override void Action()
  {
    if (Position == null) return;
    _lastHug = DateTime.Now;
    Game1.Instance.SoundSystem.PlayFluffBallSound(Game1.Instance.SoundSystem.FluffBallHugSounds, this);
    CrossSpeakManager.Instance.Output(BebooText.fluffball_hugyou);
  }

  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    if (beboo.Sleeping || !ReadyToHug) return;
    _lastHug = DateTime.Now;
    beboo.Happiness++;
    Game1.Instance.SoundSystem.PlayFluffBallSound(Game1.Instance.SoundSystem.FluffBallHugSounds, this);
    CrossSpeakManager.Instance.Output(String.Format(BebooText.fluffball_hugbeboo, beboo.Name));
    MaybeAttachTo(beboo);
  }

  /// <summary>
  /// Now and then a hug turns into something lasting and the fluffball picks its beboo. One each at
  /// most: a beboo who already has one is spoken for, and a fluffball only ever chooses once, so
  /// this stays a thing that happens to somebody rather than to everybody.
  /// </summary>
  private void MaybeAttachTo(Beboo beboo)
  {
    if (FriendName != null || beboo.Racer) return;
    if (Game1.Instance.Random.Next(6) != 0) return;
    if (Map.Maps.Values.SelectMany(map => map.Items).OfType<FluffBall>()
        .Any(ball => ball.FriendName == beboo.Name)) return;
    FriendName = beboo.Name;
    Game1.Instance.SoundSystem.PlayFluffBallSound(Game1.Instance.SoundSystem.FluffBallHugSounds, this);
    CrossSpeakManager.Instance.Output(String.Format(BebooText.fluffball_attached, beboo.Name));
  }

  public override void PlaySound() { }

  public override void Pause()
  {
    base.Pause();
    MoveBehaviour.Stop();
    DriftBehaviour.Stop();
    MurmurBehaviour.Stop();
  }

  public override void Unpause()
  {
    base.Unpause();
    MoveBehaviour.Start();
    DriftBehaviour.Start();
    MurmurBehaviour.Start();
  }

  public override void Update(GameTime gameTime)
  {
    if (Position == null) return;
    NoticeWhetherAlone();
    if (MurmurBehaviour.ItsTime())
    {
      // There is no sad fluffball sound, so a lonely one gets the same murmur with the life taken
      // out of it: lower, quieter, and further apart.
      Game1.Instance.SoundSystem.PlayFluffBallSound(
          Game1.Instance.SoundSystem.FluffBallMurmurSounds, this,
          _wasAlone ? 0.2f : 0.35f, _wasAlone ? 0.75f : 1f);
      MurmurBehaviour.Done();
    }
    if (DriftBehaviour.ItsTime())
    {
      _drift = WhereToDrift();
      DriftBehaviour.Done();
    }
    if (MoveBehaviour.ItsTime())
    {
      // One that has chosen a beboo keeps it in sight every step rather than waiting for the next
      // drift, so it really follows instead of trailing a long way behind.
      if (Friend != null) _drift = WhereToDrift();
      Drift();
      HugWhateverIsHere();
      MoveBehaviour.Done();
    }
  }

  /// <summary>
  /// Where it wants to be: its beboo, when it has one and that beboo is here and out of the water,
  /// and anywhere dry otherwise. It will not wade in after somebody.
  /// </summary>
  private Vector3? WhereToDrift()
  {
    Beboo? friend = Friend;
    if (friend != null && (Game1.Instance.Map?.Beboos.Contains(friend) ?? false)
        && !(Game1.Instance.Map?.IsInWater(friend.Position) ?? false))
      return friend.Position;
    return Game1.Instance.Map?.GenerateRandomUnoccupedPosition(excludeWater: true);
  }

  /// <summary>
  /// Says so when it is left with nobody, and says so again when somebody comes back. Only on the
  /// change: a fluffball repeating how sad it is would stop being sad and start being nagging.
  /// </summary>
  private void NoticeWhetherAlone()
  {
    // Whether it is alone was settled from the map, which is watched whether or not you are on it.
    // All this does is say so.
    bool alone = _aloneSince != DateTime.MinValue;
    if (alone == _wasAlone) return;
    _wasAlone = alone;
    MurmurBehaviour.MinMS = alone ? 9000 : 5000;
    MurmurBehaviour.MaxMS = alone ? 20000 : 12000;
    CrossSpeakManager.Instance.Output(
        alone ? BebooText.fluffball_alone : BebooText.fluffball_cheered);
    Game1.Instance.SoundSystem.PlayFluffBallSound(
        alone ? Game1.Instance.SoundSystem.FluffBallMurmurSounds
              : Game1.Instance.SoundSystem.FluffBallHugSounds,
        this, alone ? 0.2f : -1, alone ? 0.7f : 1f);
  }

  private void Drift()
  {
    if (_drift == null || Position == null) return;
    Vector3 step = _drift.Value - Position.Value;
    if (Math.Abs(step.X) < 1 && Math.Abs(step.Y) < 1) return;
    step.X = Math.Sign(step.X);
    step.Y = Math.Sign(step.Y);
    Vector3 next = Position.Value + step;
    // A soaked fluffball is not a fluffball any more.
    if (Game1.Instance.Map?.IsInWater(next) ?? false) return;
    Position = next;
  }

  /// <summary>Two fluffballs that meet hug each other, which is most of what you hear in here.</summary>
  private void HugWhateverIsHere()
  {
    if (Position == null || !ReadyToHug || Game1.Instance.Map == null) return;
    FluffBall? other = Game1.Instance.Map.Items.OfType<FluffBall>().FirstOrDefault(ball =>
        ball != this && ball.Position != null && ball.ReadyToHug
        && Util.IsInSquare(ball.Position.Value, Position.Value, 1));
    if (other == null) return;
    _lastHug = DateTime.Now;
    other._lastHug = DateTime.Now;
    Game1.Instance.SoundSystem.PlayFluffBallSound(Game1.Instance.SoundSystem.FluffBallHugSounds, this);
  }
}
