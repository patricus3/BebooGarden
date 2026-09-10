using System;
using System.Numerics;
using System.Threading.Tasks;
using BebooGarden.Content;
using CrossSpeak;

namespace BebooGarden.GameCore.Pet;

public partial class Beboo
{
  /// <summary>
  /// Under that interval between two sways, the beboo is being shaken, not rocked. Movement input
  /// only ticks every 150 ms, so this has to sit well above that: everything a player can produce
  /// by mashing left and right has to land on the shaking side of it.
  /// </summary>
  private const int SWAYTOOFASTMS = 450;
  /// <summary>Above that interval, two sways don't belong to the same rocking rhythm anymore.</summary>
  private const int SWAYRHYTHMMAXMS = 1600;
  /// <summary>After that silence, the whole sway chain is forgotten.</summary>
  private const int SWAYCHAINTIMEOUTMS = 2500;
  private const int SWAYSTOCRY = 4;
  private const int SWAYSTOSLEEP = 8;
  /// <summary>How many wake up attempts a beboo asleep in your arms shrugs off before really waking.</summary>
  private const int CRADLEWAKERESISTANCE = 3;
  /// <summary>Wake up attempts closer than that to the previous one don't even count as attempts.</summary>
  private const int CRADLEWAKEATTEMPTMS = 1500;
  /// <summary>How long a beboo stays too rattled to sleep after being shaken.</summary>
  private const int SHAKENSETTLEMS = 6000;

  private int _swayDistress;
  private int _swayLull;
  private DateTime _lastSway = DateTime.MinValue;
  private int _cradleWakeResistance;
  private DateTime _lastCradleWakeAttempt = DateTime.MinValue;
  private DateTime _lastHardSway = DateTime.MinValue;

  /// <summary>
  /// Being thrown around is not a lullaby: for a while after a hard sway the beboo is too worked
  /// up to fall asleep, however tired it is and whatever the music box is playing.
  /// </summary>
  public bool IsBeingShaken =>
      IsHeld && (DateTime.Now - _lastHardSway).TotalMilliseconds < SHAKENSETTLEMS;

  public bool IsHeld { get; private set; }

  public void PickUp()
  {
    if (IsHeld) return;
    IsHeld = true;
    AbandonErrand();
    ResetSwayChain();
    _lastHardSway = DateTime.MinValue;
    Destination = null;
    Position = Game1.Instance.PlayerPosition;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.GrassSound, this, false);
    if (Sleeping) StartCradleWakeResistance();
    else Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooSurpriseSounds, this);
    CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_pickup, Name));
  }

  public void PutDown(bool announce = true)
  {
    if (!IsHeld) return;
    IsHeld = false;
    ResetSwayChain();
    _lastHardSway = DateTime.MinValue;
    _cradleWakeResistance = 0;
    Position = Game1.Instance.PlayerPosition;
    if (!Sleeping)
    {
      MoveBehaviour.Done();
      FancyMoveBehaviour.Done();
    }
    if (announce)
    {
      Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.GrassSound, this, false);
      if (!Sleeping) Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooCuteSounds, this);
      CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_putdown, Name));
    }
  }

  /// <summary>
  /// Keeps a carried beboo, and whatever it is in the middle of saying, with the player. A channel
  /// is placed once when the sound starts, so without this its voice stays behind on the tile you
  /// picked it up from as you walk off.
  /// </summary>
  private void CarryAlong()
  {
    Position = Game1.Instance.PlayerPosition;
    try
    {
      if (Channel != null && Channel.IsPlaying)
        Channel.Set3DAttributes(Position + new Vector3(0, 0, -2), default, default);
    }
    catch (FmodAudio.FmodException)
    {
      // The channel finished between the check and the move; nothing to follow any more.
    }
  }

  /// <summary>
  /// One half of a rocking motion. The delay since the previous one is what tells a lullaby
  /// from a shaking: rock in rhythm and the beboo dozes off, jerk it around and it cries.
  /// </summary>
  public void Sway(bool toLeft)
  {
    if (!IsHeld) return;
    double sinceLastSway = (DateTime.Now - _lastSway).TotalMilliseconds;
    _lastSway = DateTime.Now;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.GrassSound, this, false);
    if (sinceLastSway > SWAYCHAINTIMEOUTMS)
    {
      _swayDistress = 0;
      _swayLull = 0;
    }
    else if (sinceLastSway < SWAYTOOFASTMS)
    {
      _lastHardSway = DateTime.Now;
      _swayLull = 0;
      _swayDistress++;
      if (Sleeping) WakeUp(true);
      if (_swayDistress >= SWAYSTOCRY) BurstInTearrsFromSwaying();
    }
    else if (sinceLastSway <= SWAYRHYTHMMAXMS)
    {
      if (_swayDistress > 0) _swayDistress--;
      // One jolt is enough to stop this counting as soothing, whatever rhythm follows it.
      if (Sleeping || IsBeingShaken) return;
      _swayLull++;
      if (_swayLull >= SWAYSTOSLEEP) FallAsleepInArms();
      else if (_swayLull % 3 == 0)
        Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooDelightSounds, this, true, 0.4f);
    }
    else _swayDistress = 0;
  }

  private void BurstInTearrsFromSwaying()
  {
    ResetSwayChain();
    WakeUp(true);
    Happiness -= 3;
    Energy--;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooWailSounds, this);
    BurstInTearrs();
    CrossSpeakManager.Instance.Output(String.Format(BebooText.beboo_swaytoohard, Name));
    Task.Run(async () =>
    {
      await Task.Delay(800);
      Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooCrySounds, this);
    });
  }

  private void FallAsleepInArms()
  {
    ResetSwayChain();
    Happiness++;
    GoAsleep(true);
  }

  private void ResetSwayChain()
  {
    _swayDistress = 0;
    _swayLull = 0;
    _lastSway = DateTime.MinValue;
  }

  internal void StartCradleWakeResistance()
  {
    if (!IsHeld) return;
    _cradleWakeResistance = CRADLEWAKERESISTANCE;
    _lastCradleWakeAttempt = DateTime.Now;
  }

  /// <summary>
  /// Snuggled in your arms, a sleeping beboo shrugs off whistles and pushy friends for a while.
  /// Returns true when this wake up attempt should be ignored.
  /// </summary>
  private bool ResistCradleWakeUp()
  {
    if (!IsHeld || !Sleeping || _cradleWakeResistance <= 0) return false;
    if ((DateTime.Now - _lastCradleWakeAttempt).TotalMilliseconds < CRADLEWAKEATTEMPTMS) return true;
    _lastCradleWakeAttempt = DateTime.Now;
    _cradleWakeResistance--;
    Game1.Instance.SoundSystem.PlayBebooSound(Game1.Instance.SoundSystem.BebooSleepingSounds, this, false, 0.3f);
    return _cradleWakeResistance > 0;
  }
}
