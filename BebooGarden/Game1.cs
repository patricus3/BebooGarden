using BebooGarden.GameCore.Item;
using BebooGarden.GameCore.Item.MusicBox;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.Minigame;
using BebooGarden.MiniGames;
using BebooGarden.Save;
using CrossSpeak;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BebooGarden;
public partial class Game1 : Game, GameCore.IGame
{
  public const string GAMENAME = "Beboo Garden: Enhanced Edition";

  public GameScreen _currentScreen;
  public GameScreen _previousGameScreen;

  public SoundSystem SoundSystem { get; }

  // Singleton
  public static Game1 Instance { get; private set; }
  public Random Random { get; set; }
  /// <summary>Scenes and menus for the shared code. See <see cref="MyraGameUi"/>.</summary>
  public GameCore.IGameUi Ui { get; } = new MyraGameUi();
  public IMiniGame? CurrentPlayingMiniGame { get; set; } = null;
  private bool _lastArrowWasUp;

  public Game1()
  {
    ScreenReader.Load();
    _graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "Content";
    Random = new Random();
    _currentScreen = GameScreen.game;
    SoundSystem = new SoundSystem();
    Instance = this; // Set the static instance
    // And install it for the shared code, which asks GameHost rather than naming this class.
    GameCore.GameHost.Use(this);
  }

  private void OnExit(object sender, ExitingEventArgs e)
  {
    if (Race.IsARaceRunning)
    {
      e.Cancel = true;
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      return;
    }
    WriteSave();
    CrossSpeakManager.Instance.Close();
  }

  /// <summary>Writes the garden down. The deciding is shared; see SaveCapture.</summary>
  private void WriteSave() => BebooGarden.Save.SaveCapture.Write(this);
  public List<Item> Inventory { get; set; } = [];
  public Item? ItemInHand { get; set; }

  public bool Wasd = false; // TODO InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName != "fr";
}