using DunGen;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretAIPatch
    {
        /*
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void patchUpdate(ref Turret __instance, 
            ref TurretMode ___turretModeLastFrame,
            ref bool ___hasLineOfSight,
            ref bool ___enteringBerserkMode,
            ref bool ___wasTargetingPlayerLastFrame,
            ref float ___turretInterval,
            ref float ___lostLOSTimer)
        {
            if (!__instance.turretActive)
            {
                return;
            }
            if (__instance.targetPlayerWithRotation == null)
            {
                if (GetTarget(__instance) != null)
                {
                    __instance.targetTransform = GetTarget(__instance).transform;
                    bool flag = true;
                    if (Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
                    {
                        flag = false;
                    }
                    if (Physics.Linecast(__instance.aimPoint.position, __instance.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                    {
                        flag = false;
                    }
                    if (flag)
                    {
                        ___hasLineOfSight = true;
                        ___lostLOSTimer = 0f;
                        __instance.tempTransform.position = __instance.targetTransform.position;
                        __instance.tempTransform.position -= Vector3.up * 0.15f;
                        __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                        return;
                    }
                    if (___hasLineOfSight)
                    {
                        ___hasLineOfSight = false;
                        ___lostLOSTimer = 0f;
                    }
                    if (!__instance.IsServer)
                    {
                        return;
                    }
                }
            }
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    if (___turretInterval >= 0.25f)
                    {
                        if (__instance.targetPlayerWithRotation == null) {
                            ___turretInterval = 0f;
                            EnemyAICollisionDetect enemy = GetTarget(__instance, 1.35f, true);
                            if (enemy != null && !enemy.mainScript.isEnemyDead)
                            {
                                __instance.targetTransform = enemy.transform;
                                __instance.turretMode = (TurretMode)1;
                            }
                        }
                    }
                    break;
                case TurretMode.Firing:
                    if (___turretInterval >= 0.21f)
                    {
                        ___turretInterval = 0f;
                        GetTarget(__instance, 3f)?.mainScript.HitEnemyOnLocalClient(2);
                        Ray shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            __instance.bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
                        }
                    }
                    break;
                case TurretMode.Berserk:
                    if (___enteringBerserkMode)
                    {
                        break;
                    }
                    if (___turretInterval >= 0.21f)
                    {
                        ___turretInterval = 0f;
                        GetTarget(__instance, 3f)?.mainScript.HitEnemyOnLocalClient(2);
                        Ray shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            __instance.bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
                        }
                    }
                    break;
            }
        }

        static EnemyAICollisionDetect GetTarget(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            if (turret.targetPlayerWithRotation == null)
            {
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (int)(0f - turret.rotationRange) / radius, 0f) * forward;
                float num = turret.rotationRange / radius * 2f;
                for (int i = 0; i <= 6; i++)
                {
                    Ray shootRay = new Ray(turret.centerPoint.position, forward);
                    //if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, 1051400, QueryTriggerInteraction.Ignore))
                    if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
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
                        if ((turret.turretMode == TurretMode.Firing || (turret.turretMode == TurretMode.Berserk && !turret.berserkAudio.isPlaying)) && hit.transform.tag.ToUpper().Contains("RAGDOLL"))
                        {
                            Rigidbody component2 = hit.transform.GetComponent<Rigidbody>();
                            if (component2 != null)
                            {
                                component2.AddForce(forward.normalized * 42f, ForceMode.Impulse);
                            }
                        }
                    }
                    forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
                }
            }
            return null;
        }
        */
    }
}
