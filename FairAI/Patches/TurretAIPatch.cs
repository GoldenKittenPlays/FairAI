using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FairAI;
using GameNetcodeStuff;
using UnityEngine;

internal class TurretAIPatch
{
    public static float viewRadius = 16f;

    public static float viewAngle = 90f;

    public static bool PatchUpdate(ref Turret __instance)
    {
        if (!(__instance == null) && Plugin.AllowFairness(__instance.transform.position))
        {
            FAIR_AI turret = (__instance).gameObject.GetComponent<FAIR_AI>() ?? (__instance).gameObject.AddComponent<FAIR_AI>();
            System.Type turretType = typeof(Turret);
            FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo SetTargetToPlayerBody = turretType.GetMethod("SetTargetToPlayerBody", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo TurnTowardsTargetIfHasLOS = turretType.GetMethod("TurnTowardsTargetIfHasLOS", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!__instance.turretActive)
            {
                wasTargetingPlayerLastFrame.SetValue(__instance, false);
                __instance.turretMode = TurretMode.Detection;
                __instance.targetPlayerWithRotation = null;
                turret.targetWithRotation = null;
                return false;
            }
            if (turret.targetWithRotation != null || __instance.targetPlayerWithRotation != null)
            {
                if (!(bool)wasTargetingPlayerLastFrame.GetValue(__instance))
                {
                    wasTargetingPlayerLastFrame.SetValue(__instance, true);
                    if (__instance.turretMode == TurretMode.Detection)
                    {
                        if (turret.targetWithRotation != null)
                        {
                            __instance.turretMode = TurretMode.Firing;
                        }
                        else
                        {
                            __instance.turretMode = TurretMode.Charging;
                        }
                    }
                }
                SetTargetToEnemyBody(ref __instance);
                TurnTowardsTargetEnemyIfHasLOS(ref __instance);
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
            if ((bool)rotatingClockwise.GetValue(__instance))
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, __instance.turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
                __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
                return false;
            }
            if ((bool)rotatingSmoothly.GetValue(__instance))
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(__instance.targetRotation, 0f - __instance.rotationRange, __instance.rotationRange), 180f);
            }
            __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
            return false;
        }
        return false;
    }

    public static void DetectionFunction(Turret turret, FAIR_AI turret_ai)
    {
        System.Type turretType = typeof(Turret);
        FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo switchRotationTimer = turretType.GetField("switchRotationTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingRight = turretType.GetField("rotatingRight", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo FadeBulletAudio = turretType.GetMethod("FadeBulletAudio", BindingFlags.Instance | BindingFlags.NonPublic);
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
            fadeBulletAudioCoroutine.SetValue(turret, turret.StartCoroutine((IEnumerator)FadeBulletAudio.Invoke(turret, new object[0])));
            turret.bulletParticles.Stop(true, (ParticleSystemStopBehavior)1);
            if (Plugin.turretSettings.Count > 7)
            {
                turret.rotationSpeed = Plugin.turretSettings[5];
            }
            else
            {
                turret.rotationSpeed = 28f;
            }
            rotatingSmoothly.SetValue(turret, true);
            turret.turretAnimator.SetInteger("TurretMode", 0);
            turretInterval.SetValue(turret, UnityEngine.Random.Range(0f, 0.15f));
        }
        if (!turret.IsServer)
        {
            return;
        }
        if (Plugin.turretSettings.Count > 7)
        {
            if ((float)switchRotationTimer.GetValue(turret) >= Plugin.turretSettings[3])
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
        }
        else
        {
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
        }
        if ((float)turretInterval.GetValue(turret) >= 0.25f)
        {
            turretInterval.SetValue(turret, 0f);
            EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(turret, 15f);
            if (enemy != null && !enemy.mainScript.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemy.mainScript.enemyType.enemyName))
            {
                turret_ai.targetWithRotation = enemy.mainScript;
                turret.turretMode = TurretMode.Firing;
                turret.SetToModeClientRpc(2);
                SwitchTurretMode.Invoke(turret, new object[1] { TurretMode.Firing });
                turret_ai.SwitchedTargetedEnemyClientRpc(turret, enemy.mainScript);
                Plugin.logger.LogInfo("Detected Enemy!");
                return;
            }
            turretInterval.SetValue(turret, 0f);
            PlayerControllerB playerControllerB = CheckForPlayersInLOS(turret, 1.35f, angleRangeCheck: true);
            if (playerControllerB != null && !playerControllerB.isPlayerDead)
            {
                turret.targetPlayerWithRotation = playerControllerB;
                SwitchTurretMode.Invoke(turret, new object[1] { 1 });
                turret.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, setModeToCharging: true);
                Plugin.logger.LogInfo("Detected Player!");
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
        FieldInfo hasLineOfSight = turretType.GetField("hasLineOfSight", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);
        if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Charging)
        {
            turretModeLastFrame.SetValue(turret, TurretMode.Charging);
            rotatingClockwise.SetValue(turret, false);
            turret.mainAudio.PlayOneShot(turret.detectPlayerSFX);
            turret.berserkAudio.Stop();
            WalkieTalkie.TransmitOneShotAudio(turret.mainAudio, turret.detectPlayerSFX);
            if (Plugin.turretSettings.Count > 7)
                turret.rotationSpeed = Plugin.turretSettings[6];
            else
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
                SwitchTurretMode.Invoke(turret, new object[1] { 2 });
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
        FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo targetingDeadPlayer = turretType.GetField("targetingDeadPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo hit = turretType.GetField("hit", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);
        if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Firing)
        {
            turretModeLastFrame.SetValue(turret, TurretMode.Firing);
            turret.berserkAudio.Stop();
            RoundManager.Instance.PlayAudibleNoise((turret.berserkAudio).transform.position, 15f, 0.9f);
            turret.mainAudio.clip = turret.firingSFX;
            turret.mainAudio.Play();
            turret.farAudio.clip = turret.firingFarSFX;
            turret.farAudio.Play();
            turret.bulletParticles.Play(true);
            turret.bulletCollisionAudio.Play();
            if (fadeBulletAudioCoroutine != null && fadeBulletAudioCoroutine.GetValue(turret) != null)
            {
                turret.StopCoroutine((Coroutine)fadeBulletAudioCoroutine.GetValue(turret));
            }
            turret.bulletCollisionAudio.volume = 1f;
            rotatingSmoothly.SetValue(turret, false);
            lostLOSTimer.SetValue(turret, 0f);
            turret.turretAnimator.SetInteger("TurretMode", 2);
        }
        if (Plugin.turretSettings.Count > 7)
        {
            if ((float)turretInterval.GetValue(turret) >= Plugin.turretSettings[1])
            {
                Plugin.logger.LogInfo("Attacking Target");
                turretInterval.SetValue(turret, 0f);
                if (CheckForPlayersInLOS(turret, 3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    int damage = (int)Plugin.turretSettings[0];
                    if (damage <= 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, false, true, (CauseOfDeath)0, 0, false, default);
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                    }
                    else
                    {
                        if (GameNetworkManager.Instance.localPlayerController.health > Plugin.turretSettings[0])
                        {
                            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                        }
                        else
                        {
                            GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                        }
                    }
                }
                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));
                RaycastHit rayHit = default(RaycastHit);
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
                {
                    hit.SetValue(turret, rayHit);
                    Transform transform = (turret.bulletCollisionAudio).transform;
                    Ray val = (Ray)shootRay.GetValue(turret);
                    RaycastHit val2 = (RaycastHit)hit.GetValue(turret);
                    Ray val3 = val;
                    transform.position = ((Ray)(val3)).GetPoint(val2.distance - 0.5f);
                }
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / 3f, 0f) * forward;
                Plugin.AttackTargets(turret.GetComponent<FAIR_AI>(), turret.aimPoint.position, forward, 30f);
                FAIR_AI ai = turret.GetComponent<FAIR_AI>();
                if (ai.targetWithRotation != null)
                {
                    EnemyAICollisionDetect detected = ai.targetWithRotation.GetComponent<EnemyAICollisionDetect>();
                    if (detected != null)
                    {
                        if (detected.mainScript.isEnemyDead || detected.mainScript.enemyHP <= 0)
                        {
                            ai.targetWithRotation = null;
                            ai.RemoveTargetedEnemyClientRpc();
                        }
                    }
                }
                if (turret.targetPlayerWithRotation != null)
                {
                    if (turret.targetPlayerWithRotation.isPlayerDead)
                    {
                        turret.targetPlayerWithRotation = null;
                        turret.RemoveTargetedPlayerClientRpc();
                    }
                }

                if (turret.targetPlayerWithRotation == null && ai.targetWithRotation == null)
                {
                    turret.turretMode = TurretMode.Detection;
                    turret.SetToModeClientRpc((int)TurretMode.Detection);
                    wasTargetingPlayerLastFrame.SetValue(turret, false);
                    targetingDeadPlayer.SetValue(turret, true);
                    turret.turretAnimator.SetInteger("TurretMode", 0);
                    SwitchTurretMode.Invoke(turret, new object[1] { TurretMode.Detection });
                }
                else
                {
                    if (turret.targetTransform != null)
                    {
                        if (turret.targetTransform.GetComponent<EnemyAI>() != null)
                        {
                            if (turret.targetTransform.GetComponent<EnemyAI>().isEnemyDead)
                            {
                                turret.turretMode = TurretMode.Detection;
                                turret.SetToModeClientRpc((int)TurretMode.Detection);
                                wasTargetingPlayerLastFrame.SetValue(turret, false);
                                targetingDeadPlayer.SetValue(turret, true);
                                turret.turretAnimator.SetInteger("TurretMode", 0);
                                SwitchTurretMode.Invoke(turret, new object[1] { TurretMode.Detection });
                            }
                        }
                        else if (turret.targetTransform.GetComponent<EnemyAICollisionDetect>())
                        {
                            if (turret.targetTransform.GetComponent<EnemyAICollisionDetect>().mainScript.isEnemyDead)
                            {
                                turret.turretMode = TurretMode.Detection;
                                turret.SetToModeClientRpc((int)TurretMode.Detection);
                                wasTargetingPlayerLastFrame.SetValue(turret, false);
                                targetingDeadPlayer.SetValue(turret, true);
                                turret.turretAnimator.SetInteger("TurretMode", 0);
                                SwitchTurretMode.Invoke(turret, new object[1] { TurretMode.Detection });
                            }
                        }
                    }
                }
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }
        else
        {
            if ((float)turretInterval.GetValue(turret) >= 0.21f)
            {
                Plugin.logger.LogInfo("Attacking Target");
                turretInterval.SetValue(turret, 0f);
                if (CheckForPlayersInLOS(turret, 3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    int damage = Plugin.GetInt("TurretConfig", "Player Damage");
                    if (damage <= 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, false, true, (CauseOfDeath)0, 0, false, default);
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                    }
                    else
                    {
                        if (GameNetworkManager.Instance.localPlayerController.health > 50)
                        {
                            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                        }
                        else
                        {
                            GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                        }
                    }
                }
                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));
                RaycastHit rayHit = default(RaycastHit);
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
                {
                    hit.SetValue(turret, rayHit);
                    Transform transform = (turret.bulletCollisionAudio).transform;
                    Ray val = (Ray)shootRay.GetValue(turret);
                    RaycastHit val2 = (RaycastHit)hit.GetValue(turret);
                    Ray val3 = val;
                    transform.position = ((Ray)(val3)).GetPoint(val2.distance - 0.5f);
                }
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / 3f, 0f) * forward;
                Plugin.AttackTargets(turret.GetComponent<FAIR_AI>(), turret.aimPoint.position, forward, 30f);
                FAIR_AI ai = turret.GetComponent<FAIR_AI>();
                if (ai.targetWithRotation != null)
                {
                    EnemyAICollisionDetect detected = ai.targetWithRotation.GetComponent<EnemyAICollisionDetect>();
                    if (detected != null)
                    {
                        if (detected.mainScript.isEnemyDead || detected.mainScript.enemyHP <= 0)
                        {
                            ai.targetWithRotation = null;
                            ai.RemoveTargetedEnemyClientRpc();
                        }
                    }
                }
                if (turret.targetPlayerWithRotation != null)
                {
                    if (turret.targetPlayerWithRotation.isPlayerDead)
                    {
                        turret.targetPlayerWithRotation = null;
                        turret.RemoveTargetedPlayerClientRpc();
                    }
                }

                if (turret.targetPlayerWithRotation == null && ai.targetWithRotation == null)
                {
                    turret.turretMode = TurretMode.Detection;
                    turret.SetToModeClientRpc((int)TurretMode.Detection);
                    wasTargetingPlayerLastFrame.SetValue(turret, false);
                    targetingDeadPlayer.SetValue(turret, true);
                    turret.turretAnimator.SetInteger("TurretMode", 0);
                    SwitchTurretMode.Invoke(turret, [TurretMode.Detection]);
                }
                else
                {
                    if (turret.targetTransform != null)
                    {
                        if (turret.targetTransform.GetComponent<EnemyAI>() != null)
                        {
                            if (turret.targetTransform.GetComponent<EnemyAI>().isEnemyDead)
                            {
                                turret.turretMode = TurretMode.Detection;
                                turret.SetToModeClientRpc((int)TurretMode.Detection);
                                wasTargetingPlayerLastFrame.SetValue(turret, false);
                                targetingDeadPlayer.SetValue(turret, true);
                                turret.turretAnimator.SetInteger("TurretMode", 0);
                                SwitchTurretMode.Invoke(turret, [TurretMode.Detection]);
                            }
                        }
                        else if (turret.targetTransform.GetComponent<EnemyAICollisionDetect>())
                        {
                            if (turret.targetTransform.GetComponent<EnemyAICollisionDetect>().mainScript.isEnemyDead)
                            {
                                turret.turretMode = TurretMode.Detection;
                                turret.SetToModeClientRpc((int)TurretMode.Detection);
                                wasTargetingPlayerLastFrame.SetValue(turret, false);
                                targetingDeadPlayer.SetValue(turret, true);
                                turret.turretAnimator.SetInteger("TurretMode", 0);
                                SwitchTurretMode.Invoke(turret, new object[1] { TurretMode.Detection });
                            }
                        }
                    }
                }
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }
    }

    public static void BerserkFunction(Turret turret)
    {
        System.Type turretType = typeof(Turret);
        FieldInfo wasTargetingPlayerLastFrame = turretType.GetField("wasTargetingPlayerLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretModeLastFrame = turretType.GetField("turretModeLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingClockwise = turretType.GetField("rotatingClockwise", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo fadeBulletAudioCoroutine = turretType.GetField("fadeBulletAudioCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rotatingSmoothly = turretType.GetField("rotatingSmoothly", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo turretInterval = turretType.GetField("turretInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo lostLOSTimer = turretType.GetField("lostLOSTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo hit = turretType.GetField("hit", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo berserkTimer = turretType.GetField("berserkTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo enteringBerserkMode = turretType.GetField("enteringBerserkMode", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo SwitchTurretMode = turretType.GetMethod("SwitchTurretMode", BindingFlags.Instance | BindingFlags.NonPublic);
        if ((TurretMode)turretModeLastFrame.GetValue(turret) != TurretMode.Berserk)
        {
            turretModeLastFrame.SetValue(turret, TurretMode.Berserk);
            turret.turretAnimator.SetInteger("TurretMode", 1);
            berserkTimer.SetValue(turret, 1.3f);
            turret.berserkAudio.Play();
            if (Plugin.turretSettings.Count > 7)
                turret.rotationSpeed = Plugin.turretSettings[7];
            else
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
                turret.bulletParticles.Play(true);
                turret.bulletCollisionAudio.Play();
                if (fadeBulletAudioCoroutine != null)
                {
                    turret.StopCoroutine((Coroutine)fadeBulletAudioCoroutine.GetValue(turret));
                }
                turret.bulletCollisionAudio.volume = 1f;
            }
            return;
        }
        if (Plugin.turretSettings.Count > 7)
        {
            if ((float)turretInterval.GetValue(turret) >= Plugin.turretSettings[1])
            {
                turretInterval.SetValue(turret, 0f);
                if (CheckForPlayersInLOS(turret, 3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    int damage = (int)Plugin.turretSettings[0];
                    if (damage <= 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, false, true, (CauseOfDeath)0, 0, false, default);
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                    }
                    else
                    {
                        if (GameNetworkManager.Instance.localPlayerController.health > damage)
                        {
                            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                        }
                        else
                        {
                            GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                        }
                    }
                }
                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));
                RaycastHit rayHit = default(RaycastHit);
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
                {
                    hit.SetValue(turret, rayHit);
                    Transform transform = (turret.bulletCollisionAudio).transform;
                    Ray val = (Ray)shootRay.GetValue(turret);
                    RaycastHit val2 = (RaycastHit)hit.GetValue(turret);
                    transform.position = ((Ray)(val)).GetPoint(val2.distance - 0.5f);
                }
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / 3f, 0f) * forward;
                Plugin.AttackTargets(turret.GetComponent<FAIR_AI>(), turret.centerPoint.position, forward, 30f);
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }
        else
        {
            if ((float)turretInterval.GetValue(turret) >= 0.21f)
            {
                turretInterval.SetValue(turret, 0f);
                if (CheckForPlayersInLOS(turret, 3f) == GameNetworkManager.Instance.localPlayerController)
                {
                    int damage = Plugin.GetInt("TurretConfig", "Player Damage");
                    if (damage <= 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, false, true, (CauseOfDeath)0, 0, false, default);
                        GameNetworkManager.Instance.localPlayerController.MakeCriticallyInjured(false);
                    }
                    else
                    {
                        if (GameNetworkManager.Instance.localPlayerController.health > 50)
                        {
                            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
                        }
                        else
                        {
                            GameNetworkManager.Instance.localPlayerController.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
                        }
                    }
                }
                shootRay.SetValue(turret, new Ray(turret.aimPoint.position, turret.aimPoint.forward));
                RaycastHit rayHit = default(RaycastHit);
                if (Physics.Raycast((Ray)shootRay.GetValue(turret), out rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
                {
                    hit.SetValue(turret, rayHit);
                    Transform transform = (turret.bulletCollisionAudio).transform;
                    Ray val = (Ray)shootRay.GetValue(turret);
                    RaycastHit val2 = (RaycastHit)hit.GetValue(turret);
                    transform.position = ((Ray)(val)).GetPoint(val2.distance - 0.5f);
                }
                Vector3 forward = turret.aimPoint.forward;
                forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / 3f, 0f) * forward;
                Plugin.AttackTargets(turret.GetComponent<FAIR_AI>(), turret.centerPoint.position, forward, 30f);
            }
            else
            {
                turretInterval.SetValue(turret, (float)turretInterval.GetValue(turret) + Time.deltaTime);
            }
        }
        if (turret.IsServer)
        {
            berserkTimer.SetValue(turret, (float)berserkTimer.GetValue(turret) - Time.deltaTime);
            if ((float)berserkTimer.GetValue(turret) <= 0f)
            {
                SwitchTurretMode.Invoke(turret, new object[1] { 0 });
                turret.SetToModeClientRpc(0);
            }
        }
    }

    public static bool CheckForTargetsInLOS(ref Turret __instance, float radius = 2f, bool angleRangeCheck = false)
    {
        FAIR_AI turret = (__instance).gameObject.GetComponent<FAIR_AI>();
        PlayerControllerB player = CheckForPlayersInLOS(__instance, radius, angleRangeCheck);
        EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(__instance, radius, angleRangeCheck);
        if (player != null)
        {
            turret.targets = new Dictionary<int, GameObject> {
            {
                0,
                (player).gameObject
            } };
            return true;
        }
        if (enemy != null)
        {
            turret.targets = new Dictionary<int, GameObject> {
            {
                1,
                (enemy).gameObject
            } };
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
        FieldInfo shootRay = turretType.GetField("shootRay", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo hit = turretType.GetField("hit", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo enteringBerserkMode = turretType.GetField("enteringBerserkMode", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i <= 6; i++)
        {
            shootRay.SetValue(turret, new Ray(turret.centerPoint.position, forward));
            if (Physics.Raycast((Ray)shootRay.GetValue(turret), out RaycastHit temp, 30f, 1051400, QueryTriggerInteraction.Ignore))
            {
                hit.SetValue(turret, temp);
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

                if ((turret.turretMode == TurretMode.Firing || (turret.turretMode == TurretMode.Berserk && !(bool)enteringBerserkMode.GetValue(turret))) && ((RaycastHit)hit.GetValue(turret)).transform.tag.StartsWith("PlayerRagdoll"))
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
        Vector3 forward = turret.aimPoint.forward;
        forward = Quaternion.Euler(0f, (float)(int)(0f - turret.rotationRange) / 3f, 0f) * forward;
        List<EnemyAICollisionDetect> enemies = Plugin.GetEnemyTargets(Plugin.GetTargets(turret.GetComponent<FAIR_AI>(), turret.aimPoint.position, forward, radius));
        if (enemies == null || !enemies.Any())
        {
            return null;
        }
        return enemies[0];
    }

    public static bool SetTargetToEnemyBody(ref Turret __instance)
    {
        if (!(__instance == null))
        {
            System.Type typ = typeof(Turret);
            FieldInfo targetingDeadPlayer = typ.GetField("targetingDeadPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            FAIR_AI turret_ai = (__instance).gameObject.GetComponent<FAIR_AI>();
            if (turret_ai.targetWithRotation != null)
            {
                if (!((turret_ai.targetWithRotation).GetComponent<EnemyAICollisionDetect>() != null))
                {
                    if (__instance.targetPlayerWithRotation != null)
                    {
                        if (__instance.targetPlayerWithRotation.isPlayerDead)
                        {
                            if (!(bool)targetingDeadPlayer.GetValue(__instance))
                            {
                                targetingDeadPlayer.SetValue(__instance, true);
                            }
                            if (__instance.targetPlayerWithRotation.deadBody != null)
                            {
                                __instance.targetTransform = (__instance.targetPlayerWithRotation.deadBody.bodyParts[5]).transform;
                            }
                        }
                        else
                        {
                            targetingDeadPlayer.SetValue(__instance, false);
                            __instance.targetTransform = (__instance.targetPlayerWithRotation.gameplayCamera).transform;
                        }
                    }
                    return false;
                }
                EnemyAICollisionDetect ai = (turret_ai.targetWithRotation).GetComponent<EnemyAICollisionDetect>();
                if (ai.mainScript.isEnemyDead)
                {
                    if (!(bool)targetingDeadPlayer.GetValue(__instance))
                    {
                        targetingDeadPlayer.SetValue(__instance, true);
                    }
                }
                else if (__instance.targetPlayerWithRotation == null)
                {
                    targetingDeadPlayer.SetValue(__instance, false);
                    __instance.targetTransform = (turret_ai.targetWithRotation).transform;
                }
            }
            else if (__instance.targetPlayerWithRotation != null)
            {
                if (__instance.targetPlayerWithRotation.isPlayerDead)
                {
                    if (!(bool)targetingDeadPlayer.GetValue(__instance))
                    {
                        targetingDeadPlayer.SetValue(__instance, true);
                    }
                    if (__instance.targetPlayerWithRotation.deadBody != null)
                    {
                        __instance.targetTransform = (__instance.targetPlayerWithRotation.deadBody.bodyParts[5]).transform;
                    }
                }
                else
                {
                    targetingDeadPlayer.SetValue(__instance, false);
                    __instance.targetTransform = (__instance.targetPlayerWithRotation.gameplayCamera).transform;
                }
            }
        }
        return false;
    }

    public static bool TurnTowardsTargetEnemyIfHasLOS(ref Turret __instance)
    {
        if (!(__instance == null))
        {
            FAIR_AI turret_ai = (__instance).GetComponent<FAIR_AI>();
            System.Type typ = typeof(Turret);
            FieldInfo targetingDeadPlayer = typ.GetField("targetingDeadPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo hasLineOfSight = typ.GetField("hasLineOfSight", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lostLOSTimer = typ.GetField("lostLOSTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            bool flag = true;
            if (__instance.targetTransform != null && __instance.centerPoint != null && __instance.forwardFacingPos != null)
            {
                Vector3 directionToTarget = __instance.targetTransform.position - __instance.centerPoint.position;
                float angleToTarget = Vector3.Angle(directionToTarget, __instance.forwardFacingPos.forward);
                if (angleToTarget > __instance.rotationRange)
                {
                    flag = false;
                }
            }
            EnemyAICollisionDetect eTarget = CheckForEnemiesInLineOfSight(__instance, 15f);
            if (eTarget == null)
            {
                flag = false;
            }
            if (flag)
            {
                __instance.targetTransform = (eTarget.mainScript).transform;
                hasLineOfSight.SetValue(__instance, true);
                lostLOSTimer.SetValue(__instance, 0f);
                __instance.tempTransform.position = __instance.targetTransform.position;
                Transform tempTransform = __instance.tempTransform;
                tempTransform.position -= Vector3.up * 0.15f;
                __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                return false;
            }
            bool pFlag = true;
            if ((bool)targetingDeadPlayer.GetValue(__instance) || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
            {
                pFlag = false;
            }
            if (Physics.Linecast(__instance.aimPoint.position, __instance.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
            {
                pFlag = false;
            }
            if (pFlag)
            {
                hasLineOfSight.SetValue(__instance, true);
                lostLOSTimer.SetValue(__instance, 0f);
                __instance.tempTransform.position = __instance.targetTransform.position;
                Transform tempTransform2 = __instance.tempTransform;
                tempTransform2.position -= Vector3.up * 0.15f;
                __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                return false;
            }
            if ((bool)hasLineOfSight.GetValue(__instance))
            {
                hasLineOfSight.SetValue(__instance, false);
                lostLOSTimer.SetValue(__instance, 0f);
            }
            if (!(__instance).IsServer)
            {
                return false;
            }
            lostLOSTimer.SetValue(__instance, (float)lostLOSTimer.GetValue(__instance) + Time.deltaTime);
            if ((float)lostLOSTimer.GetValue(__instance) >= 2f)
            {
                lostLOSTimer.SetValue(__instance, 0f);
                Plugin.logger.LogInfo("Turret: LOS timer ended on server. checking for new player target");
                PlayerControllerB playerControllerB = CheckForPlayersInLOS(__instance);
                if (playerControllerB != null)
                {
                    __instance.targetPlayerWithRotation = playerControllerB;
                    __instance.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId);
                    Plugin.logger.LogInfo("Turret: Got new player target");
                }
                else
                {
                    Plugin.logger.LogInfo("Turret: No new player to target; returning to detection mode.");
                    __instance.targetPlayerWithRotation = null;
                    __instance.RemoveTargetedPlayerClientRpc();
                }
            }
            if (__instance.targetPlayerWithRotation != null)
            {
                return false;
            }
            if ((bool)hasLineOfSight.GetValue(__instance))
            {
                hasLineOfSight.SetValue(__instance, false);
                lostLOSTimer.SetValue(__instance, 0f);
            }
            if (!(__instance).IsServer)
            {
                return false;
            }
            lostLOSTimer.SetValue(__instance, (float)lostLOSTimer.GetValue(__instance) + Time.deltaTime);
            if ((float)lostLOSTimer.GetValue(__instance) >= 2f)
            {
                lostLOSTimer.SetValue(__instance, 0f);
                Plugin.logger.LogInfo("Turret: LOS timer ended on server. checking for new enemy target");
                EnemyAICollisionDetect enemy = CheckForEnemiesInLineOfSight(__instance, 15f);
                if (enemy != null)
                {
                    turret_ai.targetWithRotation = enemy.mainScript;
                    turret_ai.SwitchedTargetedEnemyClientRpc(__instance, enemy.mainScript);
                    Plugin.logger.LogInfo("Turret: Got new enemy target");
                }
                else
                {
                    Plugin.logger.LogInfo("Turret: No new enemy to target; returning to detection mode.");
                    turret_ai.targetWithRotation = null;
                    turret_ai.RemoveTargetedEnemyClientRpc();
                }
            }
        }
        return false;
    }
}
