using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.Interface.UI;
using BebooGarden.Minigame;
using BebooGarden.MiniGames;
using BebooGarden.Save;
using CrossSpeak;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BebooGarden;

public partial class Game1
{
  private Beboo? _contester;

  /// <summary>The beboo currently carried by the player, if any.</summary>
  public Beboo? BebooInArms { get; private set; }

  private void TakeOrPutDownBeboo()
  {
    if (BebooInArms != null)
    {
      BebooInArms.PutDown();
      BebooInArms = null;
      _lastSwayWasLeft = false;
      SoundSystem.System.PlaySound(SoundSystem.ItemPutSound);
      return;
    }
    if (ItemInHand != null)
    {
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      CrossSpeakManager.Instance.Output(BebooText.beboo_handsfull);
      return;
    }
    Beboo? bebooUnderCursor = BebooUnderCursor();
    if (bebooUnderCursor == null)
    {
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      return;
    }
    bebooUnderCursor.PickUp();
    BebooInArms = bebooUnderCursor;
    _lastSwayWasLeft = false;
  }

  private void SwayBebooInArms(bool toLeft)
  {
    BebooInArms?.Sway(toLeft);
  }

  /// <summary>Lets go of the carried beboo when something else took it away, a race for instance.</summary>
  private void ReleaseBebooInArmsIfGone()
  {
    if (BebooInArms == null) return;
    if (Map != null && !Map.IsRaceMap && Map.Beboos.Contains(BebooInArms) && !BebooInArms.Racer) return;
    BebooInArms.PutDown(false);
    BebooInArms = null;
  }

  private void SayBebooState()
  {
    if (Map?.Beboos.Count == 0)
    {
      CrossSpeakManager.Instance.Output(BebooText.nobeboo);
    }
    if (Map is null) return;
    string allSentences = "";
    foreach (var beboo in Map.Beboos)
    {
      string sentence;
      string name = beboo.Name;
      if (beboo.Sleeping)
      {
        sentence = BebooText.beboo_sleep;
      }
      else
      {
        if (beboo.Happiness < 0) sentence = BebooText.beboo_verysad;
        // Read the same flag the music follows, so the two can never disagree.
        else if (!beboo.Happy) sentence = BebooText.beboo_littlesad;
        else sentence = beboo.EnergyLevel switch
        {
          EnergyStage.Exhausted => BebooText.beboo_exhausted,
          EnergyStage.Tired => BebooText.beboo_tired,
          EnergyStage.LittleTired => BebooText.beboo_littletired,
          EnergyStage.Ok => BebooText.beboo_okenergy,
          _ => BebooText.beboo_energetic,
        };
      }
      allSentences += String.Format(sentence, name);
      allSentences += "\n";
    }
    CrossSpeakManager.Instance.Output(allSentences);
  }
  private void FeedBeboo()
  {
    Beboo? bebooUnderCursor = BebooUnderCursor();
    if (Save.FruitsBasket == null || bebooUnderCursor == null) return;
    Dictionary<string, FruitSpecies> options = [];
    foreach (KeyValuePair<FruitSpecies, int> fruit in Save.FruitsBasket)
    {
      if (fruit.Value > 0) options.Add(fruit.Key.ToString() + " " + fruit.Value.ToString(), fruit.Key);
    }
    if (options.Count == 1)
    {
      OnFruitSelected(options.First().Value);
    }
    else if (options.Count > 0)
    {
      new ChooseMenu<FruitSpecies>(BebooText.ui_chooseitem, options, OnFruitSelected)
       .Show();
    }
  }

  private void OnFruitSelected(FruitSpecies choice)
  {
    if (choice != FruitSpecies.None)
    {
      Beboo? bebooUnderCursor = BebooUnderCursor();
      bebooUnderCursor?.Eat(choice);
      Save.FruitsBasket[choice]--;
    }
  }

  private CompetitionType _competitionType = CompetitionType.None;

  /// <summary>
  /// The competition centre's front desk: pick a contest, then who competes, then any options that
  /// contest has. Every contest spends from the same daily allowance.
  /// </summary>
  public void ShowCompetitionMenu()
  {
    if (!Competition.IsAnyOpen())
    {
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      CrossSpeakManager.Instance.Output(BebooText.competition_closed);
      return;
    }
    if (Map == null || Map.Beboos.Count == 0)
    {
      CrossSpeakManager.Instance.Output(BebooText.nobeboo);
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      return;
    }
    Dictionary<string, CompetitionType> options = [];
    foreach (var (name, type) in new[]
             {
               (BebooText.competition_race, CompetitionType.Race),
               (BebooText.jump_name, CompetitionType.Jump),
             })
    {
      options.Add(String.Format(BebooText.competition_entry, name,
          Competition.GetRemainingTriesToday(type)), type);
    }
    new ChooseMenu<CompetitionType>(BebooText.competition_choose, options, OnCompetitionChosen)
      .Show();
  }

  private void OnCompetitionChosen(CompetitionType competitionType)
  {
    if (competitionType == CompetitionType.None) return;
    _competitionType = competitionType;
    ChooseBebooForCompetition();
  }

  public void ChooseBebooForCompetition()
  {
    if (Map?.Beboos.Count > 0)
    {
      if (Map.Beboos.Count > 1)
      {
        Dictionary<string, Beboo> options = new();
        foreach (Beboo b in Map.Beboos)
          options.Add(b.Name, b);
        new ChooseMenu<Beboo?>(BebooText.choosebeboo, options, StartCompetition)
          .Show();
      }
      else { StartCompetition(Map?.Beboos[0]); }
    }
    else
    {
      CrossSpeakManager.Instance.Output(BebooText.nobeboo);
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
    }
  }

  private void StartCompetition(Beboo? contester)
  {
    if (contester == null) return;
    if (!Competition.IsOpen(_competitionType))
    {
      SoundSystem.System.PlaySound(SoundSystem.WarningSound);
      CrossSpeakManager.Instance.Output(BebooText.competition_closed);
      return;
    }
    _contester = contester;
    if (_competitionType == CompetitionType.Jump)
    {
      StartMiniGame(new JumpContest(contester));
      return;
    }
    Dictionary<string, RaceType> raceTypeOptions = new()
    {
      { BebooText.race_simple, RaceType.Base },
    };
    if (Save.Flags.UnlockSnowyMap) raceTypeOptions.Add(BebooText.race_snow, RaceType.Snowy);
    if (raceTypeOptions.Count > 1)
    {
      new ChooseMenu<RaceType>(BebooText.race_chooserace, raceTypeOptions, OnRaceChoosed)
        .Show();
    }
    else
    {
      OnRaceChoosed(RaceType.Base);
    }
  }

  private void OnRaceChoosed(RaceType raceType)
  {
    if (raceType == RaceType.None || _contester == null) return;
    StartMiniGame(new Minigame.Race(raceType, _contester));
  }

  private void StartMiniGame(IMiniGame miniGame)
  {
    if (Competition.IsRunning || CurrentPlayingMiniGame != null) return;
    CurrentPlayingMiniGame = miniGame;
    CurrentPlayingMiniGame.Start();
  }
}
