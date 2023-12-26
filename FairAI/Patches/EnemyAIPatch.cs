using FairAI.Component;

namespace FairAI.Patches
{
    internal class EnemyAIPatch
    {
        public static void patchKillEnemyOnOwnerClient(ref EnemyAI __instance, bool overrideDestroy = false)
        {
            if (__instance.gameObject.GetComponent<FAIR_AI>() == null)
            {
                __instance.gameObject.AddComponent<FAIR_AI>();
            }
        }
    }
}
