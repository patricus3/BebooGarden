using Android.Content;
using Android.OS;
using Android.Speech.Tts;
using BebooGarden.GameCore.Speech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BebooGarden.Droid;

/// <summary>One installed text-to-speech engine, as offered to the player.</summary>
public sealed record SpeechEngine(string Package, string Label)
{
  public override string ToString() => Label;
}

/// <summary>
/// The Android head's voice.
///
/// The important thing here is what it does NOT do: it never names an engine package. Constructing
/// TextToSpeech without one uses whatever the player chose in their own system settings, so
/// somebody running RHVoice, eSpeak-NG, Vocalizer or Acapela gets that, and only somebody who
/// actually picked Google gets Google. Hard-coding "com.google.android.tts" is the bug this exists
/// to avoid; a blind player's engine is their own choice and often a paid one.
///
/// On top of that the player can override it just for this game, because the engine you want
/// reading a pet's chatter is not always your system default. <see cref="AvailableEngines"/> lists
/// what is installed and <see cref="UseEngine"/> switches to one.
/// </summary>
public sealed class AndroidSpeech : Java.Lang.Object, ISpeech, TextToSpeech.IOnInitListener
{
  private readonly Context _context;
  private TextToSpeech? _tts;
  private bool _ready;
  private string? _pendingText;
  private bool _pendingInterrupt;

  /// <summary>The engine in use, or null when following the system default.</summary>
  public string? CurrentEngine { get; private set; }

  public AndroidSpeech(Context context, string? preferredEngine = null)
  {
    _context = context;
    Start(preferredEngine);
  }

  private void Start(string? enginePackage)
  {
    _ready = false;
    CurrentEngine = enginePackage;
    // The three-argument overload names an engine; the two-argument one follows the system default.
    // Passing null to the former is not the same thing, so pick the overload deliberately.
    _tts = enginePackage is null
        ? new TextToSpeech(_context, this)
        : new TextToSpeech(_context, this, enginePackage);
  }

  public void OnInit(OperationResult status)
  {
    _ready = status == OperationResult.Success;
    if (!_ready || _pendingText is null) return;

    // Anything the game tried to say while the engine was still starting - which includes the
    // welcome line, because startup is exactly when this is slowest.
    string text = _pendingText;
    bool interrupt = _pendingInterrupt;
    _pendingText = null;
    Say(text, interrupt);
  }

  public bool Say(string text, bool interrupt = false)
  {
    if (string.IsNullOrEmpty(text)) return false;

    if (!_ready || _tts is null)
    {
      // Keep only the newest: a queue of stale lines said all at once when the engine wakes up is
      // worse than having missed them.
      _pendingText = text;
      _pendingInterrupt = interrupt;
      return false;
    }

    var mode = interrupt ? QueueMode.Flush : QueueMode.Add;
    return _tts.Speak(text, mode, null, text.GetHashCode().ToString()) == OperationResult.Success;
  }

  /// <summary>Every engine on the device, for a settings list.</summary>
  public IReadOnlyList<SpeechEngine> AvailableEngines =>
      _tts?.Engines?
          .Where(e => e.Name is not null)
          .Select(e => new SpeechEngine(e.Name!, e.Label ?? e.Name!))
          .ToList()
      ?? [];

  /// <summary>
  /// Switches engine, or goes back to the system default when given null. The old engine is shut
  /// down first; leaving it running holds an audio session open for nothing.
  /// </summary>
  public void UseEngine(string? enginePackage)
  {
    if (enginePackage == CurrentEngine) return;
    Shutdown();
    Start(enginePackage);
  }

  public void Stop() => _tts?.Stop();

  public void Shutdown()
  {
    try
    {
      _tts?.Stop();
      _tts?.Shutdown();
    }
    catch (Exception)
    {
      // Shutting down a half-initialised engine throws on some devices and there is nothing
      // sensible to do about it.
    }
    _tts = null;
    _ready = false;
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing) Shutdown();
    base.Dispose(disposing);
  }
}
