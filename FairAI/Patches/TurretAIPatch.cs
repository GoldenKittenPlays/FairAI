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
        static bool wasTargetingLastFrame;
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void patchUpdate(ref Turret __instance, 
            ref TurretMode ___turretModeLastFrame,
            ref Coroutine ___fadeBulletAudioCoroutine,
            ref bool ___rotatingClockwise,
            ref bool ___rotatingSmoothly,
            ref bool ___rotatingRight,
            ref bool ___hasLineOfSight,
            ref bool ___enteringBerserkMode,
            ref float ___turretInterval,
            ref float ___switchRotationTimer,
            ref float ___lostLOSTimer,
            ref float ___berserkTimer)
        {
            if (!__instance.turretActive)
            {
                wasTargetingLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
                __instance.targetTransform = null;
                return;
            }
            if (__instance.targetTransform == null)
            {
                if (!wasTargetingLastFrame)
                {
                    wasTargetingLastFrame = true;
                    if (__instance.turretMode == TurretMode.Detection)
                    {
                        __instance.turretMode = TurretMode.Charging;
                    }
                }
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
                ___lostLOSTimer += Time.deltaTime;
                if (___lostLOSTimer >= 2f)
                {
                    ___lostLOSTimer = 0f;
                    Debug.Log("Turret: LOS timer ended on server. checking for new player target");
                    EnemyAICollisionDetect enemy = GetTarget(__instance);
                    //PlayerControllerB playerControllerB = CheckForPlayersInLineOfSight();
                    if (enemy != null)
                    {
                        //targetPlayerWithRotation = playerControllerB;
                        //SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId);
                        __instance.targetTransform = enemy.transform;
                        Debug.Log("Turret: Got new player target");
                    }
                    else
                    {
                        Debug.Log("Turret: No new player to target; returning to detection mode.");
                        __instance.targetTransform = null;
                        //targetPlayerWithRotation = null;
                        //RemoveTargetedPlayerClientRpc();
                    }
                }
            }
            else if (wasTargetingLastFrame)
            {
                wasTargetingLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
            }
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    if (___turretInterval >= 0.25f)
                    {
                        ___turretInterval = 0f;
                        EnemyAICollisionDetect enemy = GetTarget(__instance, 1.35f, true);
                        if (enemy != null && !enemy.mainScript.isEnemyDead)
                        {
                            __instance.targetTransform = enemy.transform;
                            __instance.turretMode = (TurretMode)1;
                            SwitchTargetedClientRpc(setModeToCharging: true, __instance);
                        }
                        /*
                        PlayerControllerB playerControllerB = CheckForPlayersInLineOfSight(1.35f, angleRangeCheck: true);
                        if (playerControllerB != null && !playerControllerB.isPlayerDead)
                        {
                            targetPlayerWithRotation = playerControllerB;
                            SwitchTurretMode(1);
                            SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, setModeToCharging: true);
                        }
                        */
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
                            __instance.targetTransform = null;
                            RemoveTargetedPlayerClientRpc(__instance);
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
                    if (___turretModeLastFrame != TurretMode.Berserk)
                    {
                        wasTargetingLastFrame = false;
                        __instance.targetTransform = null;
                    }
                    if (___enteringBerserkMode)
                    {
                        break;
                    }
                    if (___turretInterval >= 0.21f)
                    {
                        ___turretInterval = 0f;
                        if (GetTarget(__instance, 3f) != null)
                        {
                            GetTarget(__instance, 3f).mainScript.HitEnemyOnLocalClient(2);
                        }
                        Ray shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(shootRay, out RaycastHit hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            __instance.bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
                        }
                    }
                    break;
            }
        }

        [ClientRpc]
        static void SwitchTargetedClientRpc(bool setModeToCharging, Turret turret)
        {
            setModeToCharging = false;
            var type = AccessTools.TypeByName("__RpcExecStage");
            var rpc = AccessTools.Field(type, "__rpc_exec_stage");
            NetworkManager networkManager = turret.NetworkManager;
            if (networkManager is null || !networkManager.IsListening)
            {
                return;
            }
            if (rpc.FieldHandle.Value.ToString().ToUpper().Equals("CLIENT") && (networkManager.IsClient || networkManager.IsHost) && !turret.IsServer)
            {
                turret.targetTransform = GetTarget(turret).transform;
                if (setModeToCharging)
                {
                    turret.turretMode = (TurretMode)1;
                }
            }
        }

        [ClientRpc]
        static void RemoveTargetedPlayerClientRpc(Turret turret)
        {
            //var type = AccessTools.FirstInner(typeof(NetworkBehaviour), t => t.Name.Contains("__rpc_exec_stage"));
            var type = AccessTools.TypeByName("__RpcExecStage");
            var rpc = AccessTools.Field(type, "__rpc_exec_stage");
            NetworkManager networkManager = turret.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                if (rpc.FieldHandle.Value.ToString().ToUpper().Equals("CLIENT") && (networkManager.IsClient || networkManager.IsHost))
                {
                    turret.targetTransform = null;
                }
            }
        }

        static EnemyAICollisionDetect GetTarget(Turret turret, float radius = 2f, bool angleRangeCheck = false)
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
    }
}
