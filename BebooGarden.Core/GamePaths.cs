using System;
using System.Collections.Generic;
using System.IO;

namespace BebooGarden;

/// <summary>
/// Where the game reads and writes.
///
/// Everything the player owns - the save, the crash log, any mods they added - lives under
/// %LocalAppData%\BebooGarden. Everything the installer shipped stays in the install folder and is
/// only ever read. Keeping those apart is what lets the game be installed once for the whole
/// machine: Program Files is not writable by a normal account, and a game that saves next to its
/// own executable either has to be installed per user or has to have its folder opened up, and
/// opening up a folder full of executables is how a standard user ends up running code as somebody
/// else.
///
/// It also gives each Windows account its own garden, which is the behaviour you would expect
/// anyway.
/// </summary>
public static class GamePaths
{
  private const string SAVEFILE = "save.dat";
  private const string CRASHLOG = "crash.log";
  private const string MODSFOLDER = "mods";

  /// <summary>The folder the game was installed into. Read-only as far as the game is concerned.</summary>
  public static string InstallFolder { get; } = AppContext.BaseDirectory;

  /// <summary>
  /// Where the shipped sounds and music are, with a trailing slash.
  ///
  /// Settable because not every platform has them next to the executable. On Windows they are in
  /// the install folder and this is right as it stands. On Android the same files arrive inside the
  /// package and are unpacked to storage on first run, so that head points this at wherever it put
  /// them before the sound system loads anything.
  ///
  /// Always a forward slash: the 70-odd places that build a path off this concatenate with '/', and
  /// Windows accepts that perfectly well.
  /// </summary>
  public static string ContentRoot { get; set; } = AppContext.BaseDirectory.Replace('\\', '/').TrimEnd('/') + "/Content/";

  /// <summary>
  /// This player's own folder. Created on demand, so a first run does not depend on the installer
  /// having made it.
  /// </summary>
  public static string DataFolder
  {
    get
    {
      string folder = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BebooGarden");
      Directory.CreateDirectory(folder);
      return folder;
    }
  }

  public static string SaveFile => Path.Combine(DataFolder, SAVEFILE);

  public static string CrashLog => Path.Combine(DataFolder, CRASHLOG);

  /// <summary>
  /// Where a player drops mods of their own. Needs no administrator rights.
  ///
  /// Settable because "the player's own folder" is not somewhere a player can reach on every
  /// platform. On Windows <see cref="DataFolder"/> is under %LocalAppData% and Explorer opens it
  /// happily. On Android the equivalent is the app's private storage, which no file manager will
  /// show you without root - a mods folder there would be one that nobody could put a mod into.
  /// That head points this at shared storage instead, so dropping a dll in is the same gesture it
  /// is on a desktop.
  /// </summary>
  public static string UserModsFolder { get; set; } = Path.Combine(DataFolder, MODSFOLDER);

  /// <summary>
  /// Mods that came with the game. Settable for the same reason as <see cref="UserModsFolder"/>:
  /// these ship inside the package on Android and are unpacked somewhere else on first run.
  /// </summary>
  public static string InstalledModsFolder { get; set; } = Path.Combine(InstallFolder, MODSFOLDER);

  /// <summary>
  /// Both mod folders, shipped ones first so that a player's own copy of a mod wins: ids have to be
  /// unique, and the first one discovered is the one that is kept.
  /// </summary>
  public static IEnumerable<string> ModFolders
  {
    get
    {
      if (Directory.Exists(InstalledModsFolder)) yield return InstalledModsFolder;
      if (Directory.Exists(UserModsFolder)) yield return UserModsFolder;
    }
  }

  /// <summary>
  /// Brings a save written by an older version, which kept it beside the executable, across to the
  /// player's own folder. Runs once: after this there is a save in the new place and the check
  /// below stops finding anything to do.
  ///
  /// The old file is copied rather than moved. If something goes wrong here, the garden is still
  /// sitting where it always was.
  /// </summary>
  public static void MigrateLegacyFiles()
  {
    try
    {
      if (File.Exists(SaveFile)) return;
      // The old path was relative, so it landed wherever the game was started from. That is
      // normally the install folder, but not when it was launched from somewhere else.
      foreach (string folder in new[] { InstallFolder, Directory.GetCurrentDirectory() })
      {
        string legacy = Path.Combine(folder, SAVEFILE);
        if (!File.Exists(legacy)) continue;
        File.Copy(legacy, SaveFile);
        return;
      }
    }
    catch (Exception)
    {
      // A save that cannot be brought across is not worth refusing to start over. The player gets
      // a new garden, and their old file is still where they left it.
    }
  }
}
