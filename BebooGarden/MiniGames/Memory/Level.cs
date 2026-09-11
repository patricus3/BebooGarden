using BebooGarden.Content;
using BebooGarden.Interface;
using CrossSpeak;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BebooGarden.Minigame.memory;

internal class Level
{
  private int Retry { get; set; }
  private int NbSounds { get; }
  private int MaxRetry { get; set; }
  private List<(int, CaseState)> Grid { get; set; }
  private SoundSystem SoundSystem { get; set; }
  public bool Win { get; internal set; } = true;
  public bool Ended { get; private set; }

  public Level(SoundSystem soundSystem, int nbSounds, int maxRetry, string group1, string? group2 = null)
  {
    NbSounds = nbSounds;
    MaxRetry = maxRetry;
    Grid = new List<(int, CaseState)>();
    FillGridByRandomInt();
    SoundSystem = soundSystem;
    SoundSystem.LoadLevel(nbSounds, group1, group2);
    // The game said nothing whatsoever when it opened: no statement of what it is, that it wants
    // the number keys, how many cases there are, or how to leave. On a game played by ear that is
    // not a quiet start, it is indistinguishable from being stuck.
    CrossSpeakManager.Instance.Output(
        String.Format(BebooText.memory_start, nbSounds * 2, maxRetry));
  }

  private void FillGridByRandomInt()
  {
    var rnd = new Random();
    var randomDisposition = Enumerable.Range(1, NbSounds).Concat(Enumerable.Range(1, NbSounds)).OrderBy(_ => rnd.Next()).ToArray();
    foreach (int n in randomDisposition)
    {
      Grid.Add((n, CaseState.None));
    }
  }

  public void Update(GameTime gameTime, KeyboardState currentKeyboardState)
  {
    if (Ended) return;
    // A way out. There was none: the alt F4 branch that used to sit at the bottom was nested inside
    // the digit check, so it could only fire if you held a digit at the same time, and nothing else
    // ended a run. You kept whatever you had scored up to here.
    if (Game1.Instance.IsKeyPressed(currentKeyboardState, Keys.Escape))
    {
      CrossSpeakManager.Instance.Output(BebooText.memory_quit);
      Release();
      Win = false;
      Ended = true;
      return;
    }
    // D5 was missing from this list while every other digit was here, so the fifth case could not
    // be turned over from the number row at all. A grid always has six or eight cases, so one of
    // them was permanently unreachable and no level could ever be cleared.
    if (Game1.Instance.IsKeyPressed(currentKeyboardState, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8))
    {
      var key = currentKeyboardState.GetPressedKeys()[0];
      if (BebooGarden.Util.IsKeyDigit(key, out int keyInt) && keyInt <= NbSounds * 2 && keyInt > 0)
      {
        int caseIndex = keyInt - 1;
        int soundIndex = Grid[caseIndex].Item1 - 1;
        SoundSystem.PlayQueue(SoundSystem.Sounds[soundIndex], queued: false);
        TryCase(soundIndex + 1, caseIndex);
        if (Grid.All(pair => pair.Item2 == CaseState.Paired))
        {
          CrossSpeakManager.Instance.Output(BebooText.win);
          SoundSystem.PlayQueue(SoundSystem.JingleWin);
          Release();
          Win = true;
          Ended = true;
        }
        // Strictly greater: MaxRetry is how many mistakes you may make, which is what the game
        // announces at the start, so the last one you are promised should not be the one that ends
        // the run.
        else if (Retry > MaxRetry)
        {
          CrossSpeakManager.Instance.Output(BebooText.lose);
          SoundSystem.PlayQueue(SoundSystem.JingleLose);
          Release();
          Win = false;
          Ended = true;
        }
      }
    }
  }

  private void TryCase(int soundIndex, int caseIndex)
  {
    var caseIndexTouched = Grid.IndexesWhere(o => o.Item1 == soundIndex && o.Item2 == CaseState.Touched).ToList();
    var touchedCases = Grid.IndexesWhere(o => o.Item2 == CaseState.Touched).ToList();
    if (Grid[caseIndex].Item2 == CaseState.Paired || caseIndexTouched.Count == 1 && caseIndexTouched.Contains(caseIndex))
    {
      SoundSystem.PlayQueue(SoundSystem.JingleError);
    }
    else if (caseIndexTouched.Count == 1 && !caseIndexTouched.Contains(caseIndex))
    {
      Grid[caseIndexTouched[0]] = (soundIndex, CaseState.Paired);
      Grid[caseIndex] = (soundIndex, CaseState.Paired);
      SoundSystem.PlayQueue(SoundSystem.JingleCaseWin);
    }
    else if (touchedCases.Count == 1)
    {
      Grid[touchedCases[0]] = (Grid[touchedCases[0]].Item1, CaseState.None);
      Retry++;
      SoundSystem.PlayQueue(SoundSystem.JingleCaseLose);
    }
    else if (caseIndexTouched.Count == 0)
    {
      Grid[caseIndex] = (Grid[caseIndex].Item1, CaseState.Touched);
    }
  }
  public void Release()
  {
    // Never wait for those tasks from here. They run on the thread pool and wait for channels to
    // finish; this runs on the game thread. The minigame's sound system was never being updated, so
    // a channel could stay "playing" forever, and Task.WaitAll then stopped the entire game dead.
    SoundSystem.Stop();
    SoundSystem.FreeRessources();
  }
}