using BebooGarden.GameCore.Pet;

namespace BebooGarden.Save;

public class BebooInfo(string name, float age, int happiness, float energy, int swimLevel, float voice, BebooType bebooType)
{
  public string Name { get; set; } = name;
  public float Age { get; set; } = age;
  public int Happiness { get; set; } = happiness;
  public float Energy { get; set; } = energy;
  public int SwimLevel { get; set; } = swimLevel;
  public float Voice { get; set; } = voice;
  public bool KnowItsName { get; set; } = false;
  public BebooType BebooType { get; set; }=bebooType;

  /// <summary>Id of the mod creature this beboo is, when it came from a mod.</summary>
  public string? ModCreature { get; set; }

  /// <summary>
  /// This beboo's temperament. Nullable on purpose: a save written before traits existed has none,
  /// and null means draw one rather than quietly making every old beboo the first of the list.
  /// </summary>
  public GameCore.Pet.Trait? Trait { get; set; }
}
