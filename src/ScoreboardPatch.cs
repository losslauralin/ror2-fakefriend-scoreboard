using System;
using HarmonyLib;
using RoR2;
using RoR2.UI;

namespace FakeFriendScoreboard
{
    /// <summary>
    /// 接管原版计分板构建。
    ///
    /// 原版逻辑（SotS 之后）：
    ///     PlayerCharacterMasterController.instances
    ///       .Where(x => x.gameObject.activeInHierarchy
    ///                &amp;&amp; x.master.GetBody() != null
    ///                &amp;&amp; Util.GetBestMasterName(x.master) != null)
    /// 然后 SetStripCount(n) + stripAllocator.elements[i].SetMaster(master)。
    /// 我们照抄真玩家那部分，再把 FakeFriend 的 bot 追加在后面。
    /// </summary>
    [HarmonyPatch]
    internal static class ScoreboardPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScoreboardController), nameof(ScoreboardController.Rebuild))]
        private static bool Rebuild(ScoreboardController __instance)
        {
            if (!Plugin.Enabled.Value) return true;

            try
            {
                var masters = new System.Collections.Generic.List<CharacterMaster>();
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (!pcmc || !pcmc.gameObject.activeInHierarchy) continue;
                    var master = pcmc.master;
                    if (!master || master.GetBody() == null) continue;
                    if (Util.GetBestMasterName(master) == null) continue;
                    masters.Add(master);
                }

                var bots = BotLocator.GetBots();
                int total = masters.Count + bots.Count;

                __instance.SetStripCount(total);
                var strips = __instance.stripAllocator.elements;

                int index = 0;
                for (int i = 0; i < masters.Count; i++, index++)
                    strips[index].SetMaster(masters[i]);

                for (int i = 0; i < bots.Count; i++, index++)
                    strips[index].SetMaster(bots[i]);

                // Rebuild 每帧都跑：只在条的组成变化时打一行，避免刷日志。
                var signature = Describe(masters.Count, bots);
                if (signature != lastSignature)
                {
                    lastSignature = signature;
                    Plugin.Log.LogInfo(signature);
                }

                return false;   // 跳过原版 Rebuild
            }
            catch (Exception e)
            {
                LogOnce(e);
                return true;    // 出错就让原版重建，计分板不会空着
            }
        }

        private static string lastSignature;
        private static bool loggedOnce;

        private static string Describe(int players, System.Collections.Generic.List<CharacterMaster> bots)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("scoreboard: ").Append(players).Append(" player strip(s)");
            sb.Append(" + ").Append(bots.Count).Append(" bot strip(s)");
            if (bots.Count > 0)
            {
                sb.Append(" [");
                for (int i = 0; i < bots.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(bots[i].name);
                }
                sb.Append(']');
            }
            return sb.ToString();
        }

        private static void LogOnce(Exception e)
        {
            if (loggedOnce) return;
            loggedOnce = true;
            Plugin.Log.LogError($"scoreboard rebuild failed, falling back to vanilla: {e}");
        }
    }

    /// <summary>
    /// bot 没有 NetworkUser，Util.GetBestMasterName 对它会返回 null —— 那计分板条上就没名字。
    /// 只对名字形如 "FakeFriend N" 的 master 兜底，不影响真玩家。
    ///
    /// 注意：Harmony 靠**参数名**配对，所以这里的形参必须和目标方法的形参同名
    /// （游戏里是 characterMaster）；名字写错会抛 "Parameter ... not found"。
    /// </summary>
    [HarmonyPatch]
    internal static class MasterNamePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Util), nameof(Util.GetBestMasterName), new[] { typeof(CharacterMaster) })]
        private static void GetBestMasterName(ref string __result, CharacterMaster characterMaster)
        {
            if (!string.IsNullOrEmpty(__result)) return;
            if (!characterMaster) return;
            if (characterMaster.name.StartsWith(BotLocator.BotMasterNamePrefix, StringComparison.Ordinal))
                __result = characterMaster.name;
        }
    }
}
