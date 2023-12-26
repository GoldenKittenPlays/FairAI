using FairAI.Component;
using GameNetcodeStuff;
using HarmonyLib;
using LethalThings;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(RoombaAI))]
    internal class BoombaPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void patchStart(ref RoombaAI __instance)
        {
            __instance.gameObject.AddComponent<BoombaTimer>();
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        public static void patchDoAIInterval(ref RoombaAI __instance)
        {
            if (FairAIUtilities.IsAgentOnNavMesh(__instance.gameObject)
                    && (__instance.currentSearch != null || __instance.movingTowardsTargetPlayer) && (__instance.mineAudio.isPlaying || __instance.mineFarAudio.isPlaying)
                    && __instance.GetComponent<BoombaTimer>().IsActiveBomb())
            {
                Plugin.logger.LogInfo("On the run!");
                Vector3 oldPos = __instance.transform.position + Vector3.up;
                Collider[] array = Physics.OverlapSphere(oldPos, 6f, 2621448, QueryTriggerInteraction.Collide);
                for (int i = 0; i < array.Length; i++)
                {
                    float num2 = Vector3.Distance(oldPos, array[i].transform.position);
                    if (num2 > 4f && Physics.Linecast(oldPos, array[i].transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    Plugin.logger.LogInfo("Collider List!");
                    if (array[i].gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                    {
                        EnemyAICollisionDetect enemy = array[i].gameObject.GetComponent<EnemyAICollisionDetect>();
                        if (enemy.mainScript.gameObject != __instance.gameObject) 
                        {
                            if (enemy != null && enemy.mainScript.IsOwner && !enemy.mainScript.isEnemyDead)
                            {
                                Plugin.logger.LogInfo("Got enemy!");
                                Object.Instantiate(StartOfRound.Instance.explosionPrefab, oldPos, Quaternion.Euler(-90f, 0f, 0f), RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                                if (num2 < 3)
                                {
                                    enemy.mainScript.KillEnemyOnOwnerClient(true);
                                }
                                else if (num2 < 6)
                                {
                                    enemy.mainScript.HitEnemyOnLocalClient(2);
                                }
                            }
                            Plugin.logger.LogInfo("Killed Boomba!");
                            if (__instance.IsServer)
                            {
                                Debug.Log("Kill enemy called on server, destroy true");
                                __instance.KillEnemy(destroy: true);
                            }
                            else
                            {
                                __instance.KillEnemyServerRpc(destroy: true);
                            }
                        }
                    }
                }
            }
        }
    }
}
