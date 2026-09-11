# Modding Beboo Garden: Enhanced Edition — reference

Version 2.1. Mods are folders of data, not code: no compiling, no DLLs, nothing to install beyond
dropping a folder in place.

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

Either folder has the same shape:

```
mods/
  MODDING.md            this file
  my-mod/               one folder per mod
    mod.json            required; a folder without one is ignored
    creatures/
      my-mod.fuzzy/     one folder per creature, named by its id
        cute/           .wav files
        cry/
        ...
```

Folder names other than `creatures/` are ignored, so you can keep a readme, a licence or your
source files beside the manifest without upsetting anything.

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
| `features` | no | Behaviour to switch on, by name. See section 4. |

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

## 4. Features

A mod cannot bring code, so it cannot invent behaviour. What it can do is ask the game to turn on
something the game already knows how to do but does not do by default:

```json
{ "id": "confirm-pickup", "name": "Ask before picking things up", "features": ["confirmPickup"] }
```

| feature | what it does |
|---|---|
| `confirmPickup` | Pressing enter next to something on the ground asks yes or no first, instead of taking it straight away. |

Names are matched without regard to case, and one this version does not recognise is ignored rather
than refused, so a mod can name a feature from a later version and still load here.

This is also why the game ships a mod of its own: `confirm-pickup` is a preference some people want
and most do not, and the mod list is already a list of things you can tick. It saves growing a
settings screen for one checkbox.

A mod may list features, creatures, or both.

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
| Mod missing from the list | No `mod.json`, unparseable JSON, blank `id`, or an `id` another mod already used. Bad manifests are skipped silently so one bad mod cannot stop the game starting. |
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

Creatures with their voices, and the features listed in section 4.

The manifest is deliberately shaped to grow, and unknown fields and unknown feature names are both
ignored, so a mod can carry data for a later version without breaking on this one.
