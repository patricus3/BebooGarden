using BebooGarden.GameCore;
using Newtonsoft.Json;
using System;
using System.IO;

namespace BebooGarden.Save;

public class SaveManager
{
  private const string DATAFILEPATH = "save.dat";

  private static readonly JsonSerializerSettings Settings = new()
  {
    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
    NullValueHandling = NullValueHandling.Ignore,
    DefaultValueHandling = DefaultValueHandling.Populate,
    TypeNameHandling = TypeNameHandling.All
  };

  internal static SaveParameters LoadSave()
  {
    Game1.Instance.SoundSystem.LoadMenuSounds();
    SaveParameters parameters = LoadJson() ?? new SaveParameters();
    //if (parameters.Flags.NewGame) Welcome.BeforeGarden(parameters);
    return parameters;
  }

  private static SaveParameters? LoadJson()
  {
    if (!File.Exists(DATAFILEPATH)) return null;
    string json = File.ReadAllText(DATAFILEPATH);
    try
    {
      return JsonConvert.DeserializeObject<SaveParameters>(json, Settings);
    }
    catch (JsonException)
    {
      // Anything we can't read, an encrypted save from an older version included, starts over
      // rather than taking the game down on startup.
      return null;
    }
  }


  public static void WriteSave(SaveParameters parameters)
  {
    string json = JsonConvert.SerializeObject(parameters, Settings);
    File.WriteAllText(DATAFILEPATH, json);
  }
}