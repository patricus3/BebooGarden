using System;
using BebooGarden.ModApi;

namespace ConfirmPickup;

/// <summary>
/// Asks before picking anything up off the ground.
///
/// The whole mod. The game knows nothing about confirming a pickup: it offers every pickup to
/// whoever is listening, and this takes charge of the ones it cares about, asks, and then settles
/// the request either way.
/// </summary>
public class ConfirmPickupMod : IBebooMod
{
  private IModHost _host = null!;

  public void Start(IModHost host)
  {
    _host = host;
    host.OnPickingUp(Ask);
  }

  private bool Ask(PickupRequest request)
  {
    // Eggs are not picked up, they hatch. Asking whether to pick one up would be the wrong question
    // about the one thing on the ground you cannot undo, so they are left alone.
    if (request.IsEgg) return false;

    // The game's own wording, so this stays translated in every language the game speaks without
    // carrying translations of its own.
    string question = _host.Text("ui.confirmpickup") ?? "Pick up {0}?";
    _host.AskYesNo(string.Format(question, request.ItemName), taking =>
    {
      if (taking) request.Take();
      else request.Cancel();
    });
    return true;
  }
}
