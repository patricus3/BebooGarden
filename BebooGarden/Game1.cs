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

namespace BebooGarden;
public partial class Game1 : Game
{
  public const string GAMENAME = "Beboo Garden: Enhanced Edition";

  public GameScreen _currentScreen;
  public GameScreen _previousGameScreen;

  internal SoundSystem SoundSystem { get; }

  // Singleton
  public static Game1 Instance { get; private set; }
  public Random Random { get; set; }
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

  private void WriteSave()
  {
    // Rolls live in the music box, not on the ground. Every map, not just the current one:
    // rolls left elsewhere used to be saved.
    foreach (Map map in Map.Maps.Values)
      map.Items.RemoveAll(item => item is Roll);
    Dictionary<MapPreset, MapInfo> mapInfos = [];
    foreach (Map map in Map.Maps.Values)
    {
      int fruits = 0;
      if (map.TreeLines.Count > 0)
        fruits = map.TreeLines[0].Fruits;
      List<BebooInfo> bebooInfos = new();
      foreach (Beboo beboo in map.Beboos)
      {
        if (!beboo.Racer)
          bebooInfos.Add(new(beboo.Name, beboo.Age, beboo.Happiness, beboo.Energy, beboo.SwimLevel, beboo.VoicePitch, beboo.BebooType)
          {
            ModCreature = beboo.ModCreature,
          });
      }
      MapInfo mapInfo = new(map.Items, fruits, bebooInfos);
      mapInfos.Add(map.Preset, mapInfo);
    }
    SaveParameters parameters = new(CultureInfo.CurrentUICulture.Name,
        SoundSystem.Volume,
        lastPayed: DateTime.Now,
         flags: Save.Flags,
         playerName: Save.PlayerName,
         fruitsBasket: Save.FruitsBasket ?? [],
         inventory: Inventory,
         tickets: Save.Tickets,
         unlockedRolls: MusicBox.AvailableRolls,
         favoredColor: Save.FavoredColor,
         currentMap: Map?.Preset ?? MapPreset.garden,
         mapInfos: mapInfos,
         raceScores: Race.RaceScores,
         raceTodayTries: Competition.TodayTries.GetValueOrDefault(CompetitionType.Race),
         raceTotalWin: Race.TotalWin,
         musicVolume: SoundSystem.Music?.Volume ?? 0.5f,
         enabledMods: [.. Modding.ModManager.Enabled],
         competitionTries: Competition.TodayTries
     );
    SaveManager.WriteSave(parameters);
  }

  public List<Item> Inventory { get; set; } = [];
  public Item? ItemInHand { get; set; }

  public bool Wasd = false; // TODO InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName != "fr";
}