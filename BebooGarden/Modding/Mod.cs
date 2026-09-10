using System.Collections.Generic;

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
/// A folder under mods/ with a mod.json in it. The manifest only describes creatures for now, but
/// unknown fields are ignored rather than rejected, so a mod written for a later version still
/// loads what this version understands.
/// </summary>
public class Mod
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public List<ModCreature> Creatures { get; set; } = [];

  /// <summary>Filled in on load, not read from the manifest.</summary>
  public string Folder { get; set; } = string.Empty;

  public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id : Name;
}
