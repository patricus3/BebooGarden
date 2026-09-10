using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BebooGarden.Modding;

/// <summary>
/// Finds and tracks mods in the mods/ folder next to the game.
///
/// Every discovered mod's voices are loaded whether or not it is enabled, because a mod whose
/// creature you already own cannot be turned off, so its sounds are needed regardless. Enabling is
/// therefore about whether new creatures from that mod can turn up, not about whether the files
/// are read.
/// </summary>
public static class ModManager
{
  public const string MODSFOLDER = "mods";
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
  /// Reads every mod folder. A mod with a broken manifest is skipped rather than allowed to stop
  /// the game starting: somebody else's file should not cost you your garden.
  /// </summary>
  public static void Discover()
  {
    All = [];
    if (!Directory.Exists(MODSFOLDER)) return;
    foreach (string folder in Directory.GetDirectories(MODSFOLDER).OrderBy(f => f))
    {
      string manifest = Path.Combine(folder, MANIFEST);
      if (!File.Exists(manifest)) continue;
      try
      {
        Mod? mod = JsonConvert.DeserializeObject<Mod>(File.ReadAllText(manifest));
        if (mod == null || string.IsNullOrWhiteSpace(mod.Id)) continue;
        if (All.Any(other => other.Id == mod.Id)) continue;
        mod.Folder = folder;
        mod.Creatures.RemoveAll(creature => string.IsNullOrWhiteSpace(creature.Id));
        foreach (ModCreature creature in mod.Creatures)
        {
          creature.ModId = mod.Id;
          creature.VoiceFolder = Path.Combine(folder, "creatures", creature.Id);
        }
        All.Add(mod);
      }
      catch (Exception)
      {
        // A mod that will not parse is simply not there.
      }
    }
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
