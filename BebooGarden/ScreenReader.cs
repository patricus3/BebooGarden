using BebooGarden.GameCore.Speech;
using CrossSpeak;
using DavyKager;
using System;
using System.IO;

namespace BebooGarden;

/// <summary>
/// The Windows head's voice: the player's screen reader through CrossSpeak, or SAPI when no screen
/// reader is running. The shared code never names any of that - it asks <see cref="Voice"/>, and
/// <see cref="Load"/> is what puts this behind it.
/// </summary>
internal sealed class ScreenReaderSpeech : ISpeech
{
  public bool Say(string text, bool interrupt = false) => ScreenReader.Output(text, interrupt);
}

internal class ScreenReader
{
  internal static bool Output(string text, bool interrupt = false)
  {
    if (text.Length == 0) return false;

    bool success = CrossSpeakManager.Instance.Output(text, interrupt);
    return success;
  }

  internal static void Load()
  {
    // Append accessibility deps (e.g. Tolk, NVDA drivers, etc.) to PATH
    string? path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
    //var accessibilityAssembliesDir = Path.Combine("%appdata%", "XIVLauncher", "installedPlugins", name, version);
    string accessibilityAssembliesDir = Path.Combine("lib", "screen-reader-libs", "windows");
    path += $";{accessibilityAssembliesDir}";
    string fmodAssembliesDir = Path.Combine("lib");
    path += $";{fmodAssembliesDir}";
    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
    CrossSpeakManager.Instance.PreferSAPI(CrossSpeakManager.Instance.DetectScreenReader() == "");
    CrossSpeakManager.Instance.TrySAPI(true);
    CrossSpeakManager.Instance.Initialize();

    // Install this head's voice for the shared code. Must happen before anything speaks.
    Voice.Use(new ScreenReaderSpeech());
  }

  internal static void Unload()
  {
    Tolk.Unload();
  }

  internal static bool IsUsingSAPI()
  {
    return Tolk.DetectScreenReader().Equals("SAPI");
  }

  internal static void Interrupt()
  {
    Tolk.Output("", true);
  }
}