using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using BebooGarden.Content;
using BebooGarden.GameCore;
using BebooGarden.GameCore.Pet;
using Microsoft.Xna.Framework.Input;

namespace BebooGarden;

public static class Util
{
  public static readonly Vector3[] DIRECTIONS = [new(0, 1, 0), new(1, 0, 0), new(-1, 0, 0), new(0, -1, 0)];
  public static readonly string[] Colors =
      ["pink", "red", "orange", "yellow", "green", "blue", "indigo", "violet", "none"];

  /// <summary>
  /// Looks a name up by its resource key, for the things stored as english identifiers: colors in
  /// the save, fruit species names off the enum. Falls back to the identifier itself.
  /// </summary>
  public static string Localized(string key)
      => BebooText.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

  public static string LocalizedColor(string color) => Localized(color);

  public static bool IsInSquare(Vector3 otherPoint, Vector3 center, int halfSideSize)
  {
    bool isInX = Math.Abs(otherPoint.X - center.X) <= halfSideSize;
    bool isInY = Math.Abs(otherPoint.Y - center.Y) <= halfSideSize;
    return isInX && isInY;
  }

  public static IEnumerable<string> SplitToLines(this string input)
  {
    if (input == null) yield break;

    using StringReader reader = new(input);
    while (reader.ReadLine() is { } line) yield return line;
  }
  public static bool IsKeyDigit(Keys key, out int keyInt)
  {
    return int.TryParse(key.ToString().Replace("NumPad", "").Replace("D", ""), out keyInt);
  }
  /// <summary>One of the colours a beboo can be, never "none".</summary>
  public static string RandomColor()
  {
    var colors = Array.FindAll(Colors, color => color != "none");
    return colors[Game1.Instance.Random.Next(colors.Length)];
  }

  public static BebooType GetRandomBebooType()
  {
    var bebooTypes = Enum.GetValues(typeof(BebooType));
    return (BebooType)bebooTypes.GetValue(Game1.Instance.Random.Next(1, bebooTypes.Length));
  }

  public static BebooType GetBebooTypeByColor(string color)
  {
    return color switch
    {
      "none" => BebooType.Base,
      "pink" => BebooType.Pink,
      "red" => BebooType.Red,
      "orange" => BebooType.Orange,
      "yellow" => BebooType.Yellow,
      "green" => BebooType.Green,
      "blue" => BebooType.Blue,
      "indigo" => BebooType.Indigo,
      "violet" => BebooType.Violet,
      _ => BebooType.Base,
    };
  }

  /// <summary>
  /// The colour a beboo type goes by, as the english key its translation is stored under. Null for
  /// the base type, the one type that has no colour to give.
  /// </summary>
  public static string? ColorOfBebooType(BebooType bebooType)
  {
    return bebooType switch
    {
      BebooType.Pink => "pink",
      BebooType.Red => "red",
      BebooType.Orange => "orange",
      BebooType.Yellow => "yellow",
      BebooType.Green => "green",
      BebooType.Blue => "blue",
      BebooType.Indigo => "indigo",
      BebooType.Violet => "violet",
      _ => null,
    };
  }
}