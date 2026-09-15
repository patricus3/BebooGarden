using System.Numerics;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;

namespace BebooGarden.GameCore.Item;

public class BouncingBoots : Item
{
  public override string Name => BebooText.boots_name;
  public override string Description => BebooText.boots_description;
  public override Vector3? Position { get; set; } // position null=in inventory
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = false;
  public override Channel? Channel { get; set; }
  public override int Cost { get; set; } = 8;
  public override void Action()
  {
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    beboo.BootsSlippedOn = true;
  }
  public override void PlaySound()
  {
  }
}
