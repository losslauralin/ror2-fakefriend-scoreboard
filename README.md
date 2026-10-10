# FakeFriend Scoreboard

Risk of Rain 2 BepInEx plugin. Gives [FakeFriend](https://thunderstore.io/c/riskofrain2/p/NotStudio/FakeFriend/)
bots their own strip in the Tab scoreboard, so they appear in the player list with their items and equipment —
the vanilla strip already subscribes to `master.inventory`.

Requires FakeFriend. Client-side UI: no change to difficulty scaling, player count or run logic.

## Install

```bash
./scripts/package.sh                       # -> dist/FakeFriendScoreboard-<version>.zip
```

r2modman → **Settings → Import local mod** → pick that zip → install into the profile you play.

A bot left dead leaves the list until it respawns; a bot set to items mode `None` has nothing to show.

## Build and update

```bash
direnv allow                              # devenv shell: dotnet, ilspycmd, python3
./scripts/build.sh --install              # rebuild and overwrite the DLL r2modman installed
./scripts/package.sh                      # rebuild the importable zip
```

`--install` overwrites the copy r2modman manages, and refuses to create a second one: two DLLs with the same plugin
GUID break BepInEx.

Game API members come from `RiskOfRain2.GameLibs` (stripped and publicized), so the compiler checks every name and
nothing needs decompiling. Why the bots were invisible, how the bot list is read, and the log lines worth grepping
live in `AGENTS.md`.
