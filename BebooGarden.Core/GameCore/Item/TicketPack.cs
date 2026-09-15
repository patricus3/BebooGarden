using BebooGarden.GameCore;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

public class TicketPack(int amount) : Item
{
  public override string Name => BebooText.ticketpack_name;
  public override string Description => BebooText.ticketpack_description;
  public override bool IsTakable { get; set; } = true;

  public override void Action()
  {
    Take();
  }
  public int Amount { get; set; } = amount;
  public override void PlaySound()
  {
    if (Position == null) return;
    Channel = GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemTicketPackSound, (Vector3)Position);
  }

  public override Vector3? Position { get; set; } // position null=in inventory
  public override void Take()
  {
    GameHost.Current.Map?.Items.Remove(this);
    Position = null;
    GameHost.Current.GainTicket(Amount);
  }
  public override void BebooAction(Beboo beboo) => Take();

}
