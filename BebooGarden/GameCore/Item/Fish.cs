using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using FmodAudio;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

internal class Fish : Item
{
  /// <summary>
  /// How loud one fish's swimming loop is. Ten of them share the beach at once, each looping
  /// without pause, so at the full volume every other sound was playing under a wall of fish. The
  /// water they are in runs at 0.1 to 0.5 and the trees at 0.2; this sits with them.
  /// </summary>
  private const float MOVELOOPVOLUME = 0.15f;

  private System.Numerics.Vector3? position;
  public Fish()
  {
    MoveBehaviour = new(100, 150, true);
    ChangeDestinationBehaviour = new(10000, 30000, true);
  }
  private System.Numerics.Vector3? Destination { get; set; }
  private TimedBehaviour MoveBehaviour { get; set; }
  private TimedBehaviour ChangeDestinationBehaviour { get; set; }
  public override string Name => BebooText.fish_name;
  public override string Description => BebooText.fish_description;
  public override System.Numerics.Vector3? Position
  {
    get => position;
    set
    {
      if (value == null)
      {
        position = value;
      }
      else if (Game1.Instance.Map != null)
      {
        System.Numerics.Vector3 newPos = Game1.Instance.Map.Clamp(value.Value);
        if (newPos != value)
        {
          Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.WallSound, newPos);
        }
        position = newPos;
        MoveLoopTo(newPos);
      }
      else
      {
        position = value;
      }
    }
  } // position null=in inventory
  public override bool IsTakable { get; set; } = false;
  public override bool IsWaterProof { get; set; } = true;
  public override Channel? Channel { get; set; }
  public override void Action()
  {
    Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.FishFleeSound, Position.Value);
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    Action();
  }
  public override void PlaySound() { }

  /// <summary>
  /// Takes the swimming loop with the fish. It used to be started once where the fish appeared and
  /// left there, so a fish you could hear right beside you was often nowhere near.
  /// </summary>
  private void MoveLoopTo(System.Numerics.Vector3 newPos)
  {
    try
    {
      if (Channel != null && Channel.IsPlaying)
        Channel.Set3DAttributes(newPos + new System.Numerics.Vector3(0, 0, -2), default, default);
    }
    catch (FmodException)
    {
      // The loop has ended under us; the next update starts a new one.
      Channel = null;
    }
  }

  public override void Pause()
  {
    base.Pause();
    if (Channel != null && Channel.IsPlaying) Channel.Paused = true;
    MoveBehaviour.Stop();
    ChangeDestinationBehaviour.Stop();
  }
  public override void Unpause()
  {
    base.Unpause();
    if (Channel != null && Channel.IsPlaying) Channel.Paused = false;
    MoveBehaviour.Start();
    ChangeDestinationBehaviour.Start();
  }
  public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
  {
    base.Update(gameTime);
    if (Channel == null && Position != null)
    {
      Channel channel = Game1.Instance.SoundSystem.PlaySoundAtPosition(Game1.Instance.SoundSystem.FishMoveSound, Position.Value);
      channel.Volume = MOVELOOPVOLUME;
      Channel = channel;
    }
    if (ChangeDestinationBehaviour.ItsTime())
    {
      if (Position == null) return;
      Destination = Game1.Instance.Map.GenerateRandomUnoccupedPosition(onlyWater: true);
      ChangeDestinationBehaviour.Done();
    }
    if (MoveBehaviour.ItsTime())
    {
      if (Destination == null || Position == null) return;
      Vector3 direction = Destination.Value - Position.Value;
      Vector3 directionNormalized = direction;
      directionNormalized.X = Math.Sign(directionNormalized.X);
      directionNormalized.Y = Math.Sign(directionNormalized.Y);
      if (Game1.Instance.Map.IsInWater(Position.Value + direction))
      {
        Position += directionNormalized;
      }
      else
      {
        ChangeDestinationBehaviour.Start();
      }
      /*
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
      */
      MoveBehaviour.Done();
    }
  }
}