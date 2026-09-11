# Mods distributed separately

Mods in here are **not** part of the game's download. Each one is released on its own and installed
by whoever wants it, which is the point: they change how the game plays, and most people should
never have to think about them.

Each is a single dll with its manifest built in, so installing one means dropping the file into
`%LocalAppData%\BebooGarden\mods` — no unpacking, no administrator rights, and it survives
reinstalling the game. It then appears in the list the game shows at startup, with a checkbox, and
does nothing until it is ticked.

The full reference for writing one is `MODDING.md`, which installs with the game if you tick the
modding component, alongside the documentation for `BebooGarden.ModApi.dll`.

## Layout

```
confirm-pickup/
  mod.json          the manifest, embedded into the dll at build time
  src/              the mod's source
  confirm-pickup.dll   built here, and this file alone is what ships
```

Build one with:

```
dotnet build mods-standalone\confirm-pickup\src\ConfirmPickup.csproj -c Release
```

which writes the dll into the mod's folder.

## confirm-pickup

Pressing enter next to something on the ground asks yes or no first instead of taking it. Eggs are
left alone: they hatch rather than being picked up, so the question would be the wrong one.
