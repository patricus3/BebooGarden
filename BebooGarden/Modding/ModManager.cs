using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BebooGarden.Modding;

/// <summary>
/// Finds and tracks mods, both the ones that came with the game and the ones the player added to
/// their own mods folder.
///
/// Every discovered mod's voices are loaded whether or not it is enabled, because a mod whose
/// creature you already own cannot be turned off, so its sounds are needed regardless. Enabling is
/// therefore about whether new creatures from that mod can turn up, not about whether the files
/// are read.
/// </summary>
public static class ModManager
{
  private const string MANIFEST = "mod.json";

  public static List<Mod> All { get; private set; } = [];

  /// <summary>Ids of mods the player has switched on. Persisted in the save.</summary>
  public static HashSet<string> Enabled { get; private set; } = [];

  public static bool Any => All.Count > 0;

  public static bool IsEnabled(Mod mod) => Enabled.Contains(mod.Id);

  /// <summary>Creatures the game may hand out to a new egg.</summary>
  public static IEnumerable<ModCreature> AvailableCreatures =>
      All.Where(IsEnabled).SelectMany(mod => mod.Creatures);

  public static IEnumerable<ModCreature> AllCreatures => All.SelectMany(mod => mod.Creatures);

  public static ModCreature? CreatureById(string? id) =>
      id == null ? null : AllCreatures.FirstOrDefault(creature => creature.Id == id);

  /// <summary>
  /// Finds every mod: a single dll dropped in with its manifest built into it, or a folder with a
  /// mod.json in it. One that will not parse is skipped rather than allowed to stop the game
  /// starting, because somebody else's file should not cost you your garden.
  /// </summary>
  public static void Discover()
  {
    All = [];
    foreach (string root in GamePaths.ModFolders)
    {
      foreach (string folder in Directory.GetDirectories(root).OrderBy(f => f))
        ReadFolder(folder);
      foreach (string file in Directory.GetFiles(root, "*.dll").OrderBy(f => f))
        ReadAssembly(file);
    }
  }

  /// <summary>Reads one mod folder, if it holds a manifest that parses.</summary>
  private static void ReadFolder(string folder)
  {
    string manifest = Path.Combine(folder, MANIFEST);
    if (!File.Exists(manifest)) return;
    try
    {
      Register(JsonConvert.DeserializeObject<Mod>(File.ReadAllText(manifest)), folder, null);
    }
    catch (Exception)
    {
      // A mod that will not parse is simply not there.
    }
  }

  /// <summary>
  /// Reads a mod that is one file: a dll carrying its own manifest as an embedded resource, so it
  /// installs by being dropped in rather than unpacked.
  ///
  /// Reading it means loading the assembly, which is why this only reads the manifest and stops.
  /// Nothing of the mod's is constructed or run until the player has switched it on.
  /// </summary>
  private static void ReadAssembly(string file)
  {
    try
    {
      Assembly assembly = Assembly.LoadFrom(file);
      string? resource = assembly.GetManifestResourceNames().FirstOrDefault(name =>
          name.Equals(MANIFEST, StringComparison.OrdinalIgnoreCase)
          || name.EndsWith("." + MANIFEST, StringComparison.OrdinalIgnoreCase));
      if (resource == null) return;
      using Stream? stream = assembly.GetManifestResourceStream(resource);
      if (stream == null) return;
      using StreamReader reader = new(stream);
      Register(JsonConvert.DeserializeObject<Mod>(reader.ReadToEnd()),
          Path.GetDirectoryName(file) ?? string.Empty, assembly);
    }
    catch (Exception)
    {
      // Not a mod, or not one this version can read.
    }
  }

  /// <summary>Takes a parsed manifest and makes a mod of it, if it is worth having.</summary>
  private static void Register(Mod? mod, string folder, Assembly? assembly)
  {
    if (mod == null || string.IsNullOrWhiteSpace(mod.Id)) return;
    // Ids have to be unique, so a mod the player installed themselves is ignored if one of the
    // same name already came with the game.
    if (All.Any(other => other.Id == mod.Id)) return;
    mod.Folder = folder;
    mod.Assembly = assembly;
    mod.Creatures.RemoveAll(creature => string.IsNullOrWhiteSpace(creature.Id));
    foreach (ModCreature creature in mod.Creatures)
    {
      creature.ModId = mod.Id;
      creature.VoiceFolder = Path.Combine(folder, "creatures", creature.Id);
    }
    All.Add(mod);
  }

  public static void SetEnabled(IEnumerable<string> ids)
  {
    Enabled = [.. ids.Where(id => All.Any(mod => mod.Id == id))];
  }

  public static void SetEnabled(Mod mod, bool enabled)
  {
    if (enabled) Enabled.Add(mod.Id);
    else Enabled.Remove(mod.Id);
  }

  /// <summary>
  /// A mod cannot be switched off while one of its creatures is alive somewhere in the save: doing
  /// so would leave a beboo with no voice and no way to get it back.
  /// </summary>
  public static bool CanDisable(Mod mod) => CreaturesInUse(mod).Count == 0;

  /// <summary>Names of this mod's creatures that the player currently owns.</summary>
  public static List<string> CreaturesInUse(Mod mod)
  {
    HashSet<string> ids = [.. mod.Creatures.Select(creature => creature.Id)];
    List<string> names = [];
    foreach (Map map in Map.Maps.Values)
      foreach (Beboo beboo in map.Beboos)
        if (beboo.ModCreature != null && ids.Contains(beboo.ModCreature) && !names.Contains(beboo.Name))
          names.Add(beboo.Name);
    return names;
  }
}
