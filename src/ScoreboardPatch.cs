using System;
using HarmonyLib;
using RoR2;
using RoR2.UI;

namespace FakeFriendScoreboard
{
    [HarmonyPatch]
    internal static class ScoreboardPatch
    {
        /// <summary>
        /// 原版逻辑（SotS 之后）是：
        ///     PlayerCharacterMasterController.instances
        ///       .Where(x => x.gameObject.activeInHierarchy
        ///                &amp;&amp; x.master.GetBody() != null
        ///                &amp;&amp; Util.GetBestMasterName(x.master) != null)
        /// 然后 SetStripCount(n) + stripAllocator.elements[i].SetMaster(master)。
        /// 我们照抄真玩家那部分，再把 bot 追加在后面。
        /// </summary>
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
                {
                    strips[index].SetMaster(bots[i]);
                    Plugin.Log.LogDebug($"scoreboard: strip {index} <- {bots[i].name}");
                }

                return false;   // 跳过原版 Rebuild
            }
            catch (Exception e)
            {
                LogOnce(e);
                return true;    // 出错就让原版重建，计分板不会空着
            }
        }

        /// <summary>
        /// bot 没有 NetworkUser，Util.GetBestMasterName 大概率返回 null —— 那计分板条上
        /// 就没有名字。这里只对 "FakeFriend N" 这种 master 兜底，不影响真玩家。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Util), nameof(Util.GetBestMasterName), new[] { typeof(CharacterMaster) })]
        private static void GetBestMasterName(ref string __result, CharacterMaster master)
        {
            if (!string.IsNullOrEmpty(__result)) return;
            if (!master) return;
            if (master.name.StartsWith(BotLocator.BotMasterNamePrefix, StringComparison.Ordinal))
                __result = master.name;
        }

        private static bool loggedOnce;

        private static void LogOnce(Exception e)
        {
            if (loggedOnce) return;
            loggedOnce = true;
            Plugin.Log.LogError($"scoreboard rebuild failed, falling back to vanilla: {e}");
        }
    }
}
