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
        static void patchOnTriggerEnter(Collider other, Landmine __instance, ref float ___pressMineDebounceTimer)
        {
            EnemyAI component = other.gameObject.GetComponent<EnemyAI>();
            if (component != null && !component.isEnemyDead)
            {
                ___pressMineDebounceTimer = 0.5f;
                __instance.PressMineServerRpc();
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        static void patchOnTriggerExit(Collider other, ref Landmine __instance, ref bool ___sendingExplosionRPC)
        {
            EnemyAI component = other.gameObject.GetComponent<EnemyAI>();
            if (component != null && !component.isEnemyDead)
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
        static void patchSpawnExplosion(ref Landmine __instance, Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
        {
            Collider[] array = Physics.OverlapSphere(explosionPosition, 6f, 2621448, QueryTriggerInteraction.Collide);
            EnemyAI enemy = null;
            for (int i = 0; i < array.Length; i++)
            {
                float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
                if (num2 > 4f && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (array[i].gameObject.GetComponent<EnemyAI>() != null)
                {
                    enemy = array[i].gameObject.GetComponent<EnemyAI>();
                    if (enemy != null && enemy.IsOwner)
                    {
                        if (num2 < killRange)
                        {
                            Vector3 bodyVelocity = (enemy.transform.position - explosionPosition) * 80f / Vector3.Distance(enemy.transform.position, explosionPosition);
                            enemy.KillEnemyOnOwnerClient(true);
                        }
                        else if (num2 < damageRange)
                        {
                            enemy.HitEnemyOnLocalClient(2);
                        }
                    }
                }
            }
        }
    }
}
