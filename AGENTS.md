# FakeFriend Scoreboard — Agent Guide

A single-purpose BepInEx 5 plugin for Risk of Rain 2: give
[FakeFriend](https://thunderstore.io/c/riskofrain2/p/NotStudio/FakeFriend/)'s AI bots a strip in the vanilla Tab
scoreboard (which is also what makes their items visible, since the strip subscribes to `master.inventory`).

FakeFriend is **closed source**; this plugin never modifies it.

## Layout

| Path | What |
|------|------|
| `src/Plugin.cs` | BepInPlugin entry point, config, per-patch try/catch |
| `src/ScoreboardPatch.cs` | `ScoreboardController.Rebuild` prefix + `Util.GetBestMasterName` postfix |
| `src/BotLocator.cs` | finds bot `CharacterMaster`s (reflection into FakeFriend, name scan fallback) |
| `src/AssemblyInfo.cs` | the publicizer `SecurityPermission` line (see below) |
| `scripts/build.sh` | `dotnet build`; `--install` overwrites the r2modman-managed dll |
| `scripts/package.sh` | Thunderstore-format zip in `dist/` for r2modman **Import local mod** |
| `scripts/make-icon.py` | regenerates `Thunderstore/icon.png` (pure python, no gfx libs) |
| `scripts/decompile.sh` | fallback: dumps game types to `decompile/` (gitignored) |
| `Thunderstore/` | `manifest.json`, `icon.png`, mod-page `README.md`, `CHANGELOG.md` |
| `decompile/`, `dist/`, `bin/`, `obj/` | generated, gitignored — never commit |

Machine facts: game `~/.local/share/Steam/steamapps/common/Risk of Rain 2` (Unity 2021.3.45f2, code split across
`Assembly-CSharp.dll` **and** `RoR2.dll` — a type missing from one is often in the other), r2modman profile
`~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default`, log `…/Default/BepInEx/LogOutput.log`.

## Hard stops

- **Never hand-copy a DLL into the profile.** Installation goes through r2modman → *Settings → Import local mod*
  (the zip from `scripts/package.sh`). r2modman files local mods under `BepInEx/plugins/Unknown-<manifest.name>`
  (`Unknown-FakeFriendScoreboard` here). `scripts/build.sh --install` may only overwrite that already-managed
  directory, and refuses to run when it does not exist. A second copy of the same plugin GUID breaks BepInEx.
- **Do not decompile the game to look up API members.** `RiskOfRain2.GameLibs` (BepInEx NuGet feed) is the game
  assemblies **stripped + publicized**, so private/internal members compile directly and the compiler validates every
  name. `src/AssemblyInfo.cs` carries the required `[assembly: SecurityPermission(SecurityAction.RequestMinimum,
  SkipVerification = true)]`. See R2Wiki *Assembly References*. `decompile/` is a last resort, not the default.
- **Never read `/nix/store/**`** to answer questions about packages; use `nh search`.
- **The agent does not run the game.** In-game verification is the user's; provide the exact log lines to grep.

## Harmony rules learned the hard way

- Parameters are matched **by name**. A postfix on `Util.GetBestMasterName(CharacterMaster characterMaster)` must
  declare `CharacterMaster characterMaster`; naming it `master` throws `Parameter "master" not found` and kills the
  whole `PatchAll` call.
- Patch each patch class in its own `try/catch` (`Plugin.TryPatch`). One failure must not disable the others, and no
  failure log may claim the plugin is inactive when some patches did apply.
- Any patch that can fail must keep a vanilla fallback: on exception, log once and `return true` so the game's own
  logic runs (an empty scoreboard is worse than no bot strips).
- Never log per frame. `ScoreboardController.Rebuild` runs every frame while the scoreboard is open; log only when
  the composition changes (see `Describe`).

## Why bots are invisible (do not re-research this)

- The vanilla scoreboard iterates `PlayerCharacterMasterController.instances` only. FakeFriend spawns bots from AI
  master prefabs (`CommandoMonsterMaster` / the survivor's AI master) with `teamIndexOverride = TeamIndex.Player` and
  `minionOwnership.SetOwner(host)` → a bot is a `CharacterMaster` with a body and **no**
  `PlayerCharacterMasterController`. That is exactly why it takes no player slot and scales no difficulty; it is also
  why it never reaches the scoreboard. FakeFriend has zero scoreboard code.
- Bot identity: `master.name == "FakeFriend " + (slotIndex + 1)`; the authoritative list is
  `FakeFriendPlugin.bot` (private) → `BotManager.Bots` (public field on an internal class) →
  `FakeBot.Master` (public property). All internal, hence reflection; fall back to a name scan when it breaks.
- Reference implementations to copy from: `itsschwer/ror2-experimental` *sots-scoreboard* (Rebuild override,
  `SetStripCount` + `stripAllocator.elements[i].SetMaster`), `Wet-Boys/LookingGlass` (strip members:
  `itemInventoryDisplay`, `equipmentIcon`, `itemCountText`, `inventory`), `Melledy/RoR2-PlayerBots` (bot masters,
  the "PlayerMode" alternative that makes bots real players).

## Environment and running things

- The toolchain (`dotnet`, `ilspycmd`, `python3`) lives in the devenv shell: `direnv allow` once, or
  `nix develop --no-pure-eval --accept-flake-config`. Raw `nix develop` without `--accept-flake-config` prompts
  (no tty in an agent session) because `flake.nix` declares the devenv cachix substituter.
- Long builds: redirect to a log file and read it. Piping through `tail` hides output until completion and makes a
  slow build look hung; the harness can kill a long command and take its output with it.
- Read the machine instead of asking the user to run read-only commands and paste results. `LogOutput.log` and
  `mods.yml` answer most questions directly.

## Verify

```
./scripts/build.sh          # 0 errors; warnings are not acceptable either
grep -E "patched scoreboard|patched bot display name|scoreboard:" "$PROFILE/BepInEx/LogOutput.log"
```

Expected on load: `patched scoreboard (ScoreboardPatch).` and `patched bot display name (MasterNamePatch).`, plus
`FakeFriend <ver>: bot list found (BotManager.Bots).`. In a run with bots, one line per composition change:
`scoreboard: 1 player strip(s) + 3 bot strip(s) [FakeFriend 1, ...]`.

## Scope

Client-side UI only. Do not make bots real players, do not touch difficulty scaling, run logic, networking, or
FakeFriend's own code. Versioning lives in two places kept in sync: `Plugin.PluginVersion` and
`Thunderstore/manifest.json` (`version_number`); bump both when behaviour changes.
