using System;

namespace BebooGarden.GameCore.Input;

/// <summary>
/// One update's worth of input, already translated out of whatever the platform speaks.
///
/// Implemented by each head: the Windows one wraps a MonoGame KeyboardState, a phone one will wrap
/// taps on the gesture surface. The shared code sees neither.
/// </summary>
public interface IInputFrame
{
  /// <summary>
  /// True only on the update the action begins, not for as long as it is held. Holding a digit
  /// must turn one card over, not one per frame.
  /// </summary>
  bool JustPressed(GameAction action);
}

public static class InputFrameExtensions
{
  private static readonly GameAction[] Slots =
  [
    GameAction.Slot1, GameAction.Slot2, GameAction.Slot3, GameAction.Slot4,
    GameAction.Slot5, GameAction.Slot6, GameAction.Slot7, GameAction.Slot8,
  ];

  /// <summary>
  /// Which of the first <paramref name="count"/> slots the player just chose, or null.
  ///
  /// Returns the slot that was actually pressed. The keyboard version of this used to ask whether
  /// any digit had been pressed and then read GetPressedKeys()[0], which is whichever key the
  /// driver happened to list first - so a digit pressed while any other key was held could turn
  /// over a different case than the one asked for.
  /// </summary>
  public static int? ChosenSlot(this IInputFrame input, int count)
  {
    if (input is null) throw new ArgumentNullException(nameof(input));
    int limit = Math.Min(count, Slots.Length);
    for (int i = 0; i < limit; i++)
      if (input.JustPressed(Slots[i]))
        return i + 1;
    return null;
  }
}
