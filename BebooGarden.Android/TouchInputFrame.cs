using BebooGarden.GameCore.Input;
using System.Collections.Generic;

namespace BebooGarden.Droid;

/// <summary>
/// The phone's <see cref="IInputFrame"/>: actions the gesture surface has decided happened, drained
/// once per tick.
///
/// Same contract as the keyboard's - an action reads true only on the tick it begins - so the
/// shared minigames cannot tell the difference between a tap and a key. Holding a finger down does
/// not turn a card over forty times a second here either, because an action is consumed when read.
/// </summary>
public sealed class TouchInputFrame : IInputFrame
{
  private readonly object _lock = new();
  private readonly HashSet<GameAction> _pending = [];
  private HashSet<GameAction> _thisTick = [];

  /// <summary>Records an action. Called from the touch listener, off the game thread.</summary>
  public void Raise(GameAction action)
  {
    lock (_lock) _pending.Add(action);
  }

  /// <summary>
  /// Takes everything raised since the last tick and makes it the current frame. The activity calls
  /// this once per update, before anything reads the frame.
  /// </summary>
  public void BeginTick()
  {
    lock (_lock)
    {
      _thisTick = [.. _pending];
      _pending.Clear();
    }
  }

  public bool JustPressed(GameAction action) => _thisTick.Contains(action);
}
