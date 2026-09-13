using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

public class Bubble : Item
{
  private System.Numerics.Vector3? position;
  public Bubble()
  {
    MoveBehaviour = new(1200, 1500, false);
    SlowDriftBehaviour = new(20000, 40000, false);
  }
  private System.Numerics.Vector3? Direction { get; set; }
  private TimedBehaviour MoveBehaviour { get; set; }
  private TimedBehaviour SlowDriftBehaviour { get; set; }
  public override string Name => BebooText.bubble_name;
  public override string Description => BebooText.bubble_description;
  public override System.Numerics.Vector3? Position
  {
    get => position;
    set
    {
      if (value == null)
      {
        position = value;
      }
      else
      {
        System.Numerics.Vector3 newPos = ClampToOwnMap(value.Value, out bool hitWall);
        if (hitWall)
        {
          Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.WallSound, newPos);
          Direction = null;
        }
        position = newPos;
      }
    }
  } // position null=in inventory
  // Bubbles can be caught and carried. They are waterproof, so one will sit anywhere you put it,
  // including a garden that has never seen a bubble before.
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }

  public override void Take()
  {
    // Caught, not knocked: a bubble in the bag has no drift left over to carry out of it.
    Direction = null;
    base.Take();
  }

  public override void Action()
  {
    Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.BubbleSounds, Position.Value);
    if (Game1.Instance.Random.Next(5) == 1)
    {
      Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.BubblePopSound, Position.Value);
      Game1.Instance.Map?.Items.Remove(this);
      MoveBehaviour.Stop();
      SlowDriftBehaviour.Stop();
    }
    else Direction = Util.DIRECTIONS[Game1.Instance.Random.Next(Util.DIRECTIONS.Length)];
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    Action();
  }
  public override void PlaySound() { }
  public override void Pause()
  {
    base.Pause();
    MoveBehaviour.Stop();
    SlowDriftBehaviour.Stop();
  }
  public override void Unpause()
  {
    base.Unpause();
    MoveBehaviour.Start();
    SlowDriftBehaviour.Start();
  }
  public override void Update(GameTime gameTime)
  {
    base.Update(gameTime);
    if (MoveBehaviour.ItsTime())
    {
      if (Direction == null || Position == null) return;
      Position += Direction;
      Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.BubbleSounds, Position.Value);
      if (Game1.Instance.Map != null)
      {
        List<Item> bubbles = Game1.Instance.Map.Items.FindAll(x => x is Bubble);
        foreach (Bubble otherBubble in bubbles)
        {
          if (otherBubble.Direction == null && Util.IsInSquare(Position.Value, otherBubble.Position.Value, 1))
          {
            otherBubble.Action();
          }
        }
      }
      if (Game1.Instance.Random.Next(8) == 1) Direction = null;
      MoveBehaviour.Done();
    }
    if (SlowDriftBehaviour.ItsTime()) {
      if (Direction != null || Position == null) return;
      Direction = Util.DIRECTIONS[Game1.Instance.Random.Next(Util.DIRECTIONS.Length)];
      Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.BubbleUpSound, Position.Value);
      SlowDriftBehaviour.Done();
    }
  }
}
