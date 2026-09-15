using System;

namespace BebooGarden.GameCore.Speech;

/// <summary>
/// How the game talks. Every line the player hears spoken goes through this.
///
/// On Windows it is the player's screen reader, or SAPI when none is running. On a phone it will be
/// the engine they chose in their own TTS settings. The shared code only ever asks for a sentence
/// to be said and says nothing about who says it.
/// </summary>
public interface ISpeech
{
  /// <summary>
  /// Says a line. <paramref name="interrupt"/> cuts off whatever is being spoken, which is right
  /// for something the player just did and wrong for a beboo's chatter.
  /// </summary>
  bool Say(string text, bool interrupt = false);
}

/// <summary>
/// Where the shared code finds the voice the head installed at startup.
///
/// A locator rather than a constructor argument because the game's objects are built all over the
/// place and reach for the voice from deep inside behaviour code; threading one through every
/// beboo, item and minigame buys nothing. It works the same way as
/// <see cref="GameCore.GameHost"/>, and for the same reason.
/// </summary>
public static class Voice
{
  private static ISpeech? _current;

  public static ISpeech Current => _current
      ?? throw new InvalidOperationException(
          "No ISpeech installed. The platform head must call Voice.Use before the game starts.");

  public static void Use(ISpeech speech) => _current = speech;
}
