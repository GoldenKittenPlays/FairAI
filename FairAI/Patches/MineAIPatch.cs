using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace FairAI.Patches
{
    internal class MineAIPatch
    {
        public static void PatchOnTriggerEnter(ref Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
        {
            if (Plugin.AllowFairness() && !(__instance.transform.position.y > -80f))
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
        }

        public static void PatchOnTriggerExit(ref Landmine __instance, Collider other, ref bool ___sendingExplosionRPC)
        {
            if (Plugin.AllowFairness() && !(__instance.transform.position.y > -80f))
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
        }


        public static void DetonatePatch(ref Landmine __instance)
        {
            if (!(__instance == null))
            {
                __instance.StartCoroutine(WaitForUpdate(1.5f, __instance));
            }
        }

        public static IEnumerator WaitForUpdate(float waitTime, Landmine mine)
        {
            yield return new WaitForSeconds(waitTime);
            if (!(mine == null))
            {
                if (mine.GetComponent<NetworkObject>() != null)
                {
                    mine.GetComponent<NetworkObject>().Despawn(true);
                }
                else
                {
                    Object.Destroy(mine.gameObject);
                }
            }
        }

        public static void PatchSpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
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
                            enemy.mainScript.HitEnemyOnLocalClient(enemy.mainScript.enemyHP);
                        }
                        else if (num2 < damageRange)
                        {
                            enemy.mainScript.HitEnemyOnLocalClient(Mathf.RoundToInt(enemy.mainScript.enemyHP / 2));
                        }
                    }
                }
            }
        }
    }
}
