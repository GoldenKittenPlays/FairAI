using HarmonyLib;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretAIPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void patchUpdate(ref Turret __instance, ref float ___turretInterval)
        {
            if (__instance.turretMode == TurretMode.Detection)
            {
                if (!__instance.IsServer)
                {
                    return;
                }
                if (___turretInterval >= 0.25f)
                {
                    ___turretInterval = 0f;
                    EnemyAICollisionDetect enemy = GetTarget(__instance, 1.35f, angleRangeCheck: true);
                    if (enemy != null && !enemy.mainScript.isEnemyDead)
                    {
                        __instance.turretMode = (TurretMode)1;
                    }
                }
            }
            if (__instance.turretMode == TurretMode.Berserk)
            {
                if (___turretInterval >= 0.21f)
                {
                    ___turretInterval = 0f;
                    EnemyAICollisionDetect enemy = GetTarget(__instance, 3f);
                    enemy?.mainScript.HitEnemyOnLocalClient(2);
                    Ray shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                    if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                    {
                        __instance.bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
                    }
                }
            }
            if (__instance.turretMode == TurretMode.Firing)
            {
                if (___turretInterval >= 0.21f)
                {
                    ___turretInterval = 0f;
                    EnemyAICollisionDetect enemy = GetTarget(__instance, 3f);
                    enemy?.mainScript.HitEnemyOnLocalClient(2);
                    Ray shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                    if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                    {
                        __instance.bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
                    }
                }
            }
        }

        [HarmonyPatch("TurnTowardsTargetIfHasLOS")]
        [HarmonyPostfix]
        static void patchTurnTowardsTargetIfHasLOS(ref Turret __instance, ref bool ___hasLineOfSight, ref float ___lostLOSTimer)
        {
            bool flag = true;
            EnemyAICollisionDetect enemy = GetTarget(__instance);
            if (enemy != null)
            {
                if (enemy.mainScript.isEnemyDead || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
                {
                    flag = false;
                }
                if (Physics.Linecast(__instance.aimPoint.position, enemy.mainScript.transform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    flag = false;
                }
                if (flag)
                {
                    ___hasLineOfSight = true;
                    ___lostLOSTimer = 0f;
                    __instance.tempTransform.position = enemy.mainScript.transform.position;
                    __instance.tempTransform.position -= Vector3.up * 0.15f;
                    __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                    return;
                }
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
                    }
                    forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
                }
            }
            return null;
        }
    }
}
