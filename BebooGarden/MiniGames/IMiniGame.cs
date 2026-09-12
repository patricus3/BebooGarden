using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BebooGarden.MiniGames;

public interface IMiniGame
{
  public bool IsRunning { get; }
  public string Tips { get; }
  void Start();
  void Update(GameTime gameTime, KeyboardState currentKeyboardState);

  /// <summary>
  /// Tells a minigame that the player has just moved the volume. Only a minigame that brought its
  /// own sound system needs this; one that plays through the garden's has already been turned
  /// down along with it.
  /// </summary>
  void SetVolume(float volume) { }
}
