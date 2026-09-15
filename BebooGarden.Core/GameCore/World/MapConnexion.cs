using System;
using System.Numerics;

namespace BebooGarden.GameCore.World;

// The name is resolved on every read rather than captured: the maps are built once, in a static
// constructor, but the player can change the game's language at any time afterwards.
public class MapConnexion(Vector3 position, MapPreset mapPreset, Func<string> name)
{

  public Vector3 Position { get; set; } = position;
  public MapPreset MapPreset = mapPreset;
  public string Nme => name();
  public Map Map => Map.Maps[MapPreset];
}
