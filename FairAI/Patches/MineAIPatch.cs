using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class MineAIPatch
    {

        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        static void patchOnTriggerEnter(Collider other, ref Landmine __instance, ref float ___pressMineDebounceTimer)
        {
            EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component != null && !component.mainScript.isEnemyDead)
            {
                ___pressMineDebounceTimer = 0.5f;
                __instance.PressMineServerRpc();
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        static void patchOnTriggerExit(Collider other, ref Landmine __instance, ref bool ___sendingExplosionRPC)
        {
            EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (component != null && !component.mainScript.isEnemyDead)
            {
                if (!__instance.hasExploded)
                {
                    __instance.SetOffMineAnimation();
                    ___sendingExplosionRPC = true;
                    __instance.ExplodeMineServerRpc();
                }
            }
        }

        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPostfix]
        static void patchSpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
        {
            Collider[] array = Physics.OverlapSphere(explosionPosition, 6f, 2621448, QueryTriggerInteraction.Collide);
            EnemyAICollisionDetect enemy = null;
            for (int i = 0; i < array.Length; i++)
            {
                float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
                if (num2 > 4f && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (array[i].gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                {
                    enemy = array[i].gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (enemy != null && enemy.mainScript.IsOwner)
                    {
                        if (num2 < killRange)
                        {
                            Vector3 bodyVelocity = (enemy.transform.position - explosionPosition) * 80f / Vector3.Distance(enemy.transform.position, explosionPosition);
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
