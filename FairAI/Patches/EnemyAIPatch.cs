using FairAI.Component;
using HarmonyLib;
using LethalThings;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        [HarmonyPatch("KillEnemyOnOwnerClient")]
        [HarmonyPrefix]
        public static void PatchKillEnemyOnOwnerClient(ref EnemyAI __instance, bool overrideDestroy = false)
        {
            if (__instance.gameObject.GetComponent<FAIR_AI>() == null)
            {
                __instance.gameObject.AddComponent<FAIR_AI>();
            }
        }
    }
}
