namespace BebooGarden.GameCore.Pet;

/// <summary>
/// How rested a beboo is, in the order it runs down. Measured against the beboo's own maximum
/// rather than against fixed numbers, since that maximum grows as it gets older: half a tank means
/// the same thing to a hatchling and to an adult.
/// </summary>
public enum EnergyStage
{
  Exhausted,
  Tired,
  LittleTired,
  Ok,
  Energetic,
}
