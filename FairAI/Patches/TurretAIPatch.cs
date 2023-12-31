using FairAI.Component;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretAIPatch
    {
        public static float viewRadius = 10;
        public static float viewAngle = 90;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void PatchUpdateBefore(ref Turret __instance)
        {
            if (__instance.turretModeLastFrame == TurretMode.Detection)
            {

                List<EnemyAICollisionDetect> enemies = GetActualTargets(GetTargets(__instance));
                if (enemies.Any())
                {
                    __instance.turretMode = TurretMode.Charging;
                }
            }
            else if(__instance.turretModeLastFrame == TurretMode.Charging)
            {
                List<EnemyAICollisionDetect> enemies = GetActualTargets(GetTargets(__instance));
                if (enemies.Any())
                {
                    __instance.turretMode = TurretMode.Firing;
                }
            }
            if(__instance.turretMode == TurretMode.Firing || __instance.turretMode == TurretMode.Berserk)
            {
                List<EnemyAICollisionDetect> enemies = GetActualTargets(GetTargets(__instance));
                if (!HitEnemies(__instance, enemies)
                    && (__instance.turretModeLastFrame == TurretMode.Firing || __instance.turretModeLastFrame == TurretMode.Berserk))
                {
                    __instance.turretMode = TurretMode.Detection;
                }
                if (__instance.CheckForPlayersInLineOfSight(3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    if (GameNetworkManager.Instance.localPlayerController.health - 33 > 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(33, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                    }
                    else
                    {
                        GameNetworkManager.Instance.localPlayerController.KillPlayer(__instance.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                    }
                }
            }
        }

        [HarmonyPatch("TurnTowardsTargetIfHasLOS")]
        [HarmonyPrefix]
        public static void PatchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            List<EnemyAICollisionDetect> enemies = GetActualTargets(GetTargets(__instance));
            if (enemies.Any())
            {
                foreach (EnemyAICollisionDetect enemy in enemies)
                {
                    __instance.hasLineOfSight = true;
                    __instance.lostLOSTimer = 0f;
                    __instance.tempTransform.position = enemy.mainScript.transform.position;
                    __instance.tempTransform.position -= Vector3.up * 0.15f;
                    __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                    break;
                }
            }

            if (__instance.hasLineOfSight)
            {
                __instance.hasLineOfSight = false;
                __instance.lostLOSTimer = 0f;
            }
        }

        public static bool HitEnemies(Turret turret, List<EnemyAICollisionDetect> visibleEnemies)
        {
            Vector3 direction = Quaternion.Euler(0f, (int)(0f - turret.rotationRange) / 2f, 0f) * turret.aimPoint.forward;
            Ray ray = new Ray(turret.forwardFacingPos.position, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, 30f, 2621448, QueryTriggerInteraction.Collide);
            bool hitEnemy = false;
            if (!hits.Any())
            {
                return hitEnemy;
            }
            foreach (var hit in hits)
            {
                EnemyAICollisionDetect enemy = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (visibleEnemies.Contains(enemy) && Plugin.CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.mainScript.enemyType.enemyName))
                {
                    enemy.mainScript.HitEnemyOnLocalClient(3);
                    hitEnemy = true;
                }
            }
            return hitEnemy;
        }

        public static List<EnemyAICollisionDetect> GetActualTargets(List<EnemyAICollisionDetect> targets)
        {
            List<EnemyAICollisionDetect> actual = new List<EnemyAICollisionDetect>();
             targets.ForEach(target => {
                 if (!target.mainScript.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.mainScript.enemyType.enemyName))
                 {
                     actual.Add(target);
                 }
             });
            return actual;
        }

        static List<EnemyAICollisionDetect> GetTargets(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            List<EnemyAICollisionDetect> enemies = new List<EnemyAICollisionDetect>();
            List<Transform> targets = FindVisibleTargets(turret);
            targets.ForEach(e => {
                EnemyAICollisionDetect component = e.GetComponent<EnemyAICollisionDetect>();
                if (component != null && e.GetComponent<FAIR_AI>() == null && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", component.mainScript.enemyType.enemyName))
                {
                    enemies.Add(component);
                }
            });
            return enemies;
        }

        // Util to show turret line of sight
        public static Vector3 DirectionFromAngle(Turret turret, float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal) 
            {
                angleInDegrees += turret.transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        public static List<Transform> FindVisibleTargets(Turret turret)
        {
            Collider[] targetsInViewRadius = Physics.OverlapSphere(turret.aimPoint.position, viewRadius, 2621448);
            List <Transform> targets = new List<Transform>();
            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                Transform target = targetsInViewRadius[i].transform;
                Vector3 dirToTarget = (target.position - turret.aimPoint.position).normalized;
                if (Vector3.Angle(turret.aimPoint.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(turret.aimPoint.position, target.position);

                    if (!Physics.Raycast(turret.aimPoint.position, dirToTarget, dstToTarget, ~2621448))
                    {
                        if (target.GetComponent<EnemyAICollisionDetect>() != null)
                        {
                            if (Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.GetComponent<EnemyAICollisionDetect>().mainScript.enemyType.enemyName))
                            {
                                targets.Add(target);
                            }
                        }
                    }
                }
            }
            return targets;
        }
    }
}
