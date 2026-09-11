namespace BebooGarden.Modding;

/// <summary>
/// Behaviour a mod can switch on by naming it in the "features" list of its manifest.
///
/// Mods are data, not code, so they cannot bring behaviour of their own: the game has to already
/// know how to do the thing, and a feature name is how a mod asks for it. That makes the mod list
/// somewhere to put a preference that only some people want, without growing a settings screen
/// around it - you tick the mod once and that is the whole of it.
///
/// A name this version does not recognise is ignored rather than refused, so a mod written against
/// a later version still loads here.
/// </summary>
public static class ModFeatures
{
  /// <summary>
  /// Ask before picking something up off the ground, instead of taking it the moment you press
  /// enter next to it.
  /// </summary>
  public const string ConfirmPickup = "confirmPickup";
}
