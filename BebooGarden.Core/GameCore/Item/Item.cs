using BebooGarden.GameCore.Speech;
using System;
using System.Numerics;
using BebooGarden.GameCore;
using BebooGarden.GameCore.Pet;
using FmodAudio;
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

  // What a kind of item is, rather than anything a particular one of them has done, so none of
  // these three are written to the save. They used to be, and a saved copy of a value is a value
  // that can no longer be changed: bubbles were made takable here and stayed unpickable forever,
  // because every bubble already in a save carried its own "not takable" and put it straight back
  // over the top on load. Reading them off the class every time is what makes a change to one of
  // them mean anything.
  [JsonIgnore]
  public virtual bool IsTakable { get; set; } = true;

  [JsonIgnore]
  public virtual bool IsWaterProof { get; set; } = false;

  [JsonIgnore]
  public virtual int Cost { get; set; } = 1;

  /// <summary>
  /// The map this item is lying on, or null while it is in the bag. Not saved: which map an item
  /// is on is already recorded by that map being the one whose list it is in, and it is set from
  /// that list on the way back in.
  /// </summary>
  [JsonIgnore]
  public World.Map? OwnerMap { get; set; }

  /// <summary>
  /// Why this item cannot be left on that map, or null when it can. Water is asked about
  /// separately through IsWaterProof; this is for a whole place an item has no business being in.
  /// </summary>
  public virtual string? WhyItCannotGoOn(World.Map map) => null;

  /// <summary>
  /// Keeps a moving item inside the map it is actually on, and says whether it just hit the edge.
  ///
  /// The items that drift about used to clamp against GameHost.Current.Map, which is the map the
  /// player is standing on, not the item's. Every item everywhere was therefore squashed into the
  /// bounds of whichever map you happened to be in - and since that happens on load too, and the
  /// smallest map is 24 by 24 against the garden's 60 by 60, one visit to the fluff was enough to
  /// crush the whole snowfield's worth of snowballs into a quarter of it and pile them against
  /// the edge. It only ever got worse, never better.
  /// </summary>
  protected System.Numerics.Vector3 ClampToOwnMap(System.Numerics.Vector3 value, out bool hitWall)
  {
    World.Map? map = OwnerMap ?? GameHost.Current.Map;
    if (map == null)
    {
      hitWall = false;
      return value;
    }
    System.Numerics.Vector3 clamped = map.Clamp(value);
    hitWall = clamped != value;
    return clamped;
  }

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
    GameHost.Current.Map?.Items.Remove(this);
    OwnerMap = null;
    Position = null;
    GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.ItemTakeSound);
    GameHost.Current.Inventory.Add(this);
    // The sound alone is eighty milliseconds long and easily lost under a map's ambience.
    Voice.Current.Say(
        string.Format(Content.BebooText.ui_itemtake, Name));
  }

  public virtual void Buy()
  {
    if (GameHost.Current.Save.Tickets - Cost >= 0)
    {
      GameHost.Current.Save.Tickets -= Cost;
      GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.ShopSound);
      GameHost.Current.Inventory?.Add(this);
    }
    else
    {
      GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.WarningSound);
    }
  }

  public virtual void BebooAction(Beboo beboo)
  {
  }
  public virtual void Update()
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
