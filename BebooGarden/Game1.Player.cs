using BebooGarden.Content;
using BebooGarden.GameCore.Item;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.Interface.UI;
using BebooGarden.Minigame;
using BebooGarden.Modding;
using BebooGarden.MiniGames;
using BebooGarden.Save;
using BebooGarden.UI;
using CrossSpeak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BebooGarden;

public partial class Game1
{
  public void MoveOf(Vector3 movement)
  {
    if (Map == null) return;
    System.Numerics.Vector3 newPos = Map.Clamp(PlayerPosition + movement);
    if (newPos != PlayerPosition + movement)
      SoundSystem.System.PlaySound(SoundSystem.WallSound);
    else
    {
      if (Map.IsInWater(newPos))
      {
        SoundSystem.PlayWaterCursorSound();
      }
      else
      {
        SoundSystem.PlayCursorSound();
      }
    }
    PlayerPosition = newPos;
    var connexion = Map.GetConnexionArroundPosition(PlayerPosition);
    SoundSystem.MovePlayerTo(newPos);
    if (Save.Flags.UnlockShop && (Map?.IsArroundShop(PlayerPosition) ?? false)) CrossSpeakManager.Instance.Output(BebooText.shop);
    else if (Map?.IsArroundRaceGate(PlayerPosition) ?? false)
      CrossSpeakManager.Instance.Output(String.Format(BebooText.competition_gate, Competition.GetRemainingTriesToday()));
    else if (connexion?.Map.IsUnlocked() ?? false)
    {
      CrossSpeak.CrossSpeakManager.Instance.Output(connexion.Nme);
    }
    SpeakObjectUnderCursor();
  }
  private void SpeakObjectUnderCursor()
  {
    TreeLine? treeLine = Map?.GetTreeLineAtPosition(PlayerPosition);
    Item? item = Map?.GetItemArroundPosition(PlayerPosition);
    var beboosUnderCursor = BeboosUnderCursor(1);
    if (beboosUnderCursor.Count > 0)
      foreach (var beboo in beboosUnderCursor) ScreenReader.Output(beboo.Name);
    if (treeLine != null)
    {
      if (treeLine.Fruits == treeLine.FruitPerHour)
        CrossSpeakManager.Instance.Output(BebooText.trees_full);
      else if (treeLine.Fruits == 0)
        CrossSpeakManager.Instance.Output(BebooText.trees_empty);
      else if (treeLine.Fruits <= treeLine.FruitPerHour / 2)
        CrossSpeakManager.Instance.Output(BebooText.trees_soonempty);
      else if (treeLine.Fruits >= treeLine.FruitPerHour / 2)
        CrossSpeakManager.Instance.Output(BebooText.trees_soonfull);
    }
    else if (item != null) CrossSpeakManager.Instance.Output(item.Name);
  }

  private void ShakeOrPetAtPlayerPosition()
  {
    TreeLine? treeLine = Map?.GetTreeLineAtPosition(PlayerPosition);
    Beboo? bebooUnderCursor = BebooUnderCursor();
    if (treeLine != null)
    {
      FruitSpecies? dropped = treeLine.Shake();
      if (dropped != null)
        if (Save.FruitsBasket != null)
          if (Save.FruitsBasket.TryGetValue(dropped.Value, out _)) Save.FruitsBasket[dropped.Value]++;
          else Save.FruitsBasket[dropped.Value] = 1;
    }
    else if (bebooUnderCursor != null)
    {
      bebooUnderCursor.GetPetted();
    }
  }
  public Beboo? BebooUnderCursor()
  {
    foreach (Beboo beboo in Map.Beboos)
    {
      if (Util.IsInSquare(beboo.Position, PlayerPosition, 1)) return beboo;
    }
    return null;
  }
  public List<Beboo> BeboosUnderCursor(int squareSide = 1)
  {
    List<Beboo> beboos = new();
    foreach (Beboo beboo in Map.Beboos)
    {
      if (Util.IsInSquare(beboo.Position, PlayerPosition, squareSide)) beboos.Add(beboo);
    }
    return beboos;
  }
  private void SayTickets()
  {
    CrossSpeakManager.Instance.Output(String.Format(BebooText.tickets, Save.Tickets));
  }
  private void SayBasketState()
  {
    var fruits = 0;
    foreach (var fruistCount in Save.FruitsBasket.Values) fruits += fruistCount;
    if (Save.FruitsBasket != null) CrossSpeakManager.Instance.Output(String.Format(BebooText.ui_basket, fruits));
  }
  /// <summary>
  /// Picks something up off the ground, unless a mod wants to handle it instead. A mod that takes
  /// charge settles it in its own time, which may be after asking the player something, so there
  /// is nothing to do here once one has.
  /// </summary>
  private void TakeFromGround(Item item)
  {
    if (ModHost.Instance.PickupHandled(item, item.Take)) return;
    item.Take();
  }

  private void TryPutItemInHand()
  {
    Item? item = ItemInHand;
    if (item == null) return;
    string? refusal = Map != null ? item.WhyItCannotGoOn(Map) : null;
    if (refusal != null)
    {
      RefuseToPutDown(item, refusal);
      return;
    }
    bool inWater = Map?.IsInWater(PlayerPosition) ?? false;
    if (inWater && !item.IsWaterProof)
    {
      RefuseToPutDown(item, BebooText.ui_warningwater);
      return;
    }
    // AddItem refuses a tree line. Ignoring that used to announce the drop, take the item out of
    // the bag and add it nowhere, which quietly destroyed it.
    if (Map?.AddItem(item, PlayerPosition) != true)
    {
      RefuseToPutDown(item, BebooText.ui_cantputhere);
      return;
    }
    CrossSpeakManager.Instance.Output(String.Format(BebooText.ui_itemput, item.Name));
    SoundSystem.System.PlaySound(inWater ? SoundSystem.ItemPutWaterSound : SoundSystem.ItemPutSound);
    Inventory.Remove(item);
    ItemInHand = null;
  }

  /// <summary>
  /// Says why the item cannot go here and puts it back in the bag. Keeping hold of it would leave
  /// the space bar stuck on retrying the drop, with no way to do anything else.
  /// </summary>
  private void RefuseToPutDown(Item item, string reason)
  {
    SoundSystem.System.PlaySound(SoundSystem.WarningSound);
    CrossSpeakManager.Instance.Output(reason);
    CrossSpeakManager.Instance.Output(String.Format(BebooText.ui_itembacktobag, item.Name));
    ItemInHand = null;
  }
  private void Whistle()
  {
    SoundSystem.System.Get3DListenerAttributes(0, out Vector3 currentPosition, out _, out _, out _);
    SoundSystem.Whistle();
    if (Map == null) return;
    // A copy: waking a beboo can move it between maps, and the list would be changing underneath.
    foreach (Beboo beboo in Map.Beboos.ToList())
    {
      if (Map.Beboos.Count > 1 && Random.Next(2) != 1) continue;
      // The delay is drawn here rather than inside the callback: Random is shared and not safe to
      // use from several threads at once.
      beboo.Later(Random.Next(1000, 2000), () => beboo.WakeUp());
      beboo.Destination = currentPosition;
    }
  }
  public void GainTicket(int amount)
  {
    if (amount > 0)
    {
      Save.Tickets += amount;
      CrossSpeakManager.Instance.Output(String.Format(BebooText.gainticket, amount));
      SoundSystem.System.PlaySound(SoundSystem.MenuOk2Sound);
      if (!Save.Flags.UnlockShop && Map != Map.SnowyRace && Map != Map.BasicRace)
      {
        Save.Flags.UnlockShop = true;
        SoundSystem.System.PlaySound(SoundSystem.JingleComplete);
        new TalkDialog(BebooText.shopunlock)
          .Show();
      }
    }
  }
}
