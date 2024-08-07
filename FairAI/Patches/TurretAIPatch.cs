﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace FairAI.Patches
{
    internal class TurretAIPatch
    {
        public static float viewRadius = 16;
        public static float viewAngle = 90;

        /*
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);
            int startIndex = -1;
            //Label startLabel = il.DefineLabel();
            for (int i = 0; i < codes.Count - 1; i++) // -1 since we will be checking i + 1
            {
                if (codes[i].opcode == OpCodes.Ret && codes[i + 1].opcode == OpCodes.Ldarg_0)
                {
                    Plugin.logger.LogInfo("Found Start Code " + i + ": " + codes[i].ToString());
                    startIndex = i;
                    //codes[i].labels.Add(startLabel);
                    break;
                }
            }
            int endIndex = -1;
            //Label endLabel = il.DefineLabel();
            for (int i = 0; i < codes.Count - 3; i++) // -1 since we will be checking i + 1
            {
                if (CodeInstructionExtensions.StoresField(codes[i], typeof(Turret).GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance))) 
                {
                    if (CodeInstructionExtensions.StoresField(codes[i + 3], typeof(Turret).GetField("turretMode")))
                    {
                        Plugin.logger.LogInfo("Found End Code " + (i + 3) + ": " + codes[i + 3].ToString());
                        endIndex = i + 3;
                        break;
                    }
                }
            }
            if (startIndex > -1 && endIndex > -1)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex);
                Plugin.logger.LogInfo("Removed Original Turret Targeting Code!");
            }
            Plugin.logger.LogInfo("Removal Complete!");
            return codes.AsEnumerable();
        }
        */

        public static void PatchUpdate(ref Turret __instance)
        {
            if (!(__instance == null))
            {
                if (Plugin.AllowFairness(__instance.transform.position))
                {
                    FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                    if (turret == null)
                    {
                        turret = __instance.gameObject.AddComponent<FAIR_AI>();
                    }
                    System.Type turretType = typeof(Turret);
                    FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo hasLineOfSight = turretType.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
                    /*
                    if (__instance.targetPlayerWithRotation != null || turret.targetWithRotation != null)
                    {
                        if (!(bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                        {
                            wasTargetingPlayerLastFrame.SetValue(__instance, true);
                            if (__instance.turretMode == TurretMode.Detection)
                            {
                                __instance.turretMode = TurretMode.Charging;
                            }
                        }
                        MethodInfo SetTargetToPlayerBody = turretType.GetMethod("SetTargetToPlayerBody", BindingFlags.NonPublic | BindingFlags.Instance);
                        SetTargetToPlayerBody.Invoke(__instance, new object[] { });
                        MethodInfo TurnTowardsTargetIfHasLOS = turretType.GetMethod("TurnTowardsTargetIfHasLOS", BindingFlags.NonPublic | BindingFlags.Instance);
                        TurnTowardsTargetIfHasLOS.Invoke(__instance, new object[] { });
                    }
                    else if ((bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                    {
                        wasTargetingPlayerLastFrame.SetValue(__instance, false);
                        __instance.turretMode = TurretMode.Detection;
                    }
                    */
                    if (!(turret == null))
                    {
                        FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
                        switch (__instance.turretMode)
                        {
                            case TurretMode.Charging:
                                if ((float)turretInterval.GetValue(__instance) >= 1.5f)
                                {
                                    Debug.Log("Charging timer is up, setting to firing mode");
                                    if (!(bool)hasLineOfSight.GetValue(__instance))
                                    {
                                        Debug.Log("hasLineOfSight is false");
                                        turret.targetWithRotation = null;
                                        turret.RemoveTargetedEnemyClientRpc();
                                    }
                                    else
                                    {
                                        __instance.turretMode = TurretMode.Firing;
                                        __instance.SetToModeClientRpc(2);
                                    }
                                }
                                break;
                            case TurretMode.Firing:
                                if ((float)turretInterval.GetValue(__instance) >= 0.21f)
                                {
                                    //turretInterval.SetValue(__instance, 0f);
                                    //List<EnemyAICollisionDetect> enemies = GetActualTargets(__instance);
                                    //if (enemies.Any())
                                    //{
                                    Vector3 forward = __instance.aimPoint.forward;
                                    forward = Quaternion.Euler(0f, (float)(int)(0f - __instance.rotationRange) / (float)3, 0f) * forward;
                                    Plugin.AttackTargets(__instance.centerPoint.position, forward, 30f);
                                    //}
                                }
                                break;
                            case TurretMode.Berserk:
                                if ((float)turretInterval.GetValue(__instance) >= 0.21f)
                                {
                                    ///turretInterval.SetValue(__instance, 0f);
                                    //List<EnemyAICollisionDetect> enemies = GetActualTargets(__instance);
                                    //if (enemies.Any())
                                    //{
                                    Vector3 forward = __instance.aimPoint.forward;
                                    forward = Quaternion.Euler(0f, (float)(int)(0f - __instance.rotationRange) / (float)3, 0f) * forward;
                                    Plugin.AttackTargets(__instance.centerPoint.position, forward, 30f);
                                    //}
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void PatchSetTargetToPlayerBody(ref Turret __instance)
        {
            if (!(__instance == null))
            {
                System.Type typ = typeof(Turret);
                FieldInfo target_dead_type = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                if (turret.targetWithRotation != null)
                {
                    if (!(bool)target_dead_type.GetValue(__instance))
                    {
                        target_dead_type.SetValue(__instance, true);
                        //__instance.targetingDeadPlayer = true;
                    }
                    if (!turret.targetWithRotation.GetComponent<EnemyAI>().isEnemyDead)
                    {
                        target_dead_type.SetValue(__instance, false);
                        //__instance.targetingDeadPlayer = false;
                        __instance.targetTransform = turret.targetWithRotation.transform;
                    }
                }
            }
        }

        public static void PatchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            TurnTowardsTargetEnemyIfHasLOS(__instance);
        }

        public static bool TurnTowardsTargetEnemyIfHasLOS(Turret turret)
        {
            bool flag = true;
            System.Type typ = typeof(Turret);
            FieldInfo target_dead_type = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            var target_dead_value = target_dead_type.GetValue(turret);
            FieldInfo has_los_type = typ.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo los_timer_type = typ.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)target_dead_value || Vector3.Angle(turret.targetTransform.position - turret.centerPoint.position, turret.forwardFacingPos.forward) > turret.rotationRange)
            {
                flag = false;
            }

            if (Physics.Linecast(turret.aimPoint.position, turret.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                flag = false;
            }
            List<EnemyAICollisionDetect> list = GetActualTargets(turret);
            if (flag && list != null && list.Any())
            {
                has_los_type.SetValue(turret, true);
                //__instance.hasLineOfSight = true;
                los_timer_type.SetValue(turret, 0f);
                //__instance.lostLOSTimer = 0f;
                if (turret.GetComponent<FAIR_AI>() != null)
                {
                    FAIR_AI ai = turret.GetComponent<FAIR_AI>();
                    if (ai.targetWithRotation == null)
                    {
                        ai.targetWithRotation = list[0].mainScript;
                    }
                    turret.tempTransform.position = ai.targetWithRotation.transform.position;
                    turret.tempTransform.position -= Vector3.up * 0.15f;
                    turret.turnTowardsObjectCompass.LookAt(turret.tempTransform);
                }
            }
            if (!flag)
            {
                var has_los_value = has_los_type.GetValue(turret);
                if (((bool)has_los_value))
                {
                    has_los_type.SetValue(turret, false);
                    los_timer_type.SetValue(turret, 0f);
                    //__instance.hasLineOfSight = false;
                    //__instance.lostLOSTimer = 0f;
                }
                if (!turret.IsServer)
                {
                    los_timer_type.SetValue(turret, (float)los_timer_type.GetValue(turret) + Time.deltaTime);
                    FAIR_AI aim = turret.gameObject.GetComponent<FAIR_AI>();
                    List<EnemyAICollisionDetect> enemies = GetActualTargets(turret);
                    if (enemies.Any())
                    {
                        aim.targetWithRotation = enemies[0].mainScript;
                        aim.SwitchedTargetedEnemyClientRpc(turret, enemies[0].mainScript);
                    }
                    else
                    {
                        aim.targetWithRotation = null;
                        aim.RemoveTargetedEnemyClientRpc();
                    }
                }
            }
            return flag;
        }

        public static List<EnemyAICollisionDetect> GetActualTargets(Turret turret)
        {
            List<EnemyAICollisionDetect> enemies = new List<EnemyAICollisionDetect>();
            List<EnemyAICollisionDetect> newTargets = GetTargets(turret);
            if (newTargets != null)
            {
                newTargets.RemoveAll(t => t == null);
                if (newTargets.Any())
                {
                    foreach (EnemyAICollisionDetect target in newTargets)
                    {
                        if (target != null)
                        {
                            //if ((!target.mainScript.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.mainScript.enemyType.enemyName)))
                            //{
                                enemies.Add(target);
                            //}
                        }
                    }
                }
            }
            return enemies;
        }

        static List<EnemyAICollisionDetect> GetTargets(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            List<Transform> targets = FindVisibleTargets(turret);
            List<EnemyAICollisionDetect> en = new List<EnemyAICollisionDetect>();
            if (targets.Any())
            {
                targets.ForEach(e =>
                {
                    EnemyAICollisionDetect enemy = e.GetComponent<EnemyAICollisionDetect>();
                    if (enemy != null)
                    {
                        //if (enemy.mainScript != null && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemy.mainScript.enemyType.enemyName))
                        //{
                            en.Add(enemy);
                        //}
                    }
                });
            }
            return en;
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
            Collider[] targetsInViewRadius = Physics.OverlapSphere(turret.aimPoint.position, viewRadius, (2621448 | Plugin.enemyMask | StartOfRound.Instance.playersMask));
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                Transform target = targetsInViewRadius[i].transform;
                Vector3 dirToTarget = (target.position - turret.aimPoint.position).normalized;
                if (Vector3.Angle(turret.aimPoint.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(turret.aimPoint.position, target.position);

                    if (!Physics.Raycast(turret.aimPoint.position, dirToTarget, dstToTarget, ~(2621448 | Plugin.enemyMask | StartOfRound.Instance.playersMask)))
                    {
                        if (target.GetComponent<EnemyAICollisionDetect>() != null)
                        {
                            targets.Add(target);
                        }
                        else if (target.GetComponent<EnemyAI>() != null)
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
            return targets;
        }

        /*
        public static List<Transform> FindVisibleTargets(Turret turret)
        {
            Collider[] targetsInViewRadius = Physics.OverlapSphere(turret.aimPoint.position, viewRadius, Plugin.enemyMask);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                Transform target = targetsInViewRadius[i].transform;
                Vector3 dirToTarget = (target.position - turret.aimPoint.position).normalized;
                if (Vector3.Angle(turret.aimPoint.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(turret.aimPoint.position, target.position);

                    if (!Physics.Raycast(turret.aimPoint.position, dirToTarget, dstToTarget, ~Plugin.enemyMask))
                    {
                        if (target.GetComponent<EnemyAICollisionDetect>() != null)
                        {
                            if (Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.GetComponent<EnemyAICollisionDetect>().mainScript.enemyType.enemyName)) 
                            {
                                targets.Add(target);
                            }
                        }
                        if (target.GetComponent<EnemyAI>() != null)
                        {
                            if (Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.GetComponent<EnemyAI>().enemyType.enemyName))
                            {
                                targets.Add(target);
                            }
                        }
                    }
                }
            }
            return targets;
        }
        */
    }
}