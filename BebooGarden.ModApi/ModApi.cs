using System;

namespace BebooGarden.ModApi;

/// <summary>
/// What every code mod implements. The game finds it by scanning the assemblies in a mod's folder
/// for a public class with a parameterless constructor that implements this, makes one, and calls
/// <see cref="Start"/> once, after the player has said which mods are on.
///
/// A mod that only adds creatures needs none of this: its manifest and its sound files are enough.
/// This is for mods that want to change what the game does.
/// </summary>
public interface IBebooMod
{
  /// <summary>
  /// Called once, on the game thread, before the garden opens. Hold on to the host and register
  /// whatever you want to take part in.
  ///
  /// Anything thrown here is written to the crash log and the mod is dropped; the game carries on
  /// without it, because somebody else's code should not cost a player their garden.
  /// </summary>
  void Start(IModHost host);
}

/// <summary>
/// The game, as a mod is allowed to see it. Everything a mod can ask for or take part in arrives
/// through here.
/// </summary>
public interface IModHost
{
  /// <summary>The game's version, so a mod can tell what it is running against.</summary>
  string GameVersion { get; }

  /// <summary>
  /// Writes a line to the crash log, prefixed with the mod's id. The place to say what went wrong
  /// when something did.
  /// </summary>
  void Log(string message);

  /// <summary>
  /// Says something through the screen reader. This is how a mod talks to the player: the game is
  /// played by ear, so there is nowhere else for text to go.
  /// </summary>
  void Say(string text);

  /// <summary>
  /// One of the game's own translated strings, by its key, in whatever language the player is
  /// using, or null when there is no such key. Reusing the game's text is how a mod stays
  /// translated without shipping translations of its own.
  /// </summary>
  string? Text(string key);

  /// <summary>
  /// Asks the player a yes or no question and calls back with the answer. The question appears as
  /// a menu, and backing out of it counts as no.
  ///
  /// The callback happens later, on the game thread, once they have answered.
  /// </summary>
  void AskYesNo(string question, Action<bool> answer);

  /// <summary>Takes part in what happens when the player picks something up off the ground.</summary>
  void OnPickingUp(Func<PickupRequest, bool> handler);
}

/// <summary>
/// The player is about to pick something up. A handler returns true to take charge of it, and the
/// game then does nothing at all until <see cref="Take"/> or <see cref="Cancel"/> is called, which
/// may be much later - after a question has been answered, for instance.
///
/// Returning false leaves it alone and the pickup happens as usual.
/// </summary>
public sealed class PickupRequest
{
  private readonly Action _take;
  private bool _settled;

  public PickupRequest(string itemName, bool isEgg, Action take)
  {
    ItemName = itemName;
    IsEgg = isEgg;
    _take = take;
  }

  /// <summary>What the thing is called, in the player's language.</summary>
  public string ItemName { get; }

  /// <summary>
  /// Whether it is an egg. Eggs are not really picked up - touching one hatches it - so a mod that
  /// asks about picking things up usually wants to leave them alone.
  /// </summary>
  public bool IsEgg { get; }

  /// <summary>Go ahead and pick it up. Does nothing if this request was already settled.</summary>
  public void Take()
  {
    if (_settled) return;
    _settled = true;
    _take();
  }

  /// <summary>Leave it where it is. Does nothing if this request was already settled.</summary>
  public void Cancel() => _settled = true;
}
