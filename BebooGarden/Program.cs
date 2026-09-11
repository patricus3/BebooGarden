using System;
using System.IO;
using System.Threading.Tasks;

// fmod.dll sits in lib\ and is found by probing relative to the working directory, so the game only
// started when that happened to be its own folder. Shortcuts set it, which is why this was never
// noticed, but a command line, a launcher or a file manager need not, and the reward was a
// DllNotFoundException before anything was on screen. Pin it to where the game actually lives.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// A crash used to take the window away with nothing written down anywhere, which makes a bug
// report a guess. Anything that gets this far is appended to crash.log in the player's own folder,
// with the stack, so a player can send it on and it says what actually happened.
static void Record(string origin, Exception? error)
{
    if (error == null) return;
    try
    {
        File.AppendAllText(BebooGarden.GamePaths.CrashLog,
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {origin}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
    }
    catch (Exception)
    {
        // Nothing sensible left to do if even writing the log fails.
    }
}

AppDomain.CurrentDomain.UnhandledException += (_, e) => Record("unhandled", e.ExceptionObject as Exception);
// Beboo behaviour runs plenty of delayed work on the thread pool; a throw in one of those is
// otherwise swallowed entirely.
TaskScheduler.UnobservedTaskException += (_, e) => Record("background task", e.Exception);

try
{
    using var game = new BebooGarden.Game1();
    game.Run();
}
catch (Exception error)
{
    Record("startup or main loop", error);
    throw;
}
