namespace BebooGarden.GameCore.Input;

/// <summary>
/// What the player meant, not what they pressed.
///
/// The game logic is not allowed to know about keyboards. On a desktop a slot is chosen with the
/// number row or the numpad; on a phone it will be a tap, and on a gamepad a face button. All three
/// mean the same thing to a minigame, so the minigame is told the meaning and the platform is left
/// to work out how the player expressed it.
///
/// This grows as more of the game moves across. It deliberately holds only what the shared code
/// actually asks for today: the main garden loop still reads the keyboard directly in the Windows
/// head, and moves over later.
/// </summary>
public enum GameAction
{
  /// <summary>Leave, cancel, go back. Escape on a keyboard.</summary>
  Back,

  Slot1,
  Slot2,
  Slot3,
  Slot4,
  Slot5,
  Slot6,
  Slot7,
  Slot8,
}
