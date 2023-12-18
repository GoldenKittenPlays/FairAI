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
            if (__instance.targetTransform != null)
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
                TurnTowardsTarget(__instance, ref ___hasLineOfSight, ref ___lostLOSTimer);
            }
            else if (wasTargetingLastFrame)
            {
                wasTargetingLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
            }
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    if (___turretModeLastFrame != 0)
                    {
                        ___turretModeLastFrame = TurretMode.Detection;
                        ___rotatingClockwise = false;
                        __instance.mainAudio.Stop();
                        __instance.farAudio.Stop();
                        __instance.berserkAudio.Stop();
                        if (___fadeBulletAudioCoroutine != null)
                        {
                            __instance.StopCoroutine(___fadeBulletAudioCoroutine);
                        }
                        ___fadeBulletAudioCoroutine = __instance.StartCoroutine("FadeBulletAudio");
                        __instance.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                        __instance.rotationSpeed = 28f;
                        ___rotatingSmoothly = true;
                        __instance.turretAnimator.SetInteger("TurretMode", 0);
                        ___turretInterval = UnityEngine.Random.Range(0f, 0.15f);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (___switchRotationTimer >= 7f)
                    {
                        ___switchRotationTimer = 0f;
                        bool setRotateRight = !___rotatingRight;
                        __instance.SwitchRotationClientRpc(setRotateRight);
                        __instance.SwitchRotationOnInterval(setRotateRight);
                    }
                    else
                    {
                        ___switchRotationTimer += Time.deltaTime;
                    }
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
                    else
                    {
                        ___turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Charging:
                    if (___turretModeLastFrame != TurretMode.Charging)
                    {
                        ___turretModeLastFrame = TurretMode.Charging;
                        ___rotatingClockwise = false;
                        __instance.mainAudio.PlayOneShot(__instance.detectPlayerSFX);
                        __instance.berserkAudio.Stop();
                        WalkieTalkie.TransmitOneShotAudio(__instance.mainAudio, __instance.detectPlayerSFX);
                        __instance.rotationSpeed = 95f;
                        ___rotatingSmoothly = false;
                        ___lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                    }
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
                        else
                        {
                            __instance.turretMode = (TurretMode)2;
                            __instance.SetToModeClientRpc(2);
                        }
                    }
                    else
                    {
                        ___turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Firing:
                    if (___turretModeLastFrame != TurretMode.Firing)
                    {
                        ___turretModeLastFrame = TurretMode.Firing;
                        __instance.berserkAudio.Stop();
                        __instance.mainAudio.clip = __instance.firingSFX;
                        __instance.mainAudio.Play();
                        __instance.farAudio.clip = __instance.firingFarSFX;
                        __instance.farAudio.Play();
                        __instance.bulletParticles.Play(withChildren: true);
                        __instance.bulletCollisionAudio.Play();
                        if (___fadeBulletAudioCoroutine != null)
                        {
                            __instance.StopCoroutine(___fadeBulletAudioCoroutine);
                        }
                        __instance.bulletCollisionAudio.volume = 1f;
                        ___rotatingSmoothly = false;
                        ___lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 2);
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
                    else
                    {
                        ___turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Berserk:
                    if (___turretModeLastFrame != TurretMode.Berserk)
                    {
                        ___turretModeLastFrame = TurretMode.Berserk;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                        ___berserkTimer = 1.3f;
                        __instance.berserkAudio.Play();
                        __instance.rotationSpeed = 77f;
                        ___enteringBerserkMode = true;
                        ___rotatingSmoothly = true;
                        ___lostLOSTimer = 0f;
                        wasTargetingLastFrame = false;
                        __instance.targetTransform = null;
                    }
                    if (___enteringBerserkMode)
                    {
                        ___berserkTimer -= Time.deltaTime;
                        if (___berserkTimer <= 0f)
                        {
                            ___enteringBerserkMode = false;
                            ___rotatingClockwise = true;
                            ___berserkTimer = 9f;
                            __instance.turretAnimator.SetInteger("TurretMode", 2);
                            __instance.mainAudio.clip = __instance.firingSFX;
                            __instance.mainAudio.Play();
                            __instance.farAudio.clip = __instance.firingFarSFX;
                            __instance.farAudio.Play();
                            __instance.bulletParticles.Play(withChildren: true);
                            __instance.bulletCollisionAudio.Play();
                            if (___fadeBulletAudioCoroutine != null)
                            {
                                __instance.StopCoroutine(___fadeBulletAudioCoroutine);
                            }
                            __instance.bulletCollisionAudio.volume = 1f;
                        }
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
                    else
                    {
                        ___turretInterval += Time.deltaTime;
                    }
                    if (__instance.IsServer)
                    {
                        ___berserkTimer -= Time.deltaTime;
                        if (___berserkTimer <= 0f)
                        {
                            __instance.turretMode = 0;
                            __instance.SetToModeClientRpc(0);
                        }
                    }
                    break;
            }
        }

        [ClientRpc]
        static void SwitchTargetedClientRpc(bool setModeToCharging, Turret turret)
        {
            setModeToCharging = false;
            var type = AccessTools.FirstInner(typeof(Turret), t => t.Name.Contains("__rpc_exec_stage"));
            NetworkManager networkManager = turret.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (!type.GetEnumName(type).ToUpper().Equals("CLIENT") && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = default(ClientRpcParams);
                FastBufferWriter bufferWriter = (FastBufferWriter)AccessTools.Method("__beginSendClientRpc").Invoke(turret, new System.Object[] { 866050294u, clientRpcParams, RpcDelivery.Reliable });
                //BytePacker.WriteValueBitPacked(bufferWriter, playerId);
                bufferWriter.WriteValueSafe(in setModeToCharging, default(FastBufferWriter.ForPrimitives));
                ref FastBufferWriter writer = ref bufferWriter;
                AccessTools.Method("__endSendClientRpc").Invoke(turret, new System.Object[] { writer, 866050294u, clientRpcParams, RpcDelivery.Reliable });
            }
            if (type.GetEnumName(type).ToUpper().Equals("CLIENT") && (networkManager.IsClient || networkManager.IsHost) && !turret.IsServer)
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
            var type = AccessTools.FirstInner(typeof(Turret), t => t.Name.Contains("__rpc_exec_stage"));
            NetworkManager networkManager = turret.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                if (!type.GetEnumName(type).ToUpper().Equals("CLIENT") && (networkManager.IsServer || networkManager.IsHost))
                {
                    ClientRpcParams clientRpcParams = default(ClientRpcParams);
                    FastBufferWriter bufferWriter = (FastBufferWriter)AccessTools.Method("__beginSendClientRpc").Invoke(turret, new System.Object[] { 2800017671u, clientRpcParams, RpcDelivery.Reliable });
                    ref FastBufferWriter writer = ref bufferWriter;
                    AccessTools.Method("__endSendClientRpc").Invoke(turret, new System.Object[] { writer, 2800017671u, clientRpcParams, RpcDelivery.Reliable });
                }
                if (type.GetEnumName(type).ToUpper().Equals("CLIENT") && (networkManager.IsClient || networkManager.IsHost))
                {
                    turret.targetTransform = null;
                }
            }
        }

        static void TurnTowardsTarget(Turret turret, ref bool ___hasLineOfSight, ref float ___lostLOSTimer)
        {
            bool flag = true;
            if (Vector3.Angle(turret.targetTransform.position - turret.centerPoint.position, turret.forwardFacingPos.forward) > turret.rotationRange)
            {
                flag = false;
            }
            if (Physics.Linecast(turret.aimPoint.position, turret.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                flag = false;
            }
            if (flag)
            {
                ___hasLineOfSight = true;
                ___lostLOSTimer = 0f;
                turret.tempTransform.position = turret.targetTransform.position;
                turret.tempTransform.position -= Vector3.up * 0.15f;
                turret.turnTowardsObjectCompass.LookAt(turret.tempTransform);
                return;
            }
            if (___hasLineOfSight)
            {
                ___hasLineOfSight = false;
                ___lostLOSTimer = 0f;
            }
            if (!turret.IsServer)
            {
                return;
            }
            ___lostLOSTimer += Time.deltaTime;
            if (___lostLOSTimer >= 2f)
            {
                ___lostLOSTimer = 0f;
                Debug.Log("Turret: LOS timer ended on server. checking for new player target");
                EnemyAICollisionDetect enemy = GetTarget(turret);
                //PlayerControllerB playerControllerB = CheckForPlayersInLineOfSight();
                if (enemy != null)
                {
                    //targetPlayerWithRotation = playerControllerB;
                    //SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId);
                    turret.targetTransform = enemy.transform;
                    Debug.Log("Turret: Got new player target");
                }
                else
                {
                    Debug.Log("Turret: No new player to target; returning to detection mode.");
                    turret.targetTransform = null;
                    //targetPlayerWithRotation = null;
                    //RemoveTargetedPlayerClientRpc();
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
