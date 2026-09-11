using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using CrossSpeak;
using FmodAudio;
using Microsoft.Xna.Framework;
using System;
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
    set => _position = value == null ? null : Game1.Instance.Map?.Clamp(value.Value) ?? value;
  }

  private bool ReadyToHug => (DateTime.Now - _lastHug).TotalMilliseconds > HUGCOOLDOWNMS;

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
    if (MurmurBehaviour.ItsTime())
    {
      Game1.Instance.SoundSystem.PlayFluffBallSound(
          Game1.Instance.SoundSystem.FluffBallMurmurSounds, this, 0.35f);
      MurmurBehaviour.Done();
    }
    if (DriftBehaviour.ItsTime())
    {
      _drift = Game1.Instance.Map?.GenerateRandomUnoccupedPosition();
      DriftBehaviour.Done();
    }
    if (MoveBehaviour.ItsTime())
    {
      Drift();
      HugWhateverIsHere();
      MoveBehaviour.Done();
    }
  }

  private void Drift()
  {
    if (_drift == null || Position == null) return;
    Vector3 step = _drift.Value - Position.Value;
    if (Math.Abs(step.X) < 1 && Math.Abs(step.Y) < 1) return;
    step.X = Math.Sign(step.X);
    step.Y = Math.Sign(step.Y);
    Position += step;
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
