using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using RoR2;

namespace FakeFriendScoreboard
{
    /// <summary>
    /// 找出 FakeFriend 的 bot。
    ///
    /// 背景：FakeFriend 用 AI master prefab（CommandoMonsterMaster 或对应 survivor 的
    /// AI master）生成 bot，所以 bot 是「有身体、玩家队伍、但没有
    /// PlayerCharacterMasterController」的 CharacterMaster；原版计分板只遍历
    /// PlayerCharacterMasterController.instances，因此 bot 永远不在玩家列表里。
    ///
    /// 主路径读 FakeFriend 自己的数据（权威）：
    ///     FakeFriendPlugin.bot            (private 字段)
    ///       -> BotManager.Bots[]          (public 字段，class 是 internal)
    ///         -> FakeBot.Master           (public 属性)
    /// FakeFriend 是闭源的，这三个类型都是 internal，所以只能反射；FakeFriend 一旦
    /// 改动内部结构就退回按名字扫描（它在 spawn 时设了 master.name = "FakeFriend N"）。
    /// </summary>
    internal static class BotLocator
    {
        internal const string BotMasterNamePrefix = "FakeFriend ";

        private static bool reflectionUsable;
        private static System.Reflection.FieldInfo pluginBotField;
        private static System.Reflection.FieldInfo botManagerBotsField;
        private static System.Reflection.PropertyInfo fakeBotMasterProperty;

        internal static void Init()
        {
            if (!Chainloader.PluginInfos.TryGetValue(Plugin.FakeFriendGuid, out var info) || info?.Instance == null)
            {
                Plugin.Log.LogWarning("FakeFriend not loaded; nothing to add to the scoreboard.");
                return;
            }

            try
            {
                var pluginType = info.Instance.GetType();
                pluginBotField = pluginType.GetField("bot",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                var botManagerType = pluginBotField?.FieldType;
                botManagerBotsField = botManagerType?.GetField("Bots",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                var fakeBotType = botManagerBotsField?.FieldType.GetElementType();
                fakeBotMasterProperty = fakeBotType?.GetProperty("Master",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                reflectionUsable = pluginBotField != null && botManagerBotsField != null && fakeBotMasterProperty != null;
                Plugin.Log.LogInfo(reflectionUsable
                    ? $"FakeFriend {info.Metadata.Version}: bot list found (BotManager.Bots)."
                    : $"FakeFriend {info.Metadata.Version}: internals changed shape, using name scan.");
            }
            catch (Exception e)
            {
                reflectionUsable = false;
                Plugin.Log.LogWarning($"binding FakeFriend internals failed ({e.Message}), using name scan.");
            }
        }

        internal static List<CharacterMaster> GetBots()
        {
            var bots = new List<CharacterMaster>();

            if (reflectionUsable && TryReadPluginBotList(bots))
            {
                bots.Sort(CompareByName);
                return bots;
            }

            foreach (var master in InstanceTracker.GetInstancesList<CharacterMaster>())
            {
                if (!master) continue;
                if (!master.name.StartsWith(BotMasterNamePrefix, StringComparison.Ordinal)) continue;
                if (IsBotMaster(master)) bots.Add(master);
            }

            bots.Sort(CompareByName);
            return bots;
        }

        private static bool TryReadPluginBotList(List<CharacterMaster> bots)
        {
            try
            {
                var plugin = Chainloader.PluginInfos[Plugin.FakeFriendGuid].Instance;
                var manager = pluginBotField.GetValue(plugin);
                if (manager == null) return true;   // 正常：还没进 run

                if (!(botManagerBotsField.GetValue(manager) is Array array)) return true;

                foreach (var entry in array)
                {
                    if (entry == null) continue;
                    if (fakeBotMasterProperty.GetValue(entry) is CharacterMaster master && IsBotMaster(master))
                        bots.Add(master);
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"reading FakeFriend bot list failed ({e.Message}), falling back to name scan.");
                reflectionUsable = false;
                return false;
            }
        }

        /// bot = 玩家队伍、有身体、且不是真玩家（没有 PlayerCharacterMasterController）。
        private static bool IsBotMaster(CharacterMaster master)
        {
            if (!master) return false;
            if (master.teamIndex != TeamIndex.Player) return false;
            if (master.playerCharacterMasterController) return false;
            return master.GetBody() != null;   // 死亡时 body 被销毁，等复活再上条
        }

        private static int CompareByName(CharacterMaster a, CharacterMaster b) =>
            string.Compare(a.name, b.name, StringComparison.Ordinal);
    }
}
