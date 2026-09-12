using System;
using System.Collections.Generic;
using System.Globalization;
using BebooGarden.GameCore.Item;
using BebooGarden.GameCore.World;
using BebooGarden.MiniGames;

namespace BebooGarden.Save;

public class SaveParameters
{
  public SaveParameters(string? language, float volume,
      DateTime lastPayed, Flags flags, string playerName, SortedDictionary<FruitSpecies, int> fruitsBasket, List<Item> inventory, int tickets, System.Collections.Generic.List<string> unlockedRolls, string favoredColor, Dictionary<MapPreset, MapInfo> mapInfos, MapPreset currentMap, Dictionary<RaceType, double> raceScores, int raceTodayTries, int raceTotalWin, float? musicLevel, Dictionary<CompetitionType, int>? competitionTries = null, List<string>? enabledMods = null)
  {
    Volume = volume;
    Language = language;
    LastPlayed = lastPayed;
    Flags = flags;
    PlayerName = playerName;
    FruitsBasket = fruitsBasket;
    Inventory = inventory;
    Tickets = tickets;
    UnlockedRolls = unlockedRolls;
    FavoredColor = favoredColor;
    MapInfos = mapInfos;
    CurrentMap = currentMap;
    RaceScores = raceScores;
    RaceTodayTries = raceTodayTries;
    CompetitionTries = competitionTries ?? [];
    EnabledMods = enabledMods ?? [];
    RaceTotalWin = raceTotalWin;
    MusicLevel = musicLevel;
  }

  public SaveParameters()
  {
    Volume = 0.5f;
    Language = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
    LastPlayed = default;
    Flags = new Flags();
    PlayerName = string.Empty;
    FavoredColor = "none";
    FruitsBasket = [];
  }

  public float Volume { get; set; }

  /// <summary>
  /// How loud the player asked the music to be, from 0 to 1, as a share of what each track wants.
  /// Null when they have never said, which is every save written before the music keys worked.
  ///
  /// The old field was called MusicVolume and held the raw volume of whichever track happened to
  /// be playing when the game was saved. It was written back over the channel on load and then
  /// overwritten again by the next change of map, so it never meant anything. Reading those
  /// numbers as a setting would quietly halve the music for everyone who already has a save, so
  /// this is a new name and the old one is left behind.
  /// </summary>
  public float? MusicLevel { get; set; }

  /// <summary>Whether the player left the music switched off.</summary>
  public bool MusicMuted { get; set; }
  public string Language { get; set; }
  public DateTime LastPlayed { get; set; }
  public Flags Flags { get; set; }
  public string PlayerName { get; set; }
  public string FavoredColor { get; internal set; }
  public SortedDictionary<FruitSpecies, int> FruitsBasket { get; set; }
  public int Tickets { get; set; }
  public List<Item> Inventory { get; set; } = new();
  public Dictionary<MapPreset, MapInfo> MapInfos { get; set; }
  public List<string> UnlockedRolls { get; set; } = new();
  public MapPreset CurrentMap { get; set; }
  public Dictionary<RaceType, double> RaceScores { get; set; }
  public string FreeTime { get; set; }
  public string Dessert { get; set; }
  public int RaceTodayTries { get; set; }

  /// <summary>Tries spent today per contest. RaceTodayTries above is the older single counter.</summary>
  public Dictionary<CompetitionType, int> CompetitionTries { get; set; } = [];

  /// <summary>Ids of the mods the player switched on.</summary>
  public List<string> EnabledMods { get; set; } = [];

  /// <summary>
  /// Every mod the player has already been shown. The list before the garden is only for mods that
  /// are not in here yet, so it stops appearing once there is nothing new to answer.
  /// </summary>
  public List<string> KnownMods { get; set; } = [];
  public int RaceTotalWin { get; set; }
}