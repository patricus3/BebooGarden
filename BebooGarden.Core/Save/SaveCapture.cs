using BebooGarden.GameCore;
using BebooGarden.GameCore.Item;
using BebooGarden.GameCore.Item.MusicBox;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.MiniGames;
using BebooGarden.Minigame;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BebooGarden.Save;

/// <summary>
/// Turns the running game into a <see cref="SaveParameters"/> and writes it.
///
/// This is game logic - what counts as worth keeping, which beboos are real and which are race
/// stand-ins, where the rolls live - and it used to sit inside the Windows Game1, where a second
/// platform would have had to reimplement it and get every one of those decisions right a second
/// time. One wrong guess there is somebody's garden.
/// </summary>
public static class SaveCapture
{
  public static void Write(IGame game)
  {
    // Rolls live in the music box, not on the ground. Every map, not just the current one: rolls
    // left elsewhere used to be saved.
    foreach (Map map in Map.Maps.Values)
      map.Items.RemoveAll(item => item is Roll);

    Dictionary<MapPreset, MapInfo> mapInfos = [];
    foreach (Map map in Map.Maps.Values)
    {
      int fruits = map.TreeLines.Count > 0 ? map.TreeLines[0].Fruits : 0;

      List<BebooInfo> bebooInfos = [];
      foreach (Beboo beboo in map.Beboos)
      {
        // A racer is a stand-in for the duration of a race, not one of the player's beboos.
        if (beboo.Racer) continue;
        bebooInfos.Add(new BebooInfo(
            beboo.Name, beboo.Age, beboo.Happiness, beboo.Energy,
            beboo.SwimLevel, beboo.VoicePitch, beboo.BebooType)
        {
          ModCreature = beboo.ModCreature,
          Trait = beboo.Trait,
        });
      }

      mapInfos.Add(map.Preset, new MapInfo(map.Items, fruits, bebooInfos));
    }

    SaveParameters save = game.Save;
    SaveParameters parameters = new(
        CultureInfo.CurrentUICulture.Name,
        game.SoundSystem.Volume,
        lastPayed: DateTime.Now,
        flags: save.Flags,
        playerName: save.PlayerName,
        fruitsBasket: save.FruitsBasket ?? [],
        inventory: game.Inventory,
        tickets: save.Tickets,
        unlockedRolls: MusicBox.AvailableRolls,
        favoredColor: save.FavoredColor,
        currentMap: game.Map?.Preset ?? MapPreset.garden,
        mapInfos: mapInfos,
        raceScores: Race.RaceScores,
        raceTodayTries: Competition.TodayTries.GetValueOrDefault(CompetitionType.Race),
        raceTotalWin: Race.TotalWin,
        musicLevel: game.SoundSystem.MusicVolume,
        enabledMods: [.. Modding.ModManager.Enabled],
        competitionTries: Competition.TodayTries);

    parameters.MusicMuted = game.SoundSystem.MusicMuted;
    // Everything discovered this run counts as seen, so the startup list does not ask again.
    parameters.KnownMods =
        [.. (save.KnownMods ?? []).Union(Modding.ModManager.All.Select(mod => mod.Id))];

    SaveManager.WriteSave(parameters);
  }
}
