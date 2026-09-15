using BebooGarden.GameCore;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.Audio;
using System.Collections.Generic;
using System.Numerics;

namespace BebooGarden.GameCore.Item;

public class SnowBall : Item
{
  private readonly Vector3[] DIRECTIONS = [new(0, 1, 0), new(1, 0, 0), new(-1, 0, 0), new(0, -1, 0)];
  private Vector3? position;
  public SnowBall()
  {
    MoveBehaviour = new(100, 100, true);
  }
  public Vector3? Direction { get; set; }
  private TimedBehaviour MoveBehaviour { get; set; }
  public override string Name => BebooText.snowball_name;
  public override string Description => BebooText.snowball_name;
  public override Vector3? Position
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
        Vector3 newPos = ClampToOwnMap(value.Value, out bool hitWall);
        if (hitWall)
        {
          GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.WallSound, newPos);
          Direction = null;
        }
        position = newPos;
      }
    }
  } // position null=in inventory
  public override bool IsTakable { get; set; } = true;
  public override bool IsWaterProof { get; set; } = false;
  public override Channel? Channel { get; set; }
  public override int Cost { get; set; } = -1;
  /// <summary>
  /// The fluff is warm and indoors, and nothing there can be won or lost. A snowball carried in
  /// from the snow has to stay in the bag.
  /// </summary>
  public override string? WhyItCannotGoOn(World.Map map)
      => map.Preset == World.MapPreset.fluff ? BebooText.snowball_toowarm : null;

  public override void Take()
  {
    // A snowball rolls for as long as it has a direction. Taking one mid roll and putting it down
    // somewhere else used to have it shoot off the moment it landed.
    Direction = null;
    base.Take();
  }

  public override void Action()
  {
    GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemSnowBallKickSound, Position.Value);
    Direction = DIRECTIONS[GameHost.Current.Random.Next(DIRECTIONS.Length)];
  }
  public override void BebooAction(Beboo beboo)
  {
    base.BebooAction(beboo);
    Action();
  }
  public override void PlaySound() { }
  public override void Update()
  {
    base.Update();
    if (MoveBehaviour.ItsTime())
    {
      if (Direction == null || Position == null) return;
      Position += Direction;
      GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.BebooStepSnowSound, Position.Value);
      if (GameHost.Current.Map != null)
      {
        List<Item> snowBalls = GameHost.Current.Map.Items.FindAll(x => x is SnowBall);
        foreach (SnowBall otherSnowBall in snowBalls)
        {
          if (otherSnowBall.Direction == null && Util.IsInSquare(Position.Value, otherSnowBall.Position.Value, 1))
          {
            otherSnowBall.Action();
          }
        }
      }
      if (GameHost.Current.Random.Next(8) == 1) Direction = null;
      MoveBehaviour.Done();
    }
  }
}
