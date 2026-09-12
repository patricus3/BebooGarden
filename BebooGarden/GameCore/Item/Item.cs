using System;
using System.Numerics;
using BebooGarden.GameCore;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace BebooGarden.GameCore.Item;

public abstract class Item
{
  protected Item()
  {
    SoundLoopBehaviour = new TimedBehaviour(3000, 3000, true);
  }

  public virtual string Name { get;  }
  public virtual string Description { get; } = string.Empty;
  public abstract System.Numerics.Vector3? Position { get; set; } // position null = in inventory
  public virtual bool IsTakable { get; set; } = true;
  public virtual bool IsWaterProof { get; set; } = false;

  /// <summary>
  /// Why this item cannot be left on that map, or null when it can. Water is asked about
  /// separately through IsWaterProof; this is for a whole place an item has no business being in.
  /// </summary>
  public virtual string? WhyItCannotGoOn(World.Map map) => null;

  public virtual int Cost { get; set; } = 1;

  [JsonIgnore]
  public virtual Channel? Channel { get; set; }

  [JsonIgnore]
  public TimedBehaviour SoundLoopBehaviour { get; set; }

  public virtual void Action()
  {
  }

  public abstract void PlaySound();

  public virtual void Take()
  {
    Game1.Instance.Map?.Items.Remove(this);
    Position = null;
    Game1.Instance.SoundSystem.System.PlaySound(Game1.Instance.SoundSystem.ItemTakeSound);
    Game1.Instance.Inventory.Add(this);
    // The sound alone is eighty milliseconds long and easily lost under a map's ambience.
    CrossSpeak.CrossSpeakManager.Instance.Output(
        string.Format(Content.BebooText.ui_itemtake, Name));
  }

  public virtual void Buy()
  {
    if (Game1.Instance.Save.Tickets - Cost >= 0)
    {
      Game1.Instance.Save.Tickets -= Cost;
      Game1.Instance.SoundSystem.System.PlaySound(Game1.Instance.SoundSystem.ShopSound);
      Game1.Instance.Inventory?.Add(this);
    }
    else
    {
      Game1.Instance.SoundSystem.System.PlaySound(Game1.Instance.SoundSystem.WarningSound);
    }
  }

  public virtual void BebooAction(Beboo beboo)
  {
  }
  public virtual void Update(GameTime gameTime)
  {
    if (SoundLoopBehaviour.ItsTime())
    {
      PlaySound();
      SoundLoopBehaviour.Done();
    }
  }

  public virtual void Pause()
  {
    SoundLoopBehaviour?.Stop();
  }

  public virtual void Unpause()
  {
    SoundLoopBehaviour?.Start();
  }
}
