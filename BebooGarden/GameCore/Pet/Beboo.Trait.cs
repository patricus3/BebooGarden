using System;

namespace BebooGarden.GameCore.Pet;

/// <summary>
/// A beboo's temperament. Drawn at random when it hatches and kept for life, so two beboos of the
/// same colour still behave differently.
///
/// Nothing ever says out loud what a beboo is. You are meant to work out that this one always ends
/// up underfoot and that one sleeps through everything, the way you would with a real animal, and
/// being told would spoil exactly the thing it is for.
/// </summary>
public enum Trait
{
  /// <summary>Goes in the water sooner and shrugs off most frights.</summary>
  Brave,

  /// <summary>Wary of water, and stays rattled for a long time after a scare.</summary>
  Timid,

  /// <summary>Talks constantly.</summary>
  Chatty,

  /// <summary>Wants to be near the others, and melts sooner when stroked.</summary>
  Cuddly,

  /// <summary>Goes to bed early and sleeps through almost anything.</summary>
  Dreamy,

  /// <summary>Cannot leave an item alone, and is always off somewhere.</summary>
  Playful,
}

public partial class Beboo
{
  /// <summary>This beboo's temperament. Set once, at hatching, and saved with it.</summary>
  public Trait Trait { get; set; }

  public static Trait RandomTrait() =>
      (Trait)Game1.Instance.Random.Next(Enum.GetValues<Trait>().Length);

  /// <summary>
  /// Multiplier on the gap between idle noises. A chatty beboo fills the garden; a dreamy one is
  /// heard from now and then.
  /// </summary>
  private float ChatterRate => Trait switch
  {
    Trait.Chatty => 0.6f,
    Trait.Dreamy => 1.4f,
    _ => 1f,
  };

  /// <summary>Multiplier on how long it stays put before wandering off again.</summary>
  private float RestlessRate => Trait == Trait.Playful ? 0.7f : 1f;

  /// <summary>How long a fright lasts before it settles.</summary>
  private int PanikMs => Trait switch
  {
    Trait.Brave => 3000,
    Trait.Timid => 8000,
    _ => 5000,
  };

  /// <summary>
  /// The swim level at or below which water is still frightening. A brave beboo only panics the
  /// very first time; a timid one needs a good deal more practice before it stops.
  /// </summary>
  private int NerveInWater => Trait switch
  {
    Trait.Brave => 0,
    Trait.Timid => 2,
    _ => 1,
  };

  /// <summary>Share of its energy at which it takes itself to bed.</summary>
  public float SleepyAt => Trait == Trait.Dreamy ? 0.35f : SLEEPYAT;

  /// <summary>How many strokes before it melts.</summary>
  private int PetsBeforeDelight => Trait == Trait.Cuddly ? 3 : 4;
}
