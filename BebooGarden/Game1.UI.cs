using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.MiniGames;
using BebooGarden.Save;
using BebooGarden.UI;
using CrossSpeak;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BebooGarden;

public partial class Game1
{
  private readonly GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  public Desktop _desktop;
  private Panel _gamePanel;
  public TalkDialog? _talkDialog = null;
  public Dictionary<Panel, Panel> PreviousPanels { get; set; } = [];

  public void SwitchToScreen(GameScreen screen)
  {
    // The key that opened this screen is almost certainly still held, and IsKeyPressed compares
    // against the previous frame, which is not updated until the end of this one. So a screen that
    // appears mid-frame saw the very same press again and acted on it: pressing enter on something
    // lying on the ground opened the confirmation and then immediately answered Yes in it, which
    // looked exactly like no confirmation at all.
    _previousKeyboardState = _currentKeyboardState;
    _previousGameScreen=_currentScreen;
    _currentScreen = screen;
    switch (screen)
    {
      case GameScreen.First:
        break;
      case GameScreen.MainMenu:
        CreateEscapeMenu();
        _desktop.Root = _escapeMenuPanel;
        //Widget playButton = _mainMenuPanel.FindChildById("playButton");
        //playButton?.SetKeyboardFocus();
        break;
      case GameScreen.game:
        _desktop.Root = _gamePanel;
        break;
      case GameScreen.ChooseMenu:
        if (_aMenuShouldBeClosed)
        {
          _aMenuShouldBeClosed = false;
        }
        break;
    }
  }

  private void UpdateUIState()
  {
  }
  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Black);

    _spriteBatch.Begin();
    _spriteBatch.End();
    // Make myra interface
    _desktop.Render();

    base.Draw(gameTime);
  }
}