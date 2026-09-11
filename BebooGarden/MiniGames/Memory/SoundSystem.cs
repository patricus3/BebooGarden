using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FmodAudio;

namespace BebooGarden.Minigame.memory;
public class SoundSystem
{
  private const string CONTENTFOLDER = "Content/boombox/";
  public FmodSystem System { get; }
  public Sound[] Sounds { get; set; }
  public Channel[] Channels { get; set; }
  private Vector3 ListenerPos = new() { Z = -1.0f };
  public Vector3 Up = new(0, 1, 0), Forward = new(0, 0, -1);
  public int MaxSounds { get; set; }
  public List<Channel> Musics { get; private set; }
  public Sound JingleCaseWin { get; private set; }
  public Sound JingleCaseLose { get; private set; }
  public Sound JingleWin { get; private set; }
  public Sound JingleLose { get; private set; }
  public Sound JingleError { get; private set; }
  public float Volume { get { return System.MasterSoundGroup.GetValueOrDefault().Volume; } set { System.MasterSoundGroup.GetValueOrDefault().Volume = value; } }

  public SoundSystem(float initialVolume)
  {
    //Creates the FmodSystem object
    System = Fmod.CreateSystem();
    //System object Initialization
    System.Init(4093, InitFlags._3D_RightHanded);
    Volume = initialVolume;
    //Set the distance Units (Meters/Feet etc)
    System.Set3DSettings(1.0f, 1.0f, 1.0f);
    System.Set3DListenerAttributes(0, in ListenerPos, default, in Forward, in Up);
    Sounds = Array.Empty<Sound>();
    Channels = Array.Empty<Channel>();
    Musics = new List<Channel>();
  }

  public void LoadLevel(int maxSounds, string group1, string? group2 = null)
  {
    //Load sounds
    MaxSounds = maxSounds;
    Sounds = new Sound[maxSounds];
    LoadLevelSounds(group1, group2);
    Musics = new List<Channel>();
    Channels = new Channel[MaxSounds];
    LoadLevelMusics();
  }
  public void LoadMenu()
  {
    Sound sound;
  }
  private void LoadLevelSounds(string group1, string? group2)
  {
    Sound sound;
    var rnd = new Random();
    if (group2 == null)
    {
      string[] files = Directory.GetFiles(group1, "*.*", new EnumerationOptions() { RecurseSubdirectories = true });
      var rndArray = Enumerable.Range(0, files.Length).OrderBy(item => rnd.Next()).ToArray();
      for (int i = 0; i < MaxSounds; i++)
      {
        Sounds[i] = sound = System.CreateSound(files[rndArray[i]], Mode.Loop_Off);
      }
    }
    else
    {
      var files1 = Directory.GetFiles(group1, "*.wav", new EnumerationOptions() { RecurseSubdirectories = true });
      var rndArray1 = Enumerable.Range(0, files1.Length).OrderBy(item => rnd.Next()).ToArray();
      var files2 = Directory.GetFiles(group2, "*.wav", new EnumerationOptions() { RecurseSubdirectories = true });
      var rndArray2 = Enumerable.Range(0, files2.Length).OrderBy(item => rnd.Next()).ToArray();
      for (int i = 0; i < MaxSounds; i++)
      {
        if (i % 2 == 0) Sounds[i] = sound = System.CreateSound(files1[rndArray1[i]], Mode._3D | Mode.Loop_Off | Mode._3D_LinearSquareRolloff);
        else Sounds[i] = sound = System.CreateSound(files2[rndArray2[i]], Mode._3D | Mode.Loop_Off | Mode._3D_LinearSquareRolloff);
      }
    }
  }
  private void LoadLevelMusics()
  {
    Sound sound;
    sound = System.CreateStream(CONTENTFOLDER + "music/OTOATE.wav", Mode.Loop_Normal);
    Channel channel = System.PlaySound(sound, paused: false);
    channel.SetLoopPoints(TimeUnit.MS, 5201, TimeUnit.MS, sound.GetLength(TimeUnit.MS) - 1);
    Musics.Add(channel);
    JingleCaseWin = System.CreateSound(CONTENTFOLDER + "music/Jingle_SLVSTAR1.mp3");
    JingleCaseLose = System.CreateSound(CONTENTFOLDER + "music/Jingle_DROPSTAR.mp3");
    JingleWin = System.CreateSound(CONTENTFOLDER + "music/Jingle_MINICLEAR.mp3");
    JingleLose = System.CreateSound(CONTENTFOLDER + "music/Jingle_MINIOVER.mp3");
    JingleError = System.CreateSound(CONTENTFOLDER + "music/SM64_Error.ogg");
  }

  public List<Task> tasks = new();

  /// <summary>
  /// Set when the minigame is finishing. Every wait below watches it, so a task can never be left
  /// spinning on a sound that is not going to end.
  /// </summary>
  private volatile bool _stopping;

  /// <summary>Tells the waiting tasks to give up. They stop within a few milliseconds.</summary>
  public void Stop() => _stopping = true;

  /// <summary>
  /// Waits for a channel to finish, or for the minigame to be shutting down. Sleeps rather than
  /// spinning: this runs on a pool thread and used to burn a core solid while it waited.
  /// </summary>
  private void WaitFor(Channel channel)
  {
    while (!_stopping && channel.IsPlaying) Thread.Sleep(5);
  }

  public void PlayQueue(Sound sound, bool queued = true)
  {
    tasks.Add(Task.Factory.StartNew(() =>
    {
      try
      {
        if (queued)
        {
          // Wait for the queue to drain before adding to it. This had no sleep in it, so it span
          // as fast as the processor would allow.
          int real;
          do
          {
            if (_stopping) return;
            System.GetChannelsPlaying(out int _, out real);
            if (real > 2) Thread.Sleep(5);
          } while (real > 2);
        }
        if (_stopping) return;
        Channel? channel = System.PlaySound(sound, paused: false);
        if (channel == null) return;
        WaitFor(channel);
        channel.Stop();
      }
      catch (Exception)
      {
        // The system was released while this was waiting on it. Nothing left to play.
      }
    }));
  }

  public void FreeRessources()
  {
    Musics.ForEach((music) =>
    {
      music?.Stop();
    });
    Musics.Clear();
  }
}
