# Modding Beboo Garden: Enhanced Edition — reference

Version 2.1. Mods are folders of data, not code: no compiling, no DLLs, nothing to install beyond
dropping a folder in place.

---

## 1. Where mods go

Next to `BebooGarden.exe`:

```
BebooGarden.exe
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

## 4. The mod list

Shown once at startup, before the garden, whenever `mods/` contains at least one valid mod. One
checkbox per mod; enter or space toggles; choose Play to continue. Choices are saved.

**Enabling controls whether new creatures are offered, not whether files are read.** Every
discovered mod's voices load either way, because a creature already living in the save has to keep
its voice whatever the checkbox says.

**A mod cannot be switched off while the player owns one of its creatures.** The box stays ticked
and names the beboo keeping it there. Turning it off would leave that beboo mute with nothing in
the game able to fix it.

---

## 5. How creatures reach the player

When an egg hatches and at least one enabled mod offers creatures, there is roughly a **one in
three** chance the hatchling is a mod creature, chosen at random from all enabled mods. Otherwise
it is one of the built-in types.

There is currently no way to guarantee a particular creature, or to tie one to an egg colour.

---

## 6. When something does not work

| symptom | cause |
|---|---|
| Mod missing from the list | No `mod.json`, unparseable JSON, blank `id`, or an `id` another mod already used. Bad manifests are skipped silently so one bad mod cannot stop the game starting. |
| Creature appears, sounds like a normal beboo | Voice folders missing, empty, or under the wrong name. The folder under `creatures/` must equal the creature `id` exactly. |
| One sound never plays | FMOD could not decode it. Re-export as plain PCM WAV. |
| Creature never hatches | Mod not ticked, or luck — it is one in three, and only on a hatch. |

The game writes unhandled errors to `crash.log` beside the executable. If a mod does break
something, that file is the thing to send.

---

## 7. Worked example

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

## 8. What mods can add today

Creatures, and their voices. That is the whole of it in 2.1.

The manifest is deliberately shaped to grow, and unknown fields are ignored, so a mod can carry
data for a later version without breaking on this one.
