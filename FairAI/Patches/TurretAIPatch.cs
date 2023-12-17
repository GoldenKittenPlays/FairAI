using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretAIPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void patchUpdate(ref Turret __instance)
        {
            if (__instance.turretMode == TurretMode.Detection) 
            {
                EnemyAI enemy = GetTarget(__instance);
                if (enemy != null)
                {
                    enemy.HitEnemyOnLocalClient();
                }
            }
        }

        static EnemyAI GetTarget(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            if (turret.targetTransform == null)
            {
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (int)(0f - turret.rotationRange) / radius, 0f) * forward;
                float num = turret.rotationRange / radius * 2f;
                for (int i = 0; i <= 6; i++)
                {
                    Ray shootRay = new Ray(turret.centerPoint.position, forward);
                    if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, 1051400, QueryTriggerInteraction.Ignore))
                    {
                        EnemyAI component = hit.transform.GetComponent<EnemyAI>();
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
    }
}
