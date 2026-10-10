# FakeFriend Scoreboard

让 [FakeFriend](https://thunderstore.io/c/riskofrain2/p/NotStudio/FakeFriend/) 的 AI bot 出现在原版 **Tab 计分板（玩家列表）** 里。
计分板每条自带的物品图标 / 装备图标来自 `master.inventory`，所以 bot 一进列表，**它的物品就能直接看了**。

## 为什么 bot 原本不在列表里

- 原版计分板由 `RoR2.UI.ScoreboardController.Rebuild()` 生成，数据源只有 `PlayerCharacterMasterController.instances`。
- FakeFriend 用 AI master prefab（`CommandoMonsterMaster` 或对应 survivor 的 AI master）生成 bot：
  `teamIndexOverride = TeamIndex.Player`、`minionOwnership.SetOwner(host)`，
  **没有 `PlayerCharacterMasterController`** —— 这正是它"不占 player slot / 不算难度缩放"的实现方式，
  代价就是永远进不了计分板（只在 HUD 的 ally card 里）。
- FakeFriend 自身没有任何计分板相关的 hook（反编译可见）。

## 做法

独立插件，不改 FakeFriend.dll，纯客户端 UI：

1. `Prefix` 接管 `ScoreboardController.Rebuild()`：
   真玩家照抄原版过滤条件（`activeInHierarchy` / `GetBody() != null` / `Util.GetBestMasterName != null`），
   然后 `SetStripCount(n)` + `stripAllocator.elements[i].SetMaster(master)` 追加 bot，最后跳过原版。
2. `Postfix` `Util.GetBestMasterName(CharacterMaster)`：bot 没有 NetworkUser，名字大概率是 null，
   只对名字形如 `FakeFriend N` 的 master 兜底返回它的名字。
3. bot 列表优先读 FakeFriend 自己的数据（权威）：`FakeFriendPlugin.bot` → `BotManager.Bots[]` → `FakeBot.Master`
   （闭源 + 全是 internal，只能反射）；任何一个环节变了就退回按 `master.name` 扫描。
4. 整段逻辑包在 `try/catch` 里：出任何异常就 `return true` 让原版 Rebuild 跑，计分板不会空掉。

不碰：难度缩放、玩家数、网络状态、结算逻辑。计分板是客户端 UI，谁装了插件谁能看到 bot 的条。

## 开发环境

`devenv`（官方 flake-parts 模板）+ `ilspycmd`：

```bash
direnv allow          # 或 nix develop --no-pure-eval --accept-flake-config
./scripts/build.sh            # 只编译
./scripts/build.sh --install  # 编译并装进 r2modman 的 Default profile
```

游戏程序集不用自己反编译：`RiskOfRain2.GameLibs`（BepInEx nuget feed）是 **stripped + publicized** 版本，
配合 `src/AssemblyInfo.cs` 里的 `[assembly: SecurityPermission(...SkipVerification = true)]`，
可以直接读写游戏里的 private / internal 成员，成员名由编译器校验。
见 [R2Wiki: Assembly References](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Assembly-References/)。

`scripts/decompile.sh` 是备用的：需要读游戏源码时用它把相关类型 dump 到 `decompile/`（已 gitignore）。

## 测试

1. 用 r2modman 启动 modded profile，开一局带 1–4 个 FakeFriend。
2. 按住 **Tab**：真玩家条之后应该多出 `FakeFriend 1..4` 的条，带等级、金钱、物品图标、装备。
3. 排查：`BepInEx/LogOutput.log` 里应有
   `FakeFriend Scoreboard 0.1.0 loaded.` 和
   `FakeFriend 1.2.0: bot list found (BotManager.Bots).`（或 `internals changed shape, using name scan.`）
   出现 `scoreboard rebuild failed, falling back to vanilla: ...` 说明接管失败，贴日志给我。

已知取舍：bot 死亡（body 被销毁）时它的条会消失，复活后回来；item mode 设成 None 的 bot 自然没有物品图标。
