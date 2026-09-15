using BebooGarden.GameCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BebooGarden.GameCore.Pet;

public partial class Beboo
{
  public void Update()
  {
    if (Paused) return;
    RunDueWork();
    if (IsHeld) CarryAlong();
    // BeHappy is reached from a delayed task, so the music switch lands here on the main thread.
    RefreshMoodMusic();
    if (CuteBehaviour.ItsTime())
    {
      if (!Sleeping)
      {
        DoCuteThing();
        CuteBehaviour.Done();
      }
    }
    if (MoveBehaviour.ItsTime())
    {
      if (!Sleeping && !IsHeld)
      {
        MoveTowardGoal();
        MoveBehaviour.Done();
      }
    }
    if (GoToSleepOrWakeUpBehaviour.ItsTime())
    {
      if (!Sleeping && Energy <= MaxEnergy * SleepyAt && !OnAnErrand)
      {
        GoToBed();
        GoToSleepOrWakeUpBehaviour.Done();
      }
      else if (Sleeping && Energy >= MaxEnergy * RESTEDAT)
      {
        WakeUp();
        GoToSleepOrWakeUpBehaviour.Done();
      }
    }
    if (FancyMoveBehaviour.ItsTime())
    {
      if (!Sleeping && !Racer && !IsHeld && !OnAnErrand)
      {
        if (Happy || (!Happy && GameHost.Current.Random.Next(3) == 1))
          WannaGoToRandomPlace();
        FancyMoveBehaviour.Done();
      }
    }
    if (GoingTiredBehaviour.ItsTime())
    {
      if (!Sleeping)
      {
        Energy--;
        GoingTiredBehaviour.Done();
      }
    }
    if (GoingSadBehaviour.ItsTime())
    {
      if (!Sleeping && !Racer)
      {
        Happiness--;
        GoingSadBehaviour.Done();
      }
    }
    if (!Racer && !Sleeping && EmotionBehaviour.ItsTime())
    {
      if (Happy && Happiness <= 0)
        BurstInTearrs();
      else if (!Happy && Happiness >= CHEEREDUPAT)
      {
        Later(1000, BeHappy);
      }
      if (Energy > 5 && Happiness >= 9)
        BeOverexcited();
      else if (Happiness <= 0 && Energy < 5)
        BeFloppy();
      else BeNormal();
      EmotionBehaviour.Done();
    }
    UpdateErrand();
    if (PresentBehaviour.ItsTime())
    {
      StartErrand();
      PresentBehaviour.Done();
    }
    if (CryBehaviour.ItsTime())
    {
      if (!Sleeping && !Racer)
      {
        GameHost.Current.SoundSystem.PlayBebooSound(GameHost.Current.SoundSystem.BebooCrySounds, this);
        CryBehaviour.Done();
      }
    }
    if (SleepingBehaviour.ItsTime())
    {
      if (Sleeping)
      {
        Energy += SleepRecovery();
        GameHost.Current.SoundSystem.PlayBebooSound(GameHost.Current.SoundSystem.BebooSleepingSounds, this, true, 0.3f);
        SleepingBehaviour.Done(); ;
      }
    }
    //+0.1 every 3mn=1lvl/30mn
    if (!Racer && GrowthBehaviour.ItsTime())
    {
      if (Energy >= 2 && Happiness >= 2)
      {
        Age += 0.1f;
        GrowthBehaviour.Done();
      }
    }
  }
}
