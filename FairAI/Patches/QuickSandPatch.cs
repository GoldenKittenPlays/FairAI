using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace FairAI.Patches
{
    internal class QuickSandPatch
    {
        public static bool OnTriggerStayPatch(ref QuicksandTrigger __instance, Collider other)
        {
            PlayerControllerB playerScript = other.gameObject.GetComponent<PlayerControllerB>();

            if (__instance.isWater)
            {
                if (!other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<EnemyAICollisionDetect>() == null)
                {
                    return false;
                }
                if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                {
                    return false;
                }
                else
                {
                    if (playerScript != GameNetworkManager.Instance.localPlayerController && playerScript != null && playerScript.underwaterCollider != __instance)
                    {
                        playerScript.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                        return false;
                    }
                }
            }

            if (!__instance.isWater && !other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<EnemyAICollisionDetect>() == null)
            {
                return false;
            }

            if (playerScript != null)
            {
                if (playerScript != GameNetworkManager.Instance.localPlayerController)
                {
                    return false;
                }

                if ((__instance.isWater && playerScript.isInsideFactory != __instance.isInsideWater) || playerScript.isInElevator)
                {
                    if (__instance.sinkingLocalPlayer)
                    {
                        __instance.StopSinkingLocalPlayer(playerScript);
                    }

                    return false;
                }

                if (__instance.isWater && !playerScript.isUnderwater)
                {
                    playerScript.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                    playerScript.isUnderwater = true;
                }

                playerScript.statusEffectAudioIndex = __instance.audioClipIndex;
                if (playerScript.isSinking)
                {
                    return false;
                }

                if (__instance.sinkingLocalPlayer)
                {
                    if (!playerScript.CheckConditionsForSinkingInQuicksand())
                    {
                        __instance.StopSinkingLocalPlayer(playerScript);
                    }
                }
                else if (playerScript.CheckConditionsForSinkingInQuicksand())
                {
                    Debug.Log("Set local player to sinking!");
                    float hinderance = __instance.movementHinderance;
                    __instance.sinkingLocalPlayer = true;
                    playerScript.sourcesCausingSinking++;
                    playerScript.isMovementHindered++;
                    playerScript.hinderedMultiplier *= hinderance;

                    if (Plugin.rubberBootsType != null)
                    {
                        MethodInfo reduceMethod = Plugin.rubberBootsType.GetMethod("ReduceMovementHinderance");
                        MethodInfo clearMethod = Plugin.rubberBootsType.GetMethod("ClearMovementHinderance");

                        // Load the type from the MoreShipUpgrades assembly
                        Type upgradeBusType = Plugin.lguAssembly.GetType("MoreShipUpgrades.Managers.UpgradeBus");
                        Type customTerminalNodeType = Plugin.lguAssembly.GetType("MoreShipUpgrades.UI.TerminalNodes.CustomTerminalNode");

                        // Get the static "Instance" property (this holds the singleton)
                        PropertyInfo instanceProp = upgradeBusType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                        // Retrieve the UpgradeBus instance
                        object upgradeBusInstance = instanceProp.GetValue(null);
                        int[] upgradeStats = Utils.GetLateGameUpgradeTier("Rubber Boots");

                        if (upgradeStats.Length > 0)
                        {
                            // Now you can access instance fields or methods from it:
                            FieldInfo upgradeLevelsField = upgradeBusType.GetField("upgradeLevels", BindingFlags.Instance | BindingFlags.NonPublic);
                            Dictionary<string, int> upgradeLevels = (Dictionary<string, int>)upgradeLevelsField.GetValue(upgradeBusInstance);
                            int rubberBootsLvl = upgradeLevels["Rubber Boots"];
                            Plugin.logger.LogInfo("Rubber Boots Tier: " + rubberBootsLvl);
                            // Call the static methods
                            float adjustedHinderance = (float)reduceMethod.Invoke(null, [hinderance]);

                            // Apply your modified hinderance
                            playerScript.hinderedMultiplier *= adjustedHinderance;

                            // Reset or clear effect
                            clearMethod.Invoke(null, [(int)adjustedHinderance]);
                            if (upgradeStats[0] == upgradeStats[1])
                            {
                                playerScript.isMovementHindered = 0;
                                playerScript.hinderedMultiplier = 1f;
                            }
                        }
                    }
                    if (__instance.isWater)
                    {
                        playerScript.sinkingSpeedMultiplier = 0f;
                    }
                    else
                    {
                        playerScript.sinkingSpeedMultiplier = __instance.sinkingSpeedMultiplier;
                    }
                }
            }
            if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null && Plugin.GetBool("Mobs", "QuicksandAllMobs"))
            {
                EnemyAICollisionDetect enemyAI = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (enemyAI != null)
                {
                    EnemyAI ai = enemyAI.mainScript;
                    if (ai != null)
                    {
                        if (ai.agent != null)
                        {
                            NavMeshAgent agent = ai.agent;
                            float[] speeds = Plugin.SpeedAndAccelerationEnemyList(enemyAI);
                            float newSpeed = Plugin.GetInt("Quick Sand Config", "Slowing Speed") * 0.01f;
                            if (speeds[0] * newSpeed != agent.speed)
                            {
                                agent.speed = speeds[0] * newSpeed;
                            }
                            if (speeds[1] * newSpeed != agent.acceleration)
                            {
                                agent.acceleration = speeds[1] * newSpeed;
                            }
                        }
                        if (Plugin.GetBool("Mobs", Plugin.RemoveInvalidCharacters(ai.enemyType.enemyName) + ".Quicksand Kill"))
                        {
                            SetSinking(enemyAI);
                        }
                    }
                }
            }
            return false;
        }

        public static bool OnTriggerExitPatch(ref QuicksandTrigger __instance, Collider other)
        {
            if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null && Plugin.GetBool("Mobs", "QuicksandAllMobs"))
            {
                EnemyAICollisionDetect enemyAI = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                EnemyAI ai = enemyAI.mainScript;
                float[] speeds = Plugin.SpeedAndAccelerationEnemyList(enemyAI);

                ai.agent.speed = speeds[0];
                ai.agent.acceleration = speeds[1];
                if (Plugin.GetBool("Mobs", Plugin.RemoveInvalidCharacters(ai.enemyType.enemyName) + ".Quicksand Kill"))
                {
                    ai.transform.position = new Vector3(ai.transform.position.x, Plugin.positions[enemyAI.GetInstanceID()].y, ai.transform.position.z);
                    ai.SyncPositionToClients();
                    Plugin.sinkingValues[enemyAI.GetInstanceID()] = 0f;
                    Plugin.sinkingProgress[enemyAI.GetInstanceID()] = 0;
                }
            }
            else
            {
                __instance.OnExit(other);
            }
            return false;
        }

        public static bool StopSinkingLocalPlayerPatch(ref QuicksandTrigger __instance, PlayerControllerB playerScript)
        {
            if (__instance.sinkingLocalPlayer)
            {
                __instance.sinkingLocalPlayer = false;

                playerScript.sourcesCausingSinking = Mathf.Clamp(playerScript.sourcesCausingSinking - 1, 0, 100);
                playerScript.isMovementHindered = Mathf.Clamp(playerScript.isMovementHindered - 1, 0, 100);

                MethodInfo reduceMethod = Plugin.rubberBootsType.GetMethod("ReduceMovementHinderance");
                MethodInfo clearMethod = Plugin.rubberBootsType.GetMethod("ClearMovementHinderance");

                float adjustedHinderance = (float)reduceMethod.Invoke(null, [__instance.movementHinderance]);

                playerScript.hinderedMultiplier = Mathf.Clamp(
                    playerScript.hinderedMultiplier / adjustedHinderance,
                    1f, 100f
                );

                clearMethod.Invoke(null, [(int)adjustedHinderance]);

                if (playerScript.isMovementHindered == 0 && __instance.isWater)
                {
                    playerScript.isUnderwater = false;
                }
            }
            return false;
        }

        public static void SetSinking(EnemyAICollisionDetect enemy)
        {
            if (!Plugin.sinkingProgress.ContainsKey(enemy.GetInstanceID()))
            {
                Plugin.sinkingProgress.Add(enemy.GetInstanceID(), 0f);
            }
            if (!Plugin.positions.ContainsKey(enemy.GetInstanceID()))
            {
                Plugin.positions.Add(enemy.GetInstanceID(), enemy.transform.position);
            }

            if (!Plugin.sinkingValues.ContainsKey(enemy.GetInstanceID()))
            {
                Plugin.sinkingValues.Add(enemy.GetInstanceID(), 0);
            }
            float sinkingValue = Plugin.sinkingValues[enemy.GetInstanceID()];
            // Estimate height
            float height = enemy.GetComponent<Collider>().bounds.size.y;

            // Use height to scale sink distance or time
            float heightModifier = height * 0.5f;

            Plugin.sinkingProgress[enemy.GetInstanceID()] = Mathf.Clamp((Time.deltaTime / Plugin.GetFloat("Quick Sand Config", "Sink Time") / heightModifier), 0, 1);

            Vector3 enemyPos = enemy.mainScript.transform.position;
            // Sink toward a target offset (e.g., fully underground)
            Vector3 targetPos = enemyPos - Vector3.up * height;
            enemy.mainScript.transform.position = Vector3.Lerp(enemyPos, targetPos, Plugin.sinkingProgress[enemy.GetInstanceID()]);
            enemy.mainScript.SyncPositionToClients();
            sinkingValue = Mathf.Clamp(sinkingValue + (Time.deltaTime * 0.75f), 0f, Plugin.GetFloat("Quick Sand Config", "Sink Time"));
            if (Plugin.sinkingValues[enemy.GetInstanceID()] >= Plugin.GetFloat("Quick Sand Config", "Sink Time") - 0.01f)
            {
                Plugin.sinkingProgress.Remove(enemy.GetInstanceID());
                Plugin.sinkingValues.Remove(enemy.GetInstanceID());
                Plugin.positions.Remove(enemy.GetInstanceID());
                enemy.mainScript.KillEnemyOnOwnerClient(enemy.mainScript.enemyType.destroyOnDeath);
            }
            if (enemy != null)
            {
                if (enemy.mainScript != null)
                {
                    if (!enemy.mainScript.isEnemyDead)
                    {
                        Plugin.sinkingValues[enemy.GetInstanceID()] = sinkingValue;
                    }
                }
            }
        }
    }
}
