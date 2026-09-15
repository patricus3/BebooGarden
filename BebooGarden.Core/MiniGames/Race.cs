using System;
using System.Collections.Generic;
using System.Numerics;
using BebooGarden.GameCore;
using BebooGarden.GameCore.Pet;
using BebooGarden.GameCore.World;
using BebooGarden.MiniGames;
using BebooGarden.GameCore.Input;
using Vector3 = System.Numerics.Vector3;

namespace BebooGarden.Minigame;

public class Race : IMiniGame
{
  public string Tips { get; set; } = "";
  public static Dictionary<RaceType, double> RaceScores { get; set; } = new();
  public static int TotalWin { get; set; }
  public static readonly int BASERACELENGTH = 60;
  /// <summary>Alias of the competition centre's flag: only one contest runs at a time.</summary>
  public static bool IsARaceRunning
  {
    get => Competition.IsRunning;
    set => Competition.IsRunning = value;
  }
  public bool IsRunning => IsARaceRunning;
  public int Length { get; set; }
  public DateTime StartTime;
  private Beboo MainBeboo { get; }
  private RaceType RaceType { get; }
  static Race()
  {
    foreach (RaceType raceType in Enum.GetValues(typeof(RaceType)))
    {
      RaceScores[raceType] = 0;
    }
  }
  public Race(RaceType raceType, Beboo mainBeboo)
  {
    RaceType = raceType;
    switch (raceType)
    {
      case RaceType.Base:
        Length = BASERACELENGTH; break;
      case RaceType.Snowy:
        Length = BASERACELENGTH; break;
    }
    MainBeboo = mainBeboo;
  }
  public void Start()
  {
    if (IsARaceRunning) return;
    IsARaceRunning = true;
    Competition.UseATry(CompetitionType.Race);
    switch (RaceType)
    {
      case RaceType.Base: GameHost.Current.ChangeMap(Map.BasicRace); break;
      case RaceType.Snowy: GameHost.Current.ChangeMap(Map.SnowyRace); break;
    }
    GameHost.Current.Map?.Beboos.Add(MainBeboo);
    MainBeboo.Unpause();
    Vector3 startPos = new(-Length / 2, 0, 0);
    MainBeboo.Position = startPos;
    MainBeboo.Destination = startPos;
    GameHost.Current.Map?.Beboos.Add(new Beboo("bob", BebooType.Pink, 1, DateTime.Now, GameHost.Current.Random.Next(6), 3, GameHost.Current.Random.Next(8), true, 1.3f));
    GameHost.Current.Map.Beboos[1].Position = startPos + new Vector3(0, 2, 0);
    GameHost.Current.Map?.Beboos.Add(new Beboo("boby", BebooType.Green, 1, DateTime.Now, GameHost.Current.Random.Next(6), 3, GameHost.Current.Random.Next(8), true, 1.2f));
    GameHost.Current.Map.Beboos[2].Position = startPos + new Vector3(0, -2, 0);
    foreach (Beboo racer in GameHost.Current.Map?.Beboos ?? []) racer.SetCompetitionPace();
    switch (RaceType)
    {
      case RaceType.Base: GameHost.Current.SoundSystem.PlayRaceMusic(); break;
      case RaceType.Snowy: GameHost.Current.SoundSystem.PlayRaceLolMusic(); break;
    }
    GameHost.Current.SoundSystem.PlayCinematic(GameHost.Current.SoundSystem.CinematicRaceStart, true);
    StartTime = DateTime.Now;
  }
  public void End((int, double) third, (int, double) second, (int, double) first)
  {
    double contesterScore = 0;
    if (third.Item1 == 0) contesterScore = third.Item2;
    else if (second.Item1 == 0) contesterScore = second.Item2;
    else if (first.Item1 == 0) contesterScore = first.Item2;
    RaceScores[this.RaceType] = contesterScore;
    GameHost.Current.CurrentPlayingMiniGame = null;
    GameHost.Current.Ui.ShowRaceResult(third, second, first, MainBeboo, RaceType);
  }
  (int, double) first = (-1, 0), second = (-1, 0), third = (-1, 0);

  bool _secondArrived = false;
  public void Update(IInputFrame input)
  {
    for (int i = 0; i < GameHost.Current.Map?.Beboos.Count; i++)
    {
      Beboo? beboo = GameHost.Current.Map?.Beboos[i];
      if (beboo != null)
      {
        float bebooY = beboo.Position.Y;
        beboo.Destination = new Vector3(Length / 2, bebooY, 0);
        double totalSecond = Math.Round((DateTime.Now - StartTime).TotalSeconds, 2);
        if (beboo.Position.X >= Length / 2 || totalSecond >= 60 || _secondArrived)
        {
          double score = totalSecond;
          if (i != first.Item1 && i != second.Item1 && i != third.Item1)
          {
            if (first.Item1 == -1)
            {
              first = (i, score);
            }
            else if (second.Item1 == -1)
            {
              second = (i, score);
              _secondArrived = true;
            }
            else if (third.Item1 == -1)
            {
              if (_secondArrived)
                third = (i, 0);
              else
                third = (i, score);
            }
          }
        }
      }
    }
    GameHost.Current.SoundSystem.MovePlayerTo(MainBeboo.Position);
    if (first.Item1 != -1 && second.Item1 != -1 && third.Item1 != -1)
    {
      End(third, second, first);
    }
  }
  public static int GetRemainingTriesToday() => Competition.GetRemainingTriesToday(CompetitionType.Race);
}
