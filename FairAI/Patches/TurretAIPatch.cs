using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretAIPatch
    {
        /*
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void patchUpdate(ref Turret __instance, ref float ___turretInterval, ref bool ___hasLineOfSight)
        {
            if (!__instance.turretActive)
            {
                __instance.turretMode = TurretMode.Detection;
                return;
            }
            EnemyAICollisionDetect enemy = GetTarget(__instance);
            if (enemy != null)
            {
                if (!enemy.mainScript.isEnemyDead)
                {
                    if (__instance.turretMode == TurretMode.Detection)
                    {
                        __instance.turretMode = TurretMode.Charging;
                    }
                }
                __instance.TurnTowardsTargetIfHasLOS();
            }
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    if (__instance.turretModeLastFrame != 0)
                    {
                        __instance.turretModeLastFrame = TurretMode.Detection;
                        if (__instance.fadeBulletAudioCoroutine != null)
                        {
                            __instance.StopCoroutine(__instance.fadeBulletAudioCoroutine);
                        }
                        __instance.fadeBulletAudioCoroutine = __instance.StartCoroutine(__instance.FadeBulletAudio());
                        __instance.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                        __instance.rotationSpeed = 28f;
                        __instance.rotatingSmoothly = true;
                        __instance.turretAnimator.SetInteger("TurretMode", 0);
                        __instance.turretInterval = Random.Range(0f, 0.15f);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (___turretInterval >= 0.25f)
                    {
                        ___turretInterval = 0f;
                        enemy = GetTarget(__instance, 1.35f, angleRangeCheck: true);
                        if (enemy != null && !enemy.mainScript.isEnemyDead)
                        {
                            __instance.SwitchTurretMode(1);
                            __instance.SetToModeClientRpc(1);
                        }
                    }
                    break;
                case TurretMode.Charging:
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (___turretInterval >= 1.5f)
                    {
                        ___turretInterval = 0f;
                        Debug.Log("Charging timer is up, setting to firing mode");
                        if (!___hasLineOfSight)
                        {
                            Debug.Log("hasLineOfSight is false");
                        }
                        else
                        {
                            __instance.SwitchTurretMode(2);
                            __instance.SetToModeClientRpc(2);
                        }
                    }
                    break;
                case TurretMode.Firing:
                    if (___turretInterval >= 0.21f)
                    {
                        EnemyAICollisionDetect target = GetTarget(__instance, 3f);
                        if (target != null)
                        {
                            if(!target.mainScript.isEnemyDead)
                            {
                                target.mainScript.HitEnemyOnLocalClient(1);
                            }
                        }
                    }
                    break;
                case TurretMode.Berserk:
                    if (__instance.turretInterval >= 0.21f)
                    {
                        EnemyAICollisionDetect target = GetTarget(__instance, 3f);
                        if (target != null)
                        {
                            if (!target.mainScript.isEnemyDead)
                            {
                                target.mainScript.HitEnemyOnLocalClient(1);
                            }
                        }
                    }
                    break;
            }
        }

        public static void patchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            EnemyAICollisionDetect enemy = GetTarget(__instance);
            if (enemy != null)
            {
                __instance.hasLineOfSight = true;
                __instance.lostLOSTimer = 0f;
                __instance.tempTransform.position = enemy.mainScript.transform.position;
                __instance.tempTransform.position -= Vector3.up * 0.15f;
                __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
            }

            if (__instance.hasLineOfSight)
            {
                __instance.hasLineOfSight = false;
                __instance.lostLOSTimer = 0f;
            }

            if (!__instance.IsServer)
            {
                return;
            }

            __instance.lostLOSTimer += Time.deltaTime;
            if (__instance.lostLOSTimer >= 2f)
            {
                __instance.lostLOSTimer = 0f;
                Debug.Log("Turret: LOS timer ended on server. checking for new player target");
                EnemyAICollisionDetect target = GetTarget(__instance);
                if (target != null)
                {
                    __instance.SetToModeClientRpc(1);
                    Debug.Log("Turret: Got new player target");
                }
                else
                {
                    Debug.Log("Turret: No new player to target; returning to detection mode.");
                    __instance.SetToModeClientRpc(0);
                }
            }
        }
        */

        static EnemyAICollisionDetect GetTarget(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            Vector3 forward = turret.aimPoint.forward;
            forward = Quaternion.Euler(0f, (int)(0f - turret.rotationRange) / radius, 0f) * forward;
            float num = turret.rotationRange / radius * 2f;
            for (int i = 0; i <= 6; i++)
            {
                //if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, 1051400, QueryTriggerInteraction.Ignore))
                if (Physics.Raycast(turret.centerPoint.position, forward, out RaycastHit hit, 30f, Plugin.wallsAndEnemyLayerMask, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        EnemyAICollisionDetect component = hit.transform.GetComponent<EnemyAICollisionDetect>();
                        if (component != null)
                        {
                            if (angleRangeCheck && Vector3.Angle(component.transform.position + Vector3.up * 1.75f - turret.centerPoint.position, turret.forwardFacingPos.forward) > turret.rotationRange)
                            {
                                return null;
                            }
                            return component;
                        }
                    }
                }
                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }
            return null;
        }
    }
}
