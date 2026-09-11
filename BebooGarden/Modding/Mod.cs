using System.Collections.Generic;
using System.Reflection;

namespace BebooGarden.Modding;

/// <summary>One creature a mod adds. Its id is what a beboo stores and what its voice folder is called.</summary>
public class ModCreature
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;

  /// <summary>Set when the mod is loaded; not part of the manifest.</summary>
  public string ModId { get; set; } = string.Empty;

  /// <summary>Where this creature's voice folders live.</summary>
  public string VoiceFolder { get; set; } = string.Empty;
}

/// <summary>
/// A folder under mods/ with a mod.json in it. A mod adds creatures, switches on behaviour the game
/// already knows how to do, or both. Unknown fields are ignored rather than rejected, so a mod
/// written for a later version still loads what this version understands.
/// </summary>
public class Mod
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public List<ModCreature> Creatures { get; set; } = [];

  /// <summary>Filled in on load, not read from the manifest.</summary>
  public string Folder { get; set; } = string.Empty;

  /// <summary>
  /// The assembly this mod's manifest was read out of, when the mod is a single dll. Loaded to get
  /// at the manifest, but nothing in it is constructed until the mod is switched on.
  /// </summary>
  public Assembly? Assembly { get; set; }

  /// <summary>
  /// What the mod list calls this mod. A mod that ships with the game is named in the game's own
  /// translations so that it is not stuck in English; anything else uses its manifest.
  /// </summary>
  public string DisplayName =>
      Content.BebooText.ResourceManager.GetString("mods." + Id, Content.BebooText.Culture)
      ?? (string.IsNullOrWhiteSpace(Name) ? Id : Name);
}
