using BebooGarden.GameCore;
using BebooGarden.GameCore.Speech;
using BebooGarden.Content;
using BebooGarden.GameCore.Pet;
using FmodAudio;
using System.Numerics;

namespace BebooGarden.GameCore.Item.MusicBox;

public class Roll(
    string title,
    string source,
    uint startPCM,
    uint endPCM,
    Sound music,
    bool danse = false,
    bool lullaby = false) : Item
{
  public override string Name { get; } =title;
  public override string Description { get; } = source;

  public override Vector3? Position { get; set; } // position null=in inventory
  public override Channel? Channel { get; set; }
  public override bool IsTakable { get; set; } = true;
  public string Title { get; } = title;
  public string Source { get; set; } = source;
  private uint StartPCM { get; } = startPCM;
  private uint EndPCM { get; } = endPCM;
  private Sound Music { get; } = music;
  public bool Danse { get; } = danse;
  public bool Lullaby { get; } = lullaby;

  public void Play()
  {
    GameHost.Current.SoundSystem.MusicTransition(Music, StartPCM, EndPCM, TimeUnit.PCM);
    if (GameHost.Current.Map != null)
    {
      GameHost.Current.Map.IsLullabyPlaying = Lullaby;
      GameHost.Current.Map.IsDansePlaying = Danse;
    }
  }

  public override void Take()
  {
    GameHost.Current.Map?.Items.Remove(this);
    if (!MusicBox.AvailableRolls.Contains(Title + Source))
    {
      //IGlobalActions.SayLocalizedString("ui.rolltake", Title, Source);
      GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.JingleStar2);
      MusicBox.AvailableRolls.Add(Title + Source);
    }
  }

  public override void PlaySound()
  {
  }
  public override void Buy()
  {
    if (!MusicBox.AvailableRolls.Contains(Title + Source))
    {
      if (GameHost.Current.Save.Tickets - Cost >= 0)
      {
        GameHost.Current.Save.Tickets -= Cost;
        GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.ShopSound);
        Voice.Current.Say(string.Format(BebooText.shop_buy, Name));
        MusicBox.AvailableRolls.Add(Title + Source);
      }
      else
      {
        GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.WarningSound);
Voice.Current.Say(BebooText.shop_notickets);
      }
    }
    else
    {
      GameHost.Current.SoundSystem.System.PlaySound(GameHost.Current.SoundSystem.WarningSound);
      Voice.Current.Say(BebooText.shop_alreadyroll);
    }
  }
  public override void Action() => Take();
  public override void BebooAction(Beboo beboo) => Take();

}