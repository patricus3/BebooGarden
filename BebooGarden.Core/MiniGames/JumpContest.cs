using BebooGarden.GameCore;
using BebooGarden.GameCore.Speech;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.Minigame;
using BebooGarden.GameCore.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3 = System.Numerics.Vector3;

namespace BebooGarden.MiniGames;

/// <summary>
/// Jumpidy Jump: three beboos take turns bouncing down the field, the furthest one wins. The
/// listener stays on the take off line, so how far a beboo got is something you hear as much as
/// something you are told.
/// </summary>
public class JumpContest : IMiniGame
{
  private const int HOPS = 3;
  private const int HOPINTERVALMS = 450;
  private const int BETWEENJUMPERSMS = 1100;
  private const int RESULTSTEPMS = 1400;

  private enum Phase { Announce, Hopping, Landed, Results, Winner, Done }

  public string Tips { get; set; } = "";
  public bool IsRunning => Competition.IsRunning;

  private readonly Beboo _mainBeboo;
  private readonly List<Beboo> _jumpers = [];
  private readonly Dictionary<Beboo, int> _distances = [];
  private Vector3 _takeOffLine;
  private Phase _phase = Phase.Announce;
  private DateTime _timer = DateTime.Now;
  private int _jumper;
  private int _hopsDone;
  private int _resultsAnnounced;

  public JumpContest(Beboo mainBeboo)
  {
    _mainBeboo = mainBeboo;
  }

  public void Start()
  {
    if (Competition.IsRunning) return;
    Competition.IsRunning = true;
    Competition.UseATry(CompetitionType.Jump);
    GameHost.Current.ChangeMap(Map.BasicRace);
    _takeOffLine = new Vector3(-(Race.BASERACELENGTH / 2) + 2, 0, 0);

    _mainBeboo.Unpause();
    AddJumper(_mainBeboo, 0);
    // The rivals are the player's beboo's age, so the contest is decided by mood, rest and luck.
    AddJumper(new Beboo("bob", BebooType.Pink, _mainBeboo.Age, DateTime.Now,
        GameHost.Current.Random.Next(3, 9), 3 + GameHost.Current.Random.Next(5), 0, true, 1.3f), 2);
    AddJumper(new Beboo("boby", BebooType.Green, _mainBeboo.Age, DateTime.Now,
        GameHost.Current.Random.Next(3, 9), 3 + GameHost.Current.Random.Next(5), 0, true, 1.2f), -2);

    // Stay on the take off line for the whole contest: every hop is heard moving away from you.
    GameHost.Current.SoundSystem.MovePlayerTo(_takeOffLine);
    GameHost.Current.SoundSystem.PlayRaceMusic();
    _timer = DateTime.Now;
  }

  private void AddJumper(Beboo beboo, int y)
  {
    beboo.Position = _takeOffLine + new Vector3(0, y, 0);
    beboo.Destination = beboo.Position;
    GameHost.Current.Map?.Beboos.Add(beboo);
    _jumpers.Add(beboo);
    _distances[beboo] = 0;
  }

  public void Update(IInputFrame input)
  {
    // Nobody wanders off mid contest: a jumper only moves when it is its turn to hop.
    foreach (Beboo beboo in _jumpers) beboo.Destination = beboo.Position;
    double elapsed = (DateTime.Now - _timer).TotalMilliseconds;
    switch (_phase)
    {
      case Phase.Announce:
        Voice.Current.Say(BebooText.jump_start);
        GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.JingleLittleStar);
        Advance(Phase.Hopping);
        break;

      case Phase.Hopping:
        if (elapsed < HOPINTERVALMS) break;
        Hop(_jumpers[_jumper]);
        _hopsDone++;
        _timer = DateTime.Now;
        if (_hopsDone >= HOPS) _phase = Phase.Landed;
        break;

      case Phase.Landed:
        if (elapsed < HOPINTERVALMS) break;
        Beboo landed = _jumpers[_jumper];
        Voice.Current.Say(String.Format(BebooText.jump_jumped, landed.Name, _distances[landed]));
        _jumper++;
        _hopsDone = 0;
        _timer = DateTime.Now;
        _phase = _jumper < _jumpers.Count ? Phase.Results : Phase.Winner;
        // Results here just means "pause before the next jumper takes off".
        if (_phase == Phase.Winner) _resultsAnnounced = 0;
        break;

      case Phase.Results:
        if (elapsed < BETWEENJUMPERSMS) break;
        _timer = DateTime.Now;
        _phase = Phase.Hopping;
        break;

      case Phase.Winner:
        if (elapsed < RESULTSTEPMS) break;
        if (_resultsAnnounced == 0)
        {
          Beboo winner = Winner();
          GameHost.Current.SoundSystem.System.PlaySound(winner == _mainBeboo
              ? GameHost.Current.SoundSystem.RaceGoodSound
              : GameHost.Current.SoundSystem.RaceBadSound);
          Voice.Current.Say(String.Format(BebooText.jump_win, winner.Name, _distances[winner]));
          _resultsAnnounced++;
          _timer = DateTime.Now;
          break;
        }
        End();
        break;
    }
  }

  private void Advance(Phase next)
  {
    _phase = next;
    _timer = DateTime.Now;
  }

  private void Hop(Beboo beboo)
  {
    int length = HopLength(beboo);
    _distances[beboo] += length;
    beboo.Position += new Vector3(length, 0, 0);
    beboo.Destination = beboo.Position;
    GameHost.Current.SoundSystem.PlayBebooSound(GameHost.Current.SoundSystem.BoingSounds, beboo, false);
  }

  /// <summary>A rested, happy beboo bounces further, so looking after it is what wins this.</summary>
  private static int HopLength(Beboo beboo)
  {
    int fromMood = Math.Clamp(beboo.Happiness, 0, 10) / 3;
    // Tiredness is the biggest single term here: an exhausted beboo barely leaves the ground.
    int fromEnergy = beboo.EnergyLevel switch
    {
      EnergyStage.Energetic => 5,
      EnergyStage.Ok => 4,
      EnergyStage.LittleTired => 2,
      EnergyStage.Tired => 1,
      _ => 0,
    };
    int fromAge = (int)beboo.Age / 3;
    return 1 + GameHost.Current.Random.Next(3) + fromMood + fromEnergy + fromAge;
  }

  private Beboo Winner() => _jumpers.OrderByDescending(beboo => _distances[beboo]).First();

  private void End()
  {
    Beboo winner = Winner();
    foreach (Beboo beboo in _jumpers)
    {
      if (beboo != _mainBeboo) beboo.Pause();
    }
    GameHost.Current.Map?.Beboos.Clear();
    GameHost.Current.SoundSystem.PlayCinematic(GameHost.Current.SoundSystem.CinematicRaceEnd);
    GameHost.Current.LoadBackedMap();
    GameHost.Current.ChangeMapMusic();
    _mainBeboo.Position = new Vector3(0, 0, 0);
    _mainBeboo.Destination = new Vector3(0, 0, 0);
    Competition.IsRunning = false;
    GameHost.Current.CurrentPlayingMiniGame = null;
    if (winner == _mainBeboo)
    {
      GameHost.Current.GainTicket(2);
      Race.TotalWin++;
      _mainBeboo.Happiness += 2;
      _mainBeboo.Energy -= 1;
    }
    else
    {
      _mainBeboo.Happiness--;
      _mainBeboo.Energy -= 2;
    }
    _phase = Phase.Done;
    GameHost.Current.SwitchToScreen(GameScreen.game);
  }
}
