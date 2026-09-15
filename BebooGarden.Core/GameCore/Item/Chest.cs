using BebooGarden.GameCore;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

public class Chest : Item
{
  public override string Name => BebooText.chest_name;
  public override string Description => BebooText.chest_description;
  public override Vector3? Position { get; set; } // position null=in inventory
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }
  public override int Cost { get; set; } = 5;
  public override void Action()
  {
    GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemChestOpenSound, (Vector3)Position);
    //var beboo = GameHost.Current.ChooseBeboo();
    GameHost.Current.CurrentPlayingMiniGame = new Minigame.memory.Memory(GameHost.Current.SoundSystem.Volume);
    GameHost.Current.CurrentPlayingMiniGame.Start();
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
  }
  public override void PlaySound()
  {
    if (Position == null || !(GameHost.Current.Map?.Items.Contains(this) ?? false)) return;
    Channel = GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemChestSound, (Vector3)Position);
  }
}
