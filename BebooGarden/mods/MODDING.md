# Modding Beboo Garden: Enhanced Edition — reference

Version 2.2. A mod is either a single dll you drop in, or a folder of sound files, or both at once.
Creatures need no code at all; changing what the game does needs a little.

---

## 1. Where mods go

Either of two places. **Your own mods go in the second one:**

```
%LocalAppData%\BebooGarden\mods\      your mods; no administrator rights needed
<install folder>\mods\                 the ones that shipped with the game
```

`%LocalAppData%\BebooGarden` is also where your save and `crash.log` live. Paste that path into
Explorer's address bar or the Run box and it will take you there; if the `mods` folder is not there
yet, make it.

A mod is either **one dll** or **one folder**:

```
mods/
  MODDING.md            this file
  confirm-pickup.dll    a mod that is one file, manifest built in
  my-mod/               a mod that needs files on disk
    mod.json            required; a folder without one is ignored
    creatures/
      my-mod.fuzzy/     one folder per creature, named by its id
        cute/           .wav files
        cry/
        ...
```

**A code mod should be a single dll.** Its manifest lives inside it as an embedded resource, so it
installs by being dropped in - nothing to unpack and nothing that can be separated from it. See
section 4.

**A mod that adds creatures needs a folder,** because its voices are .wav files that have to sit
somewhere. Folder names other than `creatures/` are ignored, so you can keep a readme, a licence or
your source next to the manifest without upsetting anything. A folder mod may carry dlls too; they
are all examined.

Both folders are read, shipped mods first. Ids have to be unique, so if you install a mod that uses
an id one of the shipped ones already has, yours is the one ignored - rename it.

---

## 2. mod.json

```json
{
  "id": "my-mod",
  "name": "My Mod",
  "description": "Adds a fuzzy beboo.",
  "creatures": [
    { "id": "my-mod.fuzzy", "name": "Fuzzy" }
  ]
}
```

| field | required | meaning |
|---|---|---|
| `id` | yes | Unique across all mods. Used in the save and as the key everywhere. |
| `name` | no | Shown in the mod list. Falls back to `id`. |
| `description` | no | Not shown yet; write it anyway. |
| `creatures` | no | May be empty or absent. A mod with no creatures loads and does nothing. |

A mod that brings code declares nothing for it: any dll in the folder is examined. See section 4.

Each creature:

| field | required | meaning |
|---|---|---|
| `id` | yes | Unique across **every** mod, not just yours. Prefix it with your mod id. |
| `name` | no | A friendly name for the creature. |

Unrecognised fields are ignored rather than rejected, so a mod written against a later version
still loads whatever this version understands.

### Ids are permanent

A creature's `id` is three things at once: the name of its voice folder, the key its sounds are
loaded under, and **the value written into the player's save**. Rename it after people have played
with it and their beboo loses its voice. Choose it once.

---

## 3. Voices

A creature folder holds one folder per category, each with any number of `.wav` files. The game
picks one at random each time it needs that sound.

| folder | played when |
|---|---|
| `cute` | idle noises — by far the most heard |
| `pet` | being stroked |
| `fun` | playing with an item |
| `inter` | playing with another beboo |
| `song` | singing along with a friend |
| `yumy` | eating |
| `surprise` | startled |
| `angry` | woken up rudely |
| `cry` | sad, and when shaken too hard |
| `yawn` | going to sleep and waking up |
| `sleep` | breathing while asleep |

**Every category is optional.** A missing or empty one falls back to the base beboo voice, so three
good sounds beat eleven mediocre ones. A file FMOD cannot decode is skipped and the rest of the
voice still loads.

### What fits

The shipped voices are **mono 16-bit WAV at 22.05 kHz**. Match that and you will not surprise
anyone. The base beboo sits at roughly **200–1200 Hz**, with sounds between **half a second and
four seconds** long. Stray far outside that and it reads as a different animal rather than a
different beboo — which is fine if that is what you want, but know that you are doing it.

Sounds are positioned in 3D at the beboo, and pitch-shifted per beboo (hatchlings run at
1.0–1.3×), so leave a little headroom rather than mastering to the ceiling.

---

## 4. Code mods

A mod can bring code. The game loads the assembly when the mod is switched on, looks for a public
class with a parameterless constructor implementing `IBebooMod`, makes one, and calls `Start` once,
before the garden opens.

```csharp
using BebooGarden.ModApi;

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
    if (request.IsEgg) return false;
    string question = _host.Text("ui.confirmpickup") ?? "Pick up {0}?";
    _host.AskYesNo(string.Format(question, request.ItemName), taking =>
    {
      if (taking) request.Take(); else request.Cancel();
    });
    return true;
  }
}
```

That is a whole real mod - it is the `confirm-pickup` one, in full.

### Building against the API

Reference **`BebooGarden.ModApi.dll`**, which sits next to `BebooGarden.exe`, and nothing else. It
has no dependencies and exposes none of the game's own types, so a mod built against it keeps
working while the game is rearranged behind it. Install the modding files with the game and you get
its documentation alongside, which is what your editor reads for the tooltips.

Your project should **not** copy the API assembly into your mod folder - the game already has it,
and a second copy is a different type to the runtime, so your `IBebooMod` would not be recognised:

```xml
<ProjectReference Include="path\to\BebooGarden.ModApi.csproj">
  <Private>false</Private>
  <ExcludeAssets>runtime</ExcludeAssets>
</ProjectReference>
```

### One file, manifest included

Build `mod.json` into the assembly and the mod is a single file. The game looks for an embedded
resource called exactly `mod.json`:

```xml
<EmbeddedResource Include="mod.json" LogicalName="mod.json" />
```

`LogicalName` matters: without it the resource is named after your namespace and the game will not
find it. That is the whole of it - drop the dll in `mods` and it is installed.

Reading the manifest means loading the assembly, so the game loads a mod dll to see what it is, and
then constructs and runs nothing at all until the player has switched that mod on.

### What the host offers

| member | what it does |
|---|---|
| `GameVersion` | The game's version, if you need to tell what you are running against. |
| `Log(text)` | A line in `crash.log`, tagged with your mod id. |
| `Say(text)` | Says something through the screen reader. The game is played by ear; this is how you talk to the player. |
| `Text(key)` | One of the game's own translated strings, in the player's language, or null. Reusing these is how a mod stays translated without shipping translations. |
| `AskYesNo(question, answer)` | Asks a yes or no question as a menu and calls back with the answer. Backing out never calls back at all. |
| `OnPickingUp(handler)` | Take part in picking things up off the ground. |

`OnPickingUp` hands you a `PickupRequest`. Return **false** and the game picks the item up as
usual. Return **true** and the game does nothing whatsoever until you call `Take()` or `Cancel()`,
which may be much later - after a question has been answered, for instance. A request you take
charge of and then forget about leaves the item on the ground forever, so settle it either way.

`IsEgg` is on the request because an egg is not really picked up: touching one hatches it. A mod
asking about pickups usually wants to leave those alone.

### When it goes wrong

A mod that throws while starting is written to `crash.log` and dropped, and the game opens without
it. One that throws while handling a pickup is logged and the pickup carries on as if it had
returned false. Somebody else's bug should not cost a player their beboos.

### What is not there yet

One hook, and a small host. This is the shape the rest will grow into rather than the finished
thing, and the API assembly is versioned separately from the game so it can grow without breaking
what already exists.

---

## 5. The mod list

Shown before the garden when there is a mod you have not been asked about yet. One checkbox per
mod; enter or space toggles; choose Play to continue. Choices are saved, and once every mod has
been answered for the list stops appearing at startup.

It is always available from the main menu, under Mods, so a choice can be changed later.

**Enabling controls whether new creatures are offered, not whether files are read.** Every
discovered mod's voices load either way, because a creature already living in the save has to keep
its voice whatever the checkbox says.

**A mod cannot be switched off while the player owns one of its creatures.** The box stays ticked
and names the beboo keeping it there. Turning it off would leave that beboo mute with nothing in
the game able to fix it.

---

## 6. How creatures reach the player

When an egg hatches and at least one enabled mod offers creatures, there is roughly a **one in
three** chance the hatchling is a mod creature, chosen at random from all enabled mods. Otherwise
it is one of the built-in types.

There is currently no way to guarantee a particular creature, or to tie one to an egg colour.

---

## 7. When something does not work

| symptom | cause |
|---|---|
| Mod missing from the list | No manifest, unparseable JSON, blank `id`, or an `id` another mod already used. For a single dll: the embedded resource is not called exactly `mod.json`, usually a missing `LogicalName`. Bad manifests are skipped silently so one bad mod cannot stop the game starting. |
| Mod appears but its code never runs | The mod is not ticked, or the game could not construct it. `crash.log` says which, tagged with your mod id. |
| `IBebooMod` not recognised | Your project copied `BebooGarden.ModApi.dll` into the mod folder. Set `Private` to false so it does not. |
| Creature appears, sounds like a normal beboo | Voice folders missing, empty, or under the wrong name. The folder under `creatures/` must equal the creature `id` exactly. |
| One sound never plays | FMOD could not decode it. Re-export as plain PCM WAV. |
| Creature never hatches | Mod not ticked, or luck — it is one in three, and only on a hatch. |

The game writes unhandled errors to `crash.log` in `%LocalAppData%\BebooGarden`. If a mod does
break something, that file is the thing to send.

---

## 8. Worked example

```
mods/birdfolk/mod.json
{
  "id": "birdfolk",
  "name": "Bird Folk",
  "description": "Adds a beboo that sings like a bird.",
  "creatures": [{ "id": "birdfolk.chirp", "name": "Chirp" }]
}

mods/birdfolk/creatures/birdfolk.chirp/cute/chirp1.wav
mods/birdfolk/creatures/birdfolk.chirp/cute/chirp2.wav
mods/birdfolk/creatures/birdfolk.chirp/song/warble.wav
```

Three files and a manifest. Everything else falls back to the base voice.

---

## 9. What mods can add today

Creatures with their voices, and code, through the small API in section 4.

The manifest is deliberately shaped to grow and unknown fields are ignored, so a mod can carry data
for a later version without breaking on this one.
