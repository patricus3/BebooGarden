using BebooGarden.GameCore;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

public class Duck : Item
{
  public override string Name => BebooText.duck_name;
  public override string Description => BebooText.duck_description;
  public override Vector3? Position { get; set; } // position null=in inventory
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }
  public override int Cost { get; set; } = 1;
  public override void Action()
  {
    PlaySound();
    if (GameHost.Current.Random.Next(101) == 1) GameHost.Current.GainTicket(1);
#if DEBUG
    GameHost.Current.GainTicket(1);
#endif
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    Action();
  }
  public override void PlaySound()
  {
    if (Position == null || !(GameHost.Current.Map?.Items.Contains(this) ?? false)) return;
    Channel = GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemDuckSound, (Vector3)Position);
  }
}
