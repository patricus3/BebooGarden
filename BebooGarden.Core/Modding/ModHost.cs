using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BebooGarden.Content;
using BebooGarden.GameCore.Item;
using BebooGarden.ModApi;

namespace BebooGarden.Modding;

/// <summary>
/// Loads the code that switched-on mods brought with them, and is the only thing that code can see
/// of the game.
///
/// Only enabled mods are loaded. Voices are read either way, because a beboo that already exists
/// needs its sounds whatever the checkbox says, but code decides how the game behaves, so it runs
/// only when the player has asked for it.
///
/// Everything a mod does is wrapped: a mod that throws is logged and dropped, and the garden opens
/// without it. Somebody else's bug should not cost a player their beboos.
/// </summary>
public class ModHost : IModHost
{
  private readonly List<Func<PickupRequest, bool>> _pickupHandlers = [];
  private readonly HashSet<string> _started = [];
  private string _currentModId = "?";

  public static ModHost Instance { get; } = new();

  public string GameVersion =>
      typeof(ModHost).Assembly.GetName().Version?.ToString() ?? "unknown";

  /// <summary>
  /// Finds and starts the code of every enabled mod. Called once the mod list has been answered
  /// and before the garden opens, on the game thread.
  /// </summary>
  public static void StartEnabledMods()
  {
    foreach (Mod mod in ModManager.All.Where(ModManager.IsEnabled))
      Instance.Start(mod);
  }

  private void Start(Mod mod)
  {
    // Safe to call again: switching a mod on from the main menu starts only what is not running.
    // Switching one off cannot unload it, which is why that needs a restart.
    if (!_started.Add(mod.Id)) return;
    foreach (Assembly assembly in Assemblies(mod))
    {
      try
      {
        foreach (Type type in assembly.GetExportedTypes())
        {
          if (!typeof(IBebooMod).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) continue;
          if (type.GetConstructor(Type.EmptyTypes) == null) continue;
          _currentModId = mod.Id;
          ((IBebooMod)Activator.CreateInstance(type)!).Start(this);
          Log($"started {type.FullName}");
        }
      }
      catch (Exception error)
      {
        _currentModId = mod.Id;
        Log($"could not start {assembly.GetName().Name}: {error.Message}");
      }
      finally
      {
        _currentModId = "?";
      }
    }
  }

  /// <summary>
  /// The code this mod brought. A single-file mod is already loaded, since that is where its
  /// manifest came from; a folder mod may have dlls sitting beside its mod.json.
  ///
  /// LoadFrom rather than a separate load context on purpose: a mod has to end up sharing our
  /// BebooGarden.ModApi types, or the interfaces it implements are not the ones we look for.
  /// </summary>
  private static IEnumerable<Assembly> Assemblies(Mod mod)
  {
    if (mod.Assembly != null) return [mod.Assembly];
    try
    {
      return Directory.GetFiles(mod.Folder, "*.dll", SearchOption.AllDirectories)
          .OrderBy(file => file)
          .Select(Assembly.LoadFrom)
          .ToList();
    }
    catch (Exception)
    {
      return [];
    }
  }

  public void Log(string message)
  {
    try
    {
      File.AppendAllText(GamePaths.CrashLog,
          $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  mod {_currentModId}: {message}{Environment.NewLine}");
    }
    catch (Exception)
    {
      // A log that cannot be written is not worth taking the game down for.
    }
  }

  public void Say(string text) => GameCore.Speech.Voice.Current.Say(text);

  public string? Text(string key) =>
      BebooText.ResourceManager.GetString(key, BebooText.Culture);

  public void AskYesNo(string question, Action<bool> answer)
  {
    Dictionary<string, bool> answers = new()
    {
      { BebooText.ui_yes, true },
      { BebooText.ui_no, false },
    };
    // No cancel button: the two answers cover it, and escaping out never calls back at all, which
    // leaves the request unsettled and so leaves the world untouched.
    GameCore.GameHost.Current.Ui.Choose<bool>(question, answers, answer, allowCancel: false);
  }

  public void OnPickingUp(Func<PickupRequest, bool> handler) => _pickupHandlers.Add(handler);

  /// <summary>
  /// Offers a pickup to the mods, newest first, and reports whether one of them took charge. The
  /// game does nothing further when it did: the mod settles the request in its own time.
  /// </summary>
  public bool PickupHandled(Item item, Action take)
  {
    if (_pickupHandlers.Count == 0) return false;
    PickupRequest request = new(item.Name, item is Egg, take);
    foreach (Func<PickupRequest, bool> handler in _pickupHandlers.ToList())
    {
      try
      {
        if (handler(request)) return true;
      }
      catch (Exception error)
      {
        Log($"threw while handling a pickup: {error.Message}");
      }
    }
    return false;
  }
}
