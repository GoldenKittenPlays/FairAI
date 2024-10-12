using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                    FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>() ?? __instance.gameObject.AddComponent<FAIR_AI>();
                    System.Type turretType = typeof(Turret);
                    FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);

                    MethodInfo SetTargetToPlayerBody = turretType.GetMethod("SetTargetToPlayerBody", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo TurnTowardsTargetIfHasLOS = turretType.GetMethod("TurnTowardsTargetIfHasLOS", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (__instance.targetPlayerWithRotation == null)
                    {
                        if (!__instance.turretActive)
                        {
                            wasTargetingPlayerLastFrame.SetValue(__instance, false);
                            __instance.turretMode = TurretMode.Detection;
                            turret.targetWithRotation = null;
                            return;
                        }

                        if (turret.targetWithRotation != null)
                        {
                            if (!(bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                            {
                                wasTargetingPlayerLastFrame.SetValue(__instance, true);
                                if (__instance.turretMode == TurretMode.Detection)
                                {
                                    __instance.turretMode = TurretMode.Charging;
                                }
                            }
                            //SetTargetToEnemyBody(__instance);
                            //TurnTowardsTargetEnemyIfHasLOS(__instance);
                            SetTargetToPlayerBody.Invoke(__instance, new object[] { });
                            TurnTowardsTargetIfHasLOS.Invoke(__instance, new object[] { });
                        }
                        else if ((bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                        {
                            wasTargetingPlayerLastFrame.SetValue(__instance, false);
                            __instance.turretMode = TurretMode.Detection;
                        }

                        switch (__instance.turretMode)
                        {
                            case TurretMode.Detection:
                                DetectionFunction(__instance, turret);
                                break;
                            case TurretMode.Charging:
                                ChargingFunction(__instance, turret);
                                break;
                            case TurretMode.Firing:
                                FiringFunction(__instance);
                                break;
                            case TurretMode.Berserk:
                                BerserkFunction(__instance);
                                break;
                        }
                    }
                    else if (turret.targetWithRotation == null)
                    {
                        if (!__instance.turretActive)
                        {
                            wasTargetingPlayerLastFrame.SetValue(__instance, false);
                            __instance.turretMode = TurretMode.Detection;
                            __instance.targetPlayerWithRotation = null;
                            return;
                        }

                        if (__instance.targetPlayerWithRotation != null)
                        {
                            if (!(bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                            {
                                wasTargetingPlayerLastFrame.SetValue(__instance, true);
                                if (__instance.turretMode == TurretMode.Detection)
                                {
                                    __instance.turretMode = TurretMode.Charging;
                                }
                            }
                            SetTargetToPlayerBody.Invoke(__instance, new object[] { });
                            TurnTowardsTargetIfHasLOS.Invoke(__instance, new object[] { });
                        }
                        else if ((bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                        {
                            wasTargetingPlayerLastFrame.SetValue(__instance, false);
                            __instance.turretMode = TurretMode.Detection;
                        }

                        switch (__instance.turretMode)
                        {
                            case TurretMode.Detection:
                                DetectionFunction(__instance, turret);
                                break;
                            case TurretMode.Charging:
                                ChargingFunction(__instance, turret);
                                break;
                            case TurretMode.Firing:
                                FiringFunction(__instance);
                                break;
                            case TurretMode.Berserk:
                                BerserkFunction(__instance);
                                break;
                        }
                    }

                    if ((bool)rotatingClockwise.GetValue(__instance))
                    {
                        __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, __instance.turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
                        __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
                        return;
                    }

                    if ((bool)rotatingSmoothly.GetValue(__instance))
                    {
                        __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(__instance.targetRotation, 0f - __instance.rotationRange, __instance.rotationRange), 180f);
                    }

                    __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
                    
                    return;
                }
            }
        }
        public static void DetectionFunction(Turret turret, FAIR_AI turret_ai)
        {
            System.Type turretType = typeof(Turret);
            FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo switchRotationTimer = turretType.GetField("switchRotationTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingRight = turretType.GetField("rotatingRight", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo FadeBulletAudio = turretType.GetMethod("FadeBulletAudio", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((int)turretModeLastFrame.GetValue(turret) != 0)
            {
                turretModeLastFrame.SetValue(turret, TurretMode.Detection);
                rotatingClockwise.SetValue(turret, false);
                turret.mainAudio.Stop();
                turret.farAudio.Stop();
                turret.berserkAudio.Stop();

                if (fadeBulletAudioCoroutine.GetValue(turret) != null)
                {
                    turret.StopCoroutine((Coroutine)fadeBulletAudioCoroutine.GetValue(turret));
                }

                fadeBulletAudioCoroutine.SetValue(turret, turret.StartCoroutine((IEnumerator)FadeBulletAudio.Invoke(turret, new object[] { })));

                turret.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                turret.rotationSpeed = 28f;
                rotatingSmoothly.SetValue(turret, true);
                turret.turretAnimator.SetInteger("TurretMode", 0);
                turretInterval.SetValue(turret, Random.Range(0f, 0.15f));
            }

            if (!turret.IsServer)
            {
                return;
            }

            if ((float)switchRotationTimer.GetValue(turret) >= 7f)
            {
                switchRotationTimer.SetValue(turret, 0f);
                bool setRotateRight = !(bool)rotatingRight.GetValue(turret);
                turret.SwitchRotationClientRpc(setRotateRight);
                turret.SwitchRotationOnInterval(setRotateRight);
            }
            else
            {
                switchRotationTimer.SetValue(turret, (float)switchRotationTimer.GetValue(turret) + Time.deltaTime);
            }

            if ((float)turretInterval.GetValue(turret) >= 0.25f)
            {
                if (turret.targetPlayerWithRotation == null)
                {
                    turretInterval.SetValue(turret, 0f);
                    Vector3 forward = turret.aimPoint.forward;
                    forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / (float)3, 0f) * forward;
                    EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(turret, 3f);
                    if (enemy != null && !enemy.mainScript.isEnemyDead)
                    {
                        turret_ai.targetWithRotation = enemy.mainScript;
                        SwitchTurretMode.Invoke(turret, new object[] { 1 });
                        turret_ai.SwitchedTargetedEnemyClientRpc(turret, enemy.mainScript);
                        Plugin.logger.LogInfo("Detected Enemy!");
                    }
                }
                else if (turret_ai.targetWithRotation == null)
                {
                    turretInterval.SetValue(turret, 0f);
                    PlayerControllerB playerControllerB = CheckForPlayersInLOS(turret, 1.35f, true);
                    if (playerControllerB != null && !playerControllerB.isPlayerDead)
                    {
                        turret.targetPlayerWithRotation = playerControllerB;
                        SwitchTurretMode.Invoke(turret, new object[] { 1 });
                        turret.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, true);
                        Plugin.logger.LogInfo("Detected Player!");
                    }
                }
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }

        public static void ChargingFunction(Turret turret, FAIR_AI turret_ai)
        {
            System.Type turretType = typeof(Turret);
            FieldInfo hasLineOfSight = turretType.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Charging)
            {
                turretModeLastFrame.SetValue(turret, TurretMode.Charging);
                rotatingClockwise.SetValue(turret, false);
                turret.mainAudio.PlayOneShot(turret.detectPlayerSFX);
                turret.berserkAudio.Stop();
                WalkieTalkie.TransmitOneShotAudio(turret.mainAudio, turret.detectPlayerSFX);
                turret.rotationSpeed = 95f;
                rotatingSmoothly.SetValue(turret, false);
                lostLOSTimer.SetValue(turret, 0f);
                turret.turretAnimator.SetInteger("TurretMode", 1);
            }

            if (!turret.IsServer)
            {
                return;
            }

            if ((float)turretInterval.GetValue(turret) >= 1.5f)
            {
                turretInterval.SetValue(turret, 0f);
                Plugin.logger.LogInfo("Charging timer is up, setting to firing mode");
                if (!(bool)hasLineOfSight.GetValue(turret))
                {
                    Plugin.logger.LogInfo("hasLineOfSight is false");
                    turret.targetPlayerWithRotation = null;
                    turret.RemoveTargetedPlayerClientRpc();
                    turret_ai.targetWithRotation = null;
                    turret_ai.RemoveTargetedEnemyClientRpc();
                }
                else
                {
                    SwitchTurretMode.Invoke(turret, new object[] { 2 });
                    turret.SetToModeClientRpc(2);
                }
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }

        public static void FiringFunction(Turret turret)
        {
            System.Type turretType = typeof(Turret);
            FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hit = turretType.GetField("hit", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Firing)
            {
                turretModeLastFrame.SetValue(turret, TurretMode.Firing);
                turret.berserkAudio.Stop();
                RoundManager.Instance.PlayAudibleNoise(turret.berserkAudio.transform.position, 15f, 0.9f);
                turret.mainAudio.clip = turret.firingSFX;
                turret.mainAudio.Play();
                turret.farAudio.clip = turret.firingFarSFX;
                turret.farAudio.Play();
                turret.bulletParticles.Play(withChildren: true);
                turret.bulletCollisionAudio.Play();
                if (fadeBulletAudioCoroutine != null)
                {
                    if (fadeBulletAudioCoroutine.GetValue(turret) != null)
                    {
                        turret.StopCoroutine((Coroutine)fadeBulletAudioCoroutine.GetValue(turret));
                    }
                }

                turret.bulletCollisionAudio.volume = 1f;
                rotatingSmoothly.SetValue(turret, false);
                lostLOSTimer.SetValue(turret, 0f);
                turret.turretAnimator.SetInteger("TurretMode", 2);
            }

            if ((float)turretInterval.GetValue(turret) >= 0.21f)
            {
                Plugin.logger.LogInfo("Attacking Target");
                turretInterval.SetValue(turret, 0f);
                if (turret.CheckForPlayersInLineOfSight(3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    if (GameNetworkManager.Instance.localPlayerController.health > 50)
                    {
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                    }
                    else
                    {
                        GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                    }
                }

                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out RaycastHit rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    hit.SetValue(turret, rayHit);
                    turret.bulletCollisionAudio.transform.position = ((Ray)shootRay.GetValue(turret)).GetPoint(((RaycastHit)hit.GetValue(turret)).distance - 0.5f);
                }

                //Enemy Damage Code
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / (float)3, 0f) * forward;
                Plugin.AttackTargets(turret.centerPoint.position, forward, 30f);
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }

        public static void BerserkFunction(Turret turret)
        {
            System.Type turretType = typeof(Turret);
            FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hit = turretType.GetField("hit", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo berserkTimer = turretType.GetField("berserkTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo enteringBerserkMode = turretType.GetField("enteringBerserkMode", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Berserk)
            {
                turretModeLastFrame.SetValue(turret, TurretMode.Berserk);
                turret.turretAnimator.SetInteger("TurretMode", 1);
                berserkTimer.SetValue(turret, 1.3f);
                turret.berserkAudio.Play();
                turret.rotationSpeed = 77f;
                enteringBerserkMode.SetValue(turret, true);
                rotatingSmoothly.SetValue(turret, true);
                lostLOSTimer.SetValue(turret, 0f);
                wasTargetingPlayerLastFrame.SetValue(turret, false);
                turret.targetPlayerWithRotation = null;
            }

            if ((bool)enteringBerserkMode.GetValue(turret))
            {
                berserkTimer.SetValue(turret, (float)berserkTimer.GetValue(turret) - Time.deltaTime);
                if ((float)berserkTimer.GetValue(turret) <= 0f)
                {
                    enteringBerserkMode.SetValue(turret, false);
                    rotatingClockwise.SetValue(turret, true);
                    berserkTimer.SetValue(turret, 9f);
                    turret.turretAnimator.SetInteger("TurretMode", 2);
                    turret.mainAudio.clip = turret.firingSFX;
                    turret.mainAudio.Play();
                    turret.farAudio.clip = turret.firingFarSFX;
                    turret.farAudio.Play();
                    turret.bulletParticles.Play(withChildren: true);
                    turret.bulletCollisionAudio.Play();
                    if (fadeBulletAudioCoroutine != null)
                    {
                        turret.StopCoroutine((Coroutine)fadeBulletAudioCoroutine.GetValue(turret));
                    }

                    turret.bulletCollisionAudio.volume = 1f;
                }
                return;
            }

            if ((float)turretInterval.GetValue(turret) >= 0.21f)
            {
                turretInterval.SetValue(turret, 0f);
                if (turret.CheckForPlayersInLineOfSight(3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    if (GameNetworkManager.Instance.localPlayerController.health > 50)
                    {
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                    }
                    else
                    {
                        GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                    }
                }

                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));

                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out RaycastHit rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    hit.SetValue(turret, rayHit);
                    turret.bulletCollisionAudio.transform.position = ((Ray)shootRay.GetValue(turret)).GetPoint(((RaycastHit)hit.GetValue(turret)).distance - 0.5f);
                }

                //Enemy Damage Code
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / (float)3, 0f) * forward;
                Plugin.AttackTargets(turret.centerPoint.position, forward, 30f);
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }

            if (turret.IsServer)
            {
                berserkTimer.SetValue(turret, (float)berserkTimer.GetValue(turret) - Time.deltaTime);
                if ((float)berserkTimer.GetValue(turret) <= 0f)
                {
                    SwitchTurretMode.Invoke(turret, new object[] { 0 });
                    turret.SetToModeClientRpc(0);
                }
            }
        }

        public static bool CheckForTargetsInLOS(ref Turret __instance, float radius = 2f, bool angleRangeCheck = false)
        {
            FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
            PlayerControllerB player = CheckForPlayersInLOS(__instance, radius, angleRangeCheck);
            EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(__instance, radius, angleRangeCheck);
            if (player != null)
            {
                turret.targets = 
                new Dictionary<int, GameObject>
                {
                    { 0, player.gameObject }
                };
                return true;
            }

            if (enemy != null)
            {
                turret.targets =
                new Dictionary<int, GameObject>
                {
                    { 1, enemy.gameObject }
                };
                return true;
            }
            return false;
        }

        public static PlayerControllerB CheckForPlayersInLOS(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            Vector3 forward = turret.aimPoint.forward;
            forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / radius, 0f) * forward;
            float num = turret.rotationRange / radius * 2f;
            System.Type turretType = typeof(Turret);
            FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hit = turretType.GetField("hit", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo enteringBerserkMode = turretType.GetField("enteringBerserkMode", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i <= 6; i++)
            {
                shootRay.SetValue(turret, new Ray(turret.centerPoint.position, forward));

                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out RaycastHit targetHit, 30f, 1051400, QueryTriggerInteraction.Ignore))
                {
                    hit.SetValue(turret, targetHit);
                    if (((RaycastHit)hit.GetValue(turret)).transform.CompareTag("Player"))
                    {
                        PlayerControllerB component = ((RaycastHit)hit.GetValue(turret)).transform.GetComponent<PlayerControllerB>();
                        if (!(component == null))
                        {
                            if (angleRangeCheck && Vector3.Angle(component.transform.position + Vector3.up * 1.75f - turret.centerPoint.position, turret.forwardFacingPos.forward) > turret.rotationRange)
                            {
                                return null;
                            }

                            return component;
                        }

                        continue;
                    }

                    if ((turret.turretMode == TurretMode.Firing || (turret.turretMode == TurretMode.Berserk && !(bool)enteringBerserkMode.GetValue(turret)) && ((RaycastHit)hit.GetValue(turret)).transform.tag.StartsWith("PlayerRagdoll")))
                    {
                        Rigidbody component2 = ((RaycastHit)hit.GetValue(turret)).transform.GetComponent<Rigidbody>();
                        if (component2 != null)
                        {
                            component2.AddForce(forward.normalized * 42f, ForceMode.Impulse);
                        }
                    }
                }

                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }

            return null;
        }

        public static EnemyAICollisionDetect CheckForEnemiesInLineOfSight(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            System.Type turretType = typeof(Turret);
            FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hit = turretType.GetField("hit", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 forward = turret.aimPoint.forward;
            forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / radius, 0f) * forward;
            float num = turret.rotationRange / radius * 2f;
            for (int i = 0; i <= 6; i++)
            {
                shootRay.SetValue(turret, new Ray(turret.centerPoint.position, forward));
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out RaycastHit rayHit, 30f, Plugin.enemyMask, QueryTriggerInteraction.Ignore))
                {
                    hit.SetValue(turret, rayHit);
                    if (((RaycastHit)hit.GetValue(turret)).transform.GetComponent<EnemyAICollisionDetect>() != null)
                    {
                        EnemyAICollisionDetect component = ((RaycastHit)hit.GetValue(turret)).transform.GetComponent<EnemyAICollisionDetect>();
                        if (!(component == null))
                        {
                            if (angleRangeCheck && Vector3.Angle(component.transform.position + Vector3.up * 1.75f - turret.centerPoint.position, turret.forwardFacingPos.forward) > turret.rotationRange)
                            {
                                return null;
                            }

                            return component;
                        }

                        continue;
                    }
                }

                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }

            return null;
        }

        public static void SetTargetToEnemyBody(ref Turret __instance)
        {
            if (!(__instance == null))
            {
                System.Type typ = typeof(Turret);
                FieldInfo targetingDeadPlayer = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (__instance.targetPlayerWithRotation == null)
                {
                    FAIR_AI turret_ai = __instance.gameObject.GetComponent<FAIR_AI>();
                    if (turret_ai.targetWithRotation.GetComponent<EnemyAICollisionDetect>() != null)
                    {
                        EnemyAICollisionDetect ai = turret_ai.targetWithRotation.GetComponent<EnemyAICollisionDetect>();
                        if (ai.mainScript.isEnemyDead)
                        {
                            if (!(bool)targetingDeadPlayer.GetValue(__instance))
                            {
                                targetingDeadPlayer.SetValue(__instance, true);
                            }
                        }
                        else
                        {
                            targetingDeadPlayer.SetValue(__instance, false);
                            //__instance.targetingDeadPlayer = false;
                            __instance.targetTransform = turret_ai.targetWithRotation.transform;
                        }
                    }
                }
                else
                {
                    if (__instance.targetPlayerWithRotation.isPlayerDead)
                    {
                        if (!(bool)targetingDeadPlayer.GetValue(__instance))
                        {
                            targetingDeadPlayer.SetValue(__instance, true);
                        }

                        if (__instance.targetPlayerWithRotation.deadBody != null)
                        {
                            __instance.targetTransform = __instance.targetPlayerWithRotation.deadBody.bodyParts[5].transform;
                        }
                    }
                    else
                    {
                        targetingDeadPlayer.SetValue(__instance, false);
                        __instance.targetTransform = __instance.targetPlayerWithRotation.gameplayCamera.transform;
                    }
                }
            }
        }

        public static void TurnTowardsTargetEnemyIfHasLOS(ref Turret __instance)
        {
            FAIR_AI turret_ai = __instance.GetComponent<FAIR_AI>();
            System.Type typ = typeof(Turret);
            FieldInfo targetingDeadPlayer = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hasLineOfSight = typ.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo lostLOSTimer = typ.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.targetPlayerWithRotation == null)
            {
                bool flag = true;
                if ((bool)targetingDeadPlayer.GetValue(__instance) || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
                {
                    flag = false;
                }

                /*
                if (Physics.Linecast(turret.aimPoint.position, turret.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    flag = false;
                }
                */

                if (CheckForEnemiesInLineOfSight(__instance, 3) == null)
                {
                    flag = false;
                }

                if (flag)
                {
                    hasLineOfSight.SetValue(__instance, true);
                    lostLOSTimer.SetValue(__instance, 0f);
                    __instance.tempTransform.position = __instance.targetTransform.position;
                    __instance.tempTransform.position -= Vector3.up * 0.15f;
                    __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                    return;
                }

                if ((bool)hasLineOfSight.GetValue(__instance))
                {
                    hasLineOfSight.SetValue(__instance, false);
                    lostLOSTimer.SetValue(__instance, 0f);
                }

                if (!__instance.IsServer)
                {
                    return;
                }

                lostLOSTimer.SetValue(__instance, (float)lostLOSTimer.GetValue(__instance) + Time.deltaTime);
                if ((float)lostLOSTimer.GetValue(__instance) >= 2f)
                {
                    lostLOSTimer.SetValue(__instance, 0f);
                    Debug.Log("Turret: LOS timer ended on server. checking for new enemy target");
                    EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(__instance);
                    if (enemy != null)
                    {
                        turret_ai.targetWithRotation = enemy.mainScript;
                        turret_ai.SwitchedTargetedEnemyClientRpc(__instance, enemy.mainScript);
                        Debug.Log("Turret: Got new enemy target");
                    }
                    else
                    {
                        Debug.Log("Turret: No new enemy to target; returning to detection mode.");
                        turret_ai.targetWithRotation = null;
                        turret_ai.RemoveTargetedEnemyClientRpc();
                    }
                }
            }
            else
            {
                bool flag = true;
                if ((bool)targetingDeadPlayer.GetValue(__instance) || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
                {
                    flag = false;
                }

                if (Physics.Linecast(__instance.aimPoint.position, __instance.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    flag = false;
                }

                if (flag)
                {
                    hasLineOfSight.SetValue(__instance, true);
                    lostLOSTimer.SetValue(__instance, 0f);
                    __instance.tempTransform.position = __instance.targetTransform.position;
                    __instance.tempTransform.position -= Vector3.up * 0.15f;
                    __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                    return;
                }

                if ((bool)hasLineOfSight.GetValue(__instance))
                {
                    hasLineOfSight.SetValue(__instance, false);
                    lostLOSTimer.SetValue(__instance, 0f);
                }

                if (!__instance.IsServer)
                {
                    return;
                }

                lostLOSTimer.SetValue(__instance, (float)lostLOSTimer.GetValue(__instance) + Time.deltaTime);
                if ((float)lostLOSTimer.GetValue(__instance) >= 2f)
                {
                    lostLOSTimer.SetValue(__instance, 0f);
                    Debug.Log("Turret: LOS timer ended on server. checking for new player target");
                    PlayerControllerB playerControllerB = CheckForPlayersInLOS(__instance);
                    if (playerControllerB != null)
                    {
                        __instance.targetPlayerWithRotation = playerControllerB;
                        __instance.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId);
                        Debug.Log("Turret: Got new player target");
                    }
                    else
                    {
                        Debug.Log("Turret: No new player to target; returning to detection mode.");
                        __instance.targetPlayerWithRotation = null;
                        __instance.RemoveTargetedPlayerClientRpc();
                    }
                }
            }
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