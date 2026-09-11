# Mods distributed separately

Mods in here are **not** part of the game's download. Each one is packaged on its own and installed
by whoever wants it, which is the whole point: they change how the game plays, and most people
should never have to think about them.

Each folder is packaged as a zip containing the folder itself, so it unpacks straight into a mods
folder:

```
confirm-pickup.zip
  confirm-pickup/
    mod.json
```

To install one, unpack it into `%LocalAppData%\BebooGarden\mods` — no administrator rights needed,
and it survives reinstalling the game. It then appears in the list the game shows at startup, with
a checkbox.

The full reference for writing one is `MODDING.md`, which does ship with the game, in its `mods`
folder.

## confirm-pickup

Pressing enter next to something on the ground asks yes or no first instead of taking it. Eggs are
left alone: they hatch rather than being picked up, so the question would be the wrong one.
