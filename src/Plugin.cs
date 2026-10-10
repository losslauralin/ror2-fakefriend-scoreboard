using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace FakeFriendScoreboard
{
    /// <summary>
    /// 把 FakeFriend 的 AI bot 加进原版 Tab 计分板（玩家列表）。
    /// 计分板条自带物品图标 / 装备图标，所以 bot 一进去，它的物品就能看了。
    /// 纯客户端 UI 改动：不碰难度缩放、不碰玩家数、不改网络状态。
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(FakeFriendGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "loss.FakeFriendScoreboard";
        public const string PluginName = "FakeFriend Scoreboard";
        public const string PluginVersion = "0.1.0";

        /// FakeFriend 的 BepInPlugin GUID（作者把它写成了常量 FakeFriendPlugin.PluginGUID）。
        public const string FakeFriendGuid = "FakeFriend";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static ConfigEntry<bool> Enabled { get; private set; }

        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Show FakeFriend bots in the Tab scoreboard (with their items).");

            BotLocator.Init();

            harmony = new Harmony(PluginGuid);
            try
            {
                harmony.PatchAll(typeof(ScoreboardPatch));
                Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
            }
            catch (Exception e)
            {
                Log.LogError($"patching failed, mod inactive: {e}");
            }
        }

        private void OnDestroy()
        {
            try { harmony?.UnpatchSelf(); } catch { /* shutting down */ }
        }
    }
}
