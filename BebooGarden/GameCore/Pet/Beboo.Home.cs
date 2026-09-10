using BebooGarden.Content;
using BebooGarden.GameCore.Item;
using BebooGarden.GameCore.World;
using CrossSpeak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BebooGarden.GameCore.Pet;

public partial class Beboo
{
  /// <summary>How much faster beboos recover curled up against each other.</summary>
  private const float SNUGGLERECOVERY = 1.5f;
  /// <summary>
  /// A sleepy beboo will walk this far to sleep next to a friend or in a nest. Wide enough to
  /// cover most of a 40 by 40 garden: at eight tiles two wandering beboos were usually too far
  /// apart to ever find each other, and the pile almost never formed.
  /// </summary>
  private const int BEDSEARCHRANGE = 15;
  /// <summary>Happiness a beboo needs before it starts bringing things back for you.</summary>
  private const int PRESENTHAPPINESS = 6;

  private enum Errand { None, Fetching, Delivering }

  private Errand _errand = Errand.None;
  private FruitSpecies? _presentFruit;
  private Beboo? _snuggledWith;

  /// <summary>True while this beboo is off finding you something, so it is not sent elsewhere.</summary>
  public bool OnAnErrand => _errand != Errand.None;

  // ---------------------------------------------------------------- sleeping together

  /// <summary>Sleeping beboos close enough to be curled up against this one.</summary>
  private List<Beboo> SnugglingWith()
  {
    List<Beboo> around = Game1.Instance.Map?.GetBeboosArround(Position) ?? [];
    around.RemoveAll(beboo => beboo == this || !beboo.Sleeping);
    return around;
  }

  /// <summary>Checks for a nest specifically, rather than whatever item happens to be nearest.</summary>
  private bool NextToANest() => Game1.Instance.Map?.Items.OfType<Nest>().Any(nest =>
      nest.Position != null && Util.IsInSquare(nest.Position.Value, Position, 1)) ?? false;

  /// <summary>
  /// Energy gained by one tick of sleep. Sleeping alone on the grass is the baseline; a nest is
  /// better, and so is a friend to curl up against.
  /// </summary>
  private float SleepRecovery()
  {
    float recovery = MaxEnergy * RECOVERYPERTICK;
    if (NextToANest()) recovery *= Nest.RECOVERYBONUS;
    List<Beboo> snuggling = SnugglingWith();
    if (snuggling.Count > 0)
    {
      recovery *= SNUGGLERECOVERY;
      AnnounceSnuggle(snuggling[0]);
    }
    else _snuggledWith = null;
    return recovery;
  }

  /// <summary>Said once when a pile forms, not every time somebody breathes.</summary>
  private void AnnounceSnuggle(Beboo friend)
  {
    if (_snuggledWith == friend) return;
    _snuggledWith = friend;
    CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_snuggle, Name, friend.Name));
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooDelightSounds, this, true, 0.3f);
  }

  internal void ForgetSnuggling() => _snuggledWith = null;

  /// <summary>
  /// Somewhere better to sleep than right here: a nest, or a friend already asleep. Returns null
  /// when this beboo is already there, or when there is nothing worth the walk.
  /// </summary>
  private Vector3? BedWorthWalkingTo()
  {
    Map? map = Game1.Instance.Map;
    if (map == null) return null;
    // Already somewhere good: settle down here rather than set off again.
    if (NextToANest() || SnugglingWith().Count > 0) return null;
    Vector3? nest = map.Items.OfType<Nest>()
        .FirstOrDefault(n => n.Position != null && Util.IsInSquare(n.Position.Value, Position, BEDSEARCHRANGE))
        ?.Position;
    if (nest != null) return nest;
    Beboo? friend = map.Beboos.FirstOrDefault(other =>
        other != this && other.Sleeping
        && Util.IsInSquare(other.Position, Position, BEDSEARCHRANGE));
    return friend?.Position;
  }

  /// <summary>
  /// Called when a beboo gets sleepy. It heads for a nest or a friend if there is one nearby and
  /// it still has the energy for the walk; otherwise it settles down where it stands.
  /// </summary>
  internal void GoToBed()
  {
    Vector3? bed = Energy > 0 ? BedWorthWalkingTo() : null;
    if (bed != null)
    {
      Destination = bed;
      return;
    }
    GoAsleep();
  }

  // ---------------------------------------------------------------- presents

  /// <summary>
  /// A happy beboo trots off to find something and brings it back to you. Nothing is announced on
  /// the way out; the sound of it wandering off is the only hint, and the arrival is the moment.
  /// </summary>
  internal void StartErrand()
  {
    if (_errand != Errand.None || Sleeping || IsHeld || Racer) return;
    if (!Happy || Happiness < PRESENTHAPPINESS) return;
    Vector3? spot = Game1.Instance.Map?.GenerateRandomUnoccupedPosition(true);
    if (spot == null) return;
    _errand = Errand.Fetching;
    Destination = spot;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooFunSounds, this);
  }

  /// <summary>Advances the errand as the beboo reaches each end of it.</summary>
  internal void UpdateErrand()
  {
    if (_errand == Errand.None || Sleeping || IsHeld) return;
    if (Destination != null) return; // still walking
    if (_errand == Errand.Fetching)
    {
      // Mostly fruit, once in a while something shinier.
      _presentFruit = Game1.Instance.Random.Next(5) == 0
          ? null
          : Game1.Instance.Random.Next(4) == 0 ? FruitSpecies.Energetic : FruitSpecies.Normal;
      _errand = Errand.Delivering;
      Destination = Game1.Instance.PlayerPosition;
      Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooCuteSounds, this);
      return;
    }
    if (!Util.IsInSquare(Position, Game1.Instance.PlayerPosition, 1))
    {
      // You moved. Follow you rather than give up on it.
      Destination = Game1.Instance.PlayerPosition;
      return;
    }
    Deliver();
  }

  private void Deliver()
  {
    _errand = Errand.None;
    Happiness++;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooDelightSounds, this);
    if (_presentFruit == null)
    {
      CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_presentticket, Name));
      Game1.Instance.GainTicket(1);
      return;
    }
    FruitSpecies fruit = _presentFruit.Value;
    var basket = Game1.Instance.Save.FruitsBasket;
    if (basket != null) basket[fruit] = basket.GetValueOrDefault(fruit) + 1;
    Game1.Instance.SoundSystem.DropFruitSound(fruit);
    CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_present, Name, Util.Localized(fruit.ToString())));
  }

  internal void AbandonErrand() => _errand = Errand.None;
}
