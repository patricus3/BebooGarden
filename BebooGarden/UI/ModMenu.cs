using AccessibleMyraUI;
using BebooGarden.Content;
using BebooGarden.Modding;
using CrossSpeak;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BebooGarden.UI;

/// <summary>
/// The list of mods, shown before the garden opens. One checkbox per mod, plus a confirm button.
///
/// A mod whose creature you already own cannot be switched off: turning it off would leave that
/// beboo with no voice and nothing in the game to get it back with. Those boxes stay ticked and
/// say why when you try.
/// </summary>
public class ModMenu
{
  private readonly Action _onDone;
  private readonly Dictionary<Mod, AccessibleCheckBox> _boxes = [];
  private Panel _panel = new();

  public ModMenu(Action onDone)
  {
    _onDone = onDone;
  }

  public void Show()
  {
    _panel = new Panel();
    VerticalStackPanel grid = new()
    {
      Spacing = 15,
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center
    };
    grid.Widgets.Add(new Label
    {
      Text = BebooText.mods_title,
      HorizontalAlignment = HorizontalAlignment.Center
    });

    foreach (Mod mod in ModManager.All)
    {
      bool locked = !ModManager.CanDisable(mod);
      AccessibleCheckBox box = new(Label(mod, locked), locked || ModManager.IsEnabled(mod))
      {
        Id = $"mod_{mod.Id}"
      };
      Mod captured = mod;
      box.Click += (_, _) => OnToggled(captured);
      _boxes[mod] = box;
      grid.Widgets.Add(box);
    }

    ConfirmButton confirm = new(BebooText.mods_play) { Id = "modsConfirm" };
    confirm.Click += (_, _) => Confirm();
    grid.Widgets.Add(confirm);

    _panel.Widgets.Add(grid);
    Game1.Instance._desktop.Root = _panel;
    Game1.Instance.SwitchToScreen(GameScreen.ModMenu);
    Game1.Instance._desktop.FocusedKeyboardWidget = _boxes.Count > 0 ? _boxes.Values.First() : confirm;
    CrossSpeakManager.Instance.Output(BebooText.mods_title);
  }

  private static string Label(Mod mod, bool locked)
  {
    string name = mod.Creatures.Count > 0
        ? String.Format(BebooText.mods_entrycreatures, mod.DisplayName, mod.Creatures.Count)
        : mod.DisplayName;
    return locked ? String.Format(BebooText.mods_inuse, name) : name;
  }

  /// <summary>Refuses to let a mod go while one of its creatures is alive, and says whose fault it is.</summary>
  private void OnToggled(Mod mod)
  {
    AccessibleCheckBox box = _boxes[mod];
    if (!box.IsChecked && !ModManager.CanDisable(mod))
    {
      box.IsChecked = true;
      Game1.Instance.SoundSystem.System.PlaySound(Game1.Instance.SoundSystem.WarningSound);
      CrossSpeakManager.Instance.Output(String.Format(BebooText.mods_cantdisable,
          mod.DisplayName, String.Join(", ", ModManager.CreaturesInUse(mod))));
    }
  }

  private void Confirm()
  {
    ModManager.SetEnabled(_boxes.Where(pair => pair.Value.IsChecked).Select(pair => pair.Key.Id));
    Game1.Instance.SoundSystem.System.PlaySound(Game1.Instance.SoundSystem.MenuOkSound);
    _onDone();
  }
}
