using BebooGarden.GameCore.Input;
using Microsoft.Xna.Framework.Input;

namespace BebooGarden;

/// <summary>
/// The Windows head's answer to <see cref="IInputFrame"/>: a MonoGame keyboard snapshot, read
/// through the same just-pressed test the rest of the game uses.
///
/// This is the only file on this side that maps keys to meanings. When the Android head arrives it
/// writes its own version of this and nothing in GameCore changes.
/// </summary>
internal sealed class KeyboardInputFrame : IInputFrame
{
  private readonly Game1 _game;
  private readonly KeyboardState _state;

  public KeyboardInputFrame(Game1 game, KeyboardState state)
  {
    _game = game;
    _state = state;
  }

  public bool JustPressed(GameAction action) => _game.IsKeyPressed(_state, KeysFor(action));

  /// <summary>
  /// Both the number row and the numpad, because a slot is a slot however you reached for it.
  /// </summary>
  private static Keys[] KeysFor(GameAction action) => action switch
  {
    GameAction.Back => [Keys.Escape],
    GameAction.Slot1 => [Keys.D1, Keys.NumPad1],
    GameAction.Slot2 => [Keys.D2, Keys.NumPad2],
    GameAction.Slot3 => [Keys.D3, Keys.NumPad3],
    GameAction.Slot4 => [Keys.D4, Keys.NumPad4],
    GameAction.Slot5 => [Keys.D5, Keys.NumPad5],
    GameAction.Slot6 => [Keys.D6, Keys.NumPad6],
    GameAction.Slot7 => [Keys.D7, Keys.NumPad7],
    GameAction.Slot8 => [Keys.D8, Keys.NumPad8],
    _ => [],
  };
}
