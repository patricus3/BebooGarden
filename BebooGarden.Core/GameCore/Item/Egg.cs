using BebooGarden.GameCore;
using System;
using System.Linq;
using System.Numerics;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;

namespace BebooGarden.GameCore.Item;

public class Egg : Item
{
  /// <summary>
  /// An egg knows its colour from the moment it exists, so that finding one, buying one or picking
  /// one up tells the player which beboo is on the way. An egg asked for with no colour, the ones
  /// the garden leaves lying about and the one in the shop, draws its own here rather than at
  /// hatching time, which used to leave the player holding an egg of no colour at all.
  /// </summary>
  public Egg(string color)
  {
    Color = color == "none" ? Util.RandomColor() : color;
  }

  public override string Name => String.Format(BebooText.egg_name, Util.LocalizedColor(Color));
  public override string Description => BebooText.egg_description;
  public override Vector3? Position { get; set; } // position null=in inventory
  public override bool IsTakable { get; set; } = false;
  public override int Cost { get; set; } = 20;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }
  public string Color { get; }

  public override void Action() => Hatch();
  public override void Take() => Hatch();
  public override void BebooAction(Beboo beboo) => Hatch();
  private void Hatch()
  {
    GameHost.Current.Map?.Items.Remove(this);
    SoundLoopBehaviour.Stop();
    BebooType bebooType = Util.GetBebooTypeByColor(Color);
    // Roughly one hatchling in three is a mod creature when any mod offering them is switched on.
    Modding.ModCreature? modCreature = null;
    var offered = Modding.ModManager.AvailableCreatures.ToList();
    if (offered.Count > 0 && GameHost.Current.Random.Next(3) == 0)
      modCreature = offered[GameHost.Current.Random.Next(offered.Count)];
    Sound cinematic;
    if (!GameHost.Current.SoundSystem.CinematicsHatch.TryGetValue(bebooType, out cinematic)) cinematic = GameHost.Current.SoundSystem.CinematicsHatch[BebooType.Base];
    GameHost.Current.SoundSystem.PlayCinematic(cinematic);
    string name = "";// NewBeboo.Run();
    int swimLevel = (GameHost.Current.Map?.IsInWater(Position ?? new(0, 0, 0)) ?? false) ? 5 : 0;
    var beboo = new Beboo(name, bebooType, 1, DateTime.MinValue, 3, 3, swimLevel, false, 1 + (GameHost.Current.Random.Next(4) / 10))
    {
      Position = this.Position ?? new(0, 0, 0),
      ModCreature = modCreature?.Id,
    };
    GameHost.Current.Map?.Beboos.Add(beboo);
    GameHost.Current.Ui.ShowNewBeboo(beboo);
    GameHost.Current.ChangeMapMusic();
  }

  public override void PlaySound()
  {
    if (Position == null) return;
    Channel = GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.EggKrakSounds, (Vector3)Position);
  }
}