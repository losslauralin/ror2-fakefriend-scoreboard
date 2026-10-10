# FakeFriend Scoreboard

Adds [FakeFriend](https://thunderstore.io/c/riskofrain2/p/NotStudio/FakeFriend/)'s AI partners to the vanilla
**Tab scoreboard**, so they show up in the player list like real teammates — with their **items and equipment**,
because the vanilla scoreboard strip already subscribes to `master.inventory`.

## Why they were missing

FakeFriend spawns its bots from AI master prefabs with `teamIndexOverride = TeamIndex.Player`, so a bot is a
`CharacterMaster` with a body and no `PlayerCharacterMasterController`. That is exactly what makes it not take a
player slot (and not scale difficulty) — and also why the vanilla scoreboard, which only iterates
`PlayerCharacterMasterController.instances`, never lists it. FakeFriend itself has no scoreboard code.

## What this mod does

- Prefix on `ScoreboardController.Rebuild()`: real players exactly as vanilla, then one extra strip per bot.
- Postfix on `Util.GetBestMasterName(CharacterMaster)`: bots have no `NetworkUser`, so they get their name back
  (`FakeFriend 1` … `FakeFriend 4`). Only applies to masters named `FakeFriend N`.
- Bot list is read from FakeFriend's own `BotManager.Bots` (reflection); if FakeFriend ever changes its internals,
  it falls back to scanning for masters named `FakeFriend N`.
- Everything is wrapped in `try/catch`: if anything goes wrong, the vanilla scoreboard is used instead.

Client-side UI only: no change to difficulty scaling, player count, networking or run logic. Being UI, it only shows
up for players who have this mod installed (the bots themselves are already synced to everyone by the game).

## Known limits

- A dead bot disappears from the scoreboard until it respawns (its body is gone).
- Bots set to item mode **None** have no items to show.
- The scoreboard strip shows item icons and the equipment icon. For full item descriptions on those icons you want
  [LookingGlass](https://thunderstore.io/c/riskofrain2/p/DropPod/LookingGlass/) — it hooks the same strip methods and
  works fine alongside this mod.

## Credits

- [FakeFriend](https://thunderstore.io/c/riskofrain2/p/NotStudio/FakeFriend/) by NotStudio (not required to be open
  source for this to work; the bot list is read at runtime).
- API knowledge from open source mods: [itsschwer/RestoreScoreboard](https://github.com/itsschwer/ror2-experimental)
  (scoreboard rebuild), [Wet-Boys/LookingGlass](https://github.com/Wet-Boys/LookingGlass) (strip members),
  [Melledy/RoR2-PlayerBots](https://github.com/Melledy/RoR2-PlayerBots) (bot master layout).
- Game assemblies via `RiskOfRain2.GameLibs` (stripped + publicized), per the
  [R2Wiki](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Assembly-References/).
