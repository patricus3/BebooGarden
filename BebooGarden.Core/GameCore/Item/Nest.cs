using BebooGarden.GameCore;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

/// <summary>
/// A bed for a beboo. Sleeping in one is worth more than sleeping on bare grass, and a sleepy
/// beboo will walk over to it on its own, which gives the garden somewhere that counts as home.
/// </summary>
public class Nest : Item
{
  /// <summary>How much faster a beboo recovers asleep in a nest.</summary>
  public const float RECOVERYBONUS = 1.7f;

  public override string Name => BebooText.nest_name;
  public override string Description => BebooText.nest_description;
  public override Vector3? Position { get; set; } // position null=in inventory
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = false;
  public override Channel? Channel { get; set; }
  public override int Cost { get; set; } = 6;

  public override void Action() => PlaySound();

  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    // Bumping into a bed while tired is all the invitation a beboo needs.
    if (!beboo.Sleeping && beboo.Energy <= beboo.MaxEnergy * beboo.SleepyAt) beboo.GoAsleep();
    else PlaySound();
  }

  /// <summary>A soft rustle every few seconds, so the nest can be found by ear like any item.</summary>
  public override void PlaySound()
  {
    if (Position == null || !(GameHost.Current.Map?.Items.Contains(this) ?? false)) return;
    Channel = GameHost.Current.SoundSystem.PlaySoundAtPosition(
        GameHost.Current.SoundSystem.GrassSound, (Vector3)Position, -0.6);
  }
}
