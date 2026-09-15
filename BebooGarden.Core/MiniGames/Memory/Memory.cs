using BebooGarden.GameCore.Speech;
using BebooGarden.Content;
using BebooGarden.GameCore;
using BebooGarden.GameCore.Pet;
using BebooGarden.MiniGames;
using FmodAudio;
using BebooGarden.GameCore.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace BebooGarden.Minigame.memory;

public class Memory : IMiniGame
{
  private static bool AlreadyPlaied = false;
  private Random _random;
  private List<string> _groups;
  private Level? _level;

  public FmodSystem System { get; }
  public SoundSystem SoundSystem { get; set; }
  private int MaxScore { get; set; }

  public bool IsRunning { get; set; }

  public string Tips => "";

  public int Score { get; set; }

  private readonly string CONTENTFOLDER = "Content/boombox/sounds/";

  public Memory(float volume)
  {
    SoundSystem = new SoundSystem(MemoryVolumeFor(volume));
  }

  /// <summary>The memory's own sounds are mastered louder than the garden's, so they come down a bit.</summary>
  private static float MemoryVolumeFor(float volume) => Math.Max(0f, volume - 0.3f);

  public void SetVolume(float volume) => SoundSystem.Volume = MemoryVolumeFor(volume);

  public void Start()
  {
    IsRunning = true;
    SoundSystem.FreeRessources();
    GameHost.Current.Pause();
    if (!AlreadyPlaied)
    {
      //IWindowManager.ShowTalk("welcome");
      //IWindowManager.ShowTalk("goal");
      AlreadyPlaied = true;
    }
    SoundSystem.LoadMenu();
    _random = new Random();
    // Concat returns a new sequence rather than adding to this one, so the boombox groups were
    // built and then dropped on the floor: every level was drawn from beboo voices alone.
    _groups = [
      .. Directory.GetDirectories(Path.Combine(BebooGarden.SoundSystem.CONTENTFOLDER, BebooGarden.SoundSystem.BEBOOSOUNDSFOLDER)),
      .. Directory.GetDirectories(CONTENTFOLDER),
    ];
    Score = 0;
    StartNewLevel();
  }

  public void Update(IInputFrame input)
  {
    // The minigame has an FMOD system of its own, separate from the game's, and nothing was ever
    // updating it. Its channels therefore never reported themselves finished, which is what left
    // the sound tasks waiting forever.
    SoundSystem.System.Update();
    _level?.Update(input);
    if ((_level?.Ended??false) && (_level?.Win ?? false))
    {
      Score++;
      _level = null;
      StartNewLevel();
    } else if((_level?.Ended??false) && !(_level?.Win??false))
    {
      _level = null;
      Voice.Current.Say(String.Format(BebooText.score, Score));
      End();
    }
  }

  private void End()
  {
    if (MaxScore < Score) MaxScore = Score;
    GameHost.Current.Unpause();
    // Let the waiting tasks go before the system they are waiting on disappears, but never wait
    // longer than a moment: finishing the minigame must not be able to hang the game.
    SoundSystem.Stop();
    Task.WaitAll(SoundSystem.tasks.ToArray(), 500);
    SoundSystem.System.Release();
    IsRunning = false;
    // The whole point of the treasure chest, and it was commented out: you played the memory game
    // and were given nothing at all for it.
    GameHost.Current.GainTicket(Score / 4);
/*
    beboo.Age += ((int)score / 5) * 0.1f;
    if (score > 0) beboo.Happiness++;
    else beboo.Happiness--;
  */
    //GameHost.Current.SoundSystem.PlaySoundAtPosition(GameHost.Current.SoundSystem.ItemChestCloseSound, (Vector3)Position);
    GameHost.Current.CurrentPlayingMiniGame = null;
  }

  private void StartNewLevel()
  {
    var group1 = _groups[_random.Next(_groups.Count)];
    string? group2 = null;
    int nbSounds = 4;
    if (Score % 3 == 0 && Score != 0)
    {
      do
      {
        group2 = _groups[_random.Next(_groups.Count)];
      } while (group1 == group2);
    }
    else if (Score % 4 == 0 || Score == 0)
    {
      nbSounds = 3;
      group1 = _groups[0];
    }
    // Mistakes scale with the size of the grid instead of being a flat three. Three across eight
    // cases meant playing very nearly perfectly from the first guess, which is less a memory game
    // than a coin toss. It tightens as you get further in, but never below one per pair.
    int maxRetry = nbSounds + 2 - Math.Min(2, Score / 6);
    _level = new Level(SoundSystem, nbSounds, maxRetry, group1, group2);
  }
}