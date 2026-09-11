# Mods

Full reference: **MODDING.md**, beside this file.

Put your own mods in `%LocalAppData%\BebooGarden\mods` rather than in here: this folder lives in
the install directory, which normally needs administrator rights to write to. Both are read, and
the game shows everything it finds. Paste that path into Explorer's address bar to get there, and
make the `mods` folder if it does not exist yet.

Drop a mod folder in there and it appears in the list the game shows at startup, with a checkbox.
Tick the ones you want and choose play.

A mod whose creature you already own **cannot be switched off**. Turning it off would leave that
beboo with no voice and nothing in the game to get it back with, so those boxes stay ticked and
tell you which of your beboos is holding them there.

## Layout

```
mods/
  my-mod/
    mod.json
    creatures/
      my-mod.fuzzy/
        cute/     one or more .wav files
        cry/
        sleep/
        ...
```

## mod.json

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

`id` has to be unique across mods, and a creature `id` has to be unique across every mod, so
prefix it with your mod's id. The creature `id` is also the name of its folder under `creatures/`,
and it is what gets written into the save, so **do not rename it once people are playing with it**.

Fields the game does not recognise are ignored rather than rejected, so a mod written for a later
version still loads whatever this version understands.

## Voices

Each creature folder holds one folder per category, each with any number of `.wav` files. The game
picks one at random each time.

| folder     | when it plays                         |
|------------|---------------------------------------|
| `cute`     | idle noises, the voice you hear most  |
| `pet`      | being stroked                         |
| `fun`      | playing with an item                  |
| `inter`    | playing with another beboo            |
| `song`     | singing along with a friend           |
| `yumy`     | eating                                |
| `surprise` | startled                              |
| `angry`    | woken up rudely                       |
| `cry`      | sad                                   |
| `yawn`     | going to sleep, waking up             |
| `sleep`    | asleep                                |

**Every category is optional.** A missing or empty one falls back to the base beboo voice, so you
can ship three good sounds rather than eleven mediocre ones. A file FMOD cannot read is skipped and
the rest of the voice still loads.

Mono 16 bit WAV at 22.05 kHz matches what the game ships. The base voice sits around 200 to 1200 Hz
with sounds between half a second and four seconds long; going far outside that will sound like a
different animal rather than a different beboo.

## What mods can add today

Creatures. The manifest is shaped to grow, but creatures are what this version reads.
