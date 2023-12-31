using HarmonyLib;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class MineAIPatch
    {

        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        static void PatchOnTriggerEnter(ref Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
        {
            EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component != null && !component.mainScript.isEnemyDead)
            {
                if (Plugin.CanMob("ExplodeAllMobs", ".Mine", component.mainScript.enemyType.enemyName.ToUpper()))
                {
                    ___pressMineDebounceTimer = 0.5f;
                    __instance.PressMineServerRpc();
                }
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPrefix]
        static void PatchOnTriggerExit(ref Landmine __instance, Collider other, ref bool ___sendingExplosionRPC)
        {
            EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component != null && !component.mainScript.isEnemyDead)
            {
                if (Plugin.CanMob("ExplodeAllMobs", ".Mine", component.mainScript.enemyType.enemyName.ToUpper()))
                {
                    if (!__instance.hasExploded)
                    {
                        __instance.SetOffMineAnimation();
                        ___sendingExplosionRPC = true;
                        __instance.ExplodeMineServerRpc();
                    }
                }
            }
        }

        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPrefix]
        static void PatchSpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
        {
            Collider[] array = Physics.OverlapSphere(explosionPosition, 6f, 2621448, QueryTriggerInteraction.Collide);
            for (int i = 0; i < array.Length; i++)
            {
                float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
                if (num2 > 4f && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (array[i].gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                {
                    EnemyAICollisionDetect enemy = array[i].gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (enemy != null && enemy.mainScript.IsOwner && !enemy.mainScript.isEnemyDead)
                    {
                        if (num2 < killRange)
                        {
                            enemy.mainScript.KillEnemyOnOwnerClient(true);
                        }
                        else if (num2 < damageRange)
                        {
                            enemy.mainScript.HitEnemyOnLocalClient(2);
                        }
                    }
                }
            }
        }
    }
}
