using Android.Content;
using Android.OS;
using BebooGarden;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BebooGarden.Droid;

/// <summary>
/// Decides where everything lives on a phone and puts the shipped content somewhere FMOD can open
/// it.
///
/// FMOD opens sounds as files by path. Android assets are not files - they sit inside the apk and
/// are only reachable through AssetManager - so the ~470 MB of audio is unpacked once, on first
/// run, into the app's own storage, and <see cref="GamePaths.ContentRoot"/> is pointed at it.
/// </summary>
public static class AndroidStorage
{
  private const string ContentMarker = ".unpacked";

  /// <summary>
  /// Points the crash log somewhere the player can actually reach, and nothing else.
  ///
  /// Separate from <see cref="Prepare"/> and far cheaper, because it has to run before anything
  /// that might throw - including <see cref="Prepare"/> itself, which unpacks half a gigabyte and
  /// is the likeliest thing in the whole app to fail on a device that is out of room.
  /// </summary>
  public static void PrepareCrashLog(Context context)
  {
    string? external = context.GetExternalFilesDir(null)?.AbsolutePath;
    string folder = external ?? context.FilesDir?.AbsolutePath ?? Path.GetTempPath();
    Directory.CreateDirectory(folder);
    GamePaths.CrashLog = Path.Combine(folder, "crash.log");
  }

  /// <summary>
  /// Sets every path the shared code reads, then unpacks the content if it has not been unpacked
  /// already. Call once, before anything loads a sound.
  /// </summary>
  public static void Prepare(Context context, Action<int, int>? onProgress = null)
  {
    string files = context.FilesDir?.AbsolutePath
        ?? throw new InvalidOperationException("No files directory.");

    string content = Path.Combine(files, "Content");
    GamePaths.ContentRoot = content.Replace('\\', '/').TrimEnd('/') + "/";
    GamePaths.InstalledModsFolder = Path.Combine(files, "mods");

    // Mods go somewhere a file manager can actually reach. The app's private storage cannot be
    // browsed without root, so a mods folder there would be one nobody could put a mod into.
    // This path needs no permission on any version: it is the app's own external directory.
    string? external = context.GetExternalFilesDir(null)?.AbsolutePath;
    string userMods = Path.Combine(external ?? files, "mods");
    Directory.CreateDirectory(userMods);
    GamePaths.UserModsFolder = userMods;

    // The crash log goes in the same reachable place, for the same reason and then some: it is
    // written so that somebody can send it on, and a log nobody can open is no log at all. The
    // save deliberately stays in private storage - that one is the player's and wants protecting,
    // not sharing.
    GamePaths.CrashLog = Path.Combine(external ?? files, "crash.log");

    // A note left where somebody looking for the folder will find it.
    string readme = Path.Combine(userMods, "PUT MODS HERE.txt");
    if (!File.Exists(readme))
    {
      File.WriteAllText(readme,
          "Drop a mod's .dll here, or its folder, and restart Beboo Garden.\r\n" +
          "Turn mods on in the game's mod menu.\r\n\r\n" +
          $"Full path: {userMods}\r\n\r\n" +
          "If the game ever crashes, crash.log is in the folder above this one -\r\n" +
          "send it on and it says what actually happened.\r\n");
    }

    UnpackContent(context, content, onProgress);
  }

  /// <summary>
  /// Unpacks the content zip. Done once per app version: a new build may carry new or changed
  /// sounds, and a stale marker would leave the player without them.
  /// </summary>
  private static void UnpackContent(Context context, string target, Action<int, int>? onProgress)
  {
    string marker = Path.Combine(target, ContentMarker);
    string version = context.PackageManager?
        .GetPackageInfo(context.PackageName!, 0)?.LongVersionCode.ToString() ?? "0";

    if (File.Exists(marker) && File.ReadAllText(marker) == version) return;

    var assets = context.Assets ?? throw new InvalidOperationException("No asset manager.");
    Directory.CreateDirectory(target);

    // ZipArchive has to seek to read the central directory, and an asset stream cannot seek - it
    // reads out of the apk. So spill it to a real file first. The copy is deleted as soon as the
    // extraction finishes, but it does mean first run briefly needs room for the content twice.
    string spill = Path.Combine(context.CacheDir?.AbsolutePath ?? target, "content.zip");
    try
    {
      using (Stream packed = assets.Open("content.zip"))
      using (var copy = File.Create(spill))
        packed.CopyTo(copy);

      using var archive = ZipFile.OpenRead(spill);
      int done = 0;
      int total = archive.Entries.Count;
      foreach (ZipArchiveEntry entry in archive.Entries)
      {
        // A directory entry has an empty name; there is nothing to write for it.
        if (string.IsNullOrEmpty(entry.Name))
        {
          done++;
          continue;
        }

        string destination = Path.GetFullPath(Path.Combine(target, entry.FullName));
        // Never let an entry write outside the target, whatever path the archive claims.
        if (!destination.StartsWith(Path.GetFullPath(target), StringComparison.Ordinal))
          continue;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        entry.ExtractToFile(destination, overwrite: true);
        onProgress?.Invoke(++done, total);
      }
    }
    finally
    {
      try { if (File.Exists(spill)) File.Delete(spill); } catch (IOException) { }
    }

    File.WriteAllText(marker, version);
  }
}
