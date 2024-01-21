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
        public static float viewRadius = 10;
        public static float viewAngle = 90;

        public static void PatchStart(ref Turret __instance)
        {
            FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
            if (turret == null)
            {
                turret = __instance.gameObject.AddComponent<FAIR_AI>();
            }
        }
        public static bool PatchUpdate(ref Turret __instance)
        {
            FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
            System.Type typ = typeof(Turret);
            if (!__instance.turretActive)
            {
                FieldInfo turret_type = typ.GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                turret_type.SetValue(__instance, false);
                __instance.turretMode = TurretMode.Detection;
                __instance.targetPlayerWithRotation = null;
                turret.targetWithRotation = null;
                return false;
            }
            FieldInfo was_target_type = typ.GetField("wasTargetingPlayerLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = was_target_type.GetValue(__instance);
            if (__instance.targetPlayerWithRotation != null || turret.targetWithRotation != null)
            {
                if (!(bool)value)
                {
                    was_target_type.SetValue(__instance, true);
                    if (__instance.turretMode == TurretMode.Detection)
                    {
                        __instance.turretMode = TurretMode.Charging;
                    }
                }
                MethodInfo target_method = typ.GetMethod("SetTargetToPlayerBody", BindingFlags.NonPublic | BindingFlags.Instance);
                target_method.Invoke(__instance, null);
                MethodInfo los_method = typ.GetMethod("TurnTowardsTargetIfHasLOS", BindingFlags.NonPublic | BindingFlags.Instance);
                los_method.Invoke(__instance, null);
                //__instance.SetTargetToPlayerBody();
                //__instance.TurnTowardsTargetIfHasLOS();
            }
            else if (((bool)value))
            {
                was_target_type.SetValue(__instance, false);
                //__instance.wasTargetingPlayerLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
            }
            FieldInfo mode_last_type = typ.GetField("turretModeLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            var mode_last_value = mode_last_type.GetValue(__instance);
            FieldInfo rot_cw_type = typ.GetField("rotatingClockwise", BindingFlags.NonPublic | BindingFlags.Instance);
            var rot_cw_value = rot_cw_type.GetValue(__instance);
            FieldInfo fade_cor_type = typ.GetField("fadeBulletAudioCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            var fade_cor_value = fade_cor_type.GetValue(__instance);
            FieldInfo inter_type = typ.GetField("turretInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            var inter_value = inter_type.GetValue(__instance);
            FieldInfo rot_s_type = typ.GetField("rotatingSmoothly", BindingFlags.NonPublic | BindingFlags.Instance);
            var rot_s_value = rot_s_type.GetValue(__instance);
            FieldInfo rot_timer_type = typ.GetField("switchRotationTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var rot_timer_value = rot_timer_type.GetValue(__instance);
            FieldInfo rot_r_type = typ.GetField("rotatingRight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo has_los_type = typ.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
            var has_los_value = has_los_type.GetValue(__instance);
            FieldInfo los_timer_type = typ.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo ray_type = typ.GetField("shootRay", BindingFlags.NonPublic | BindingFlags.Instance);
            var ray_value = ray_type.GetValue(__instance);
            FieldInfo hit_type = typ.GetField("hit", BindingFlags.NonPublic | BindingFlags.Instance);
            var hit_value = hit_type.GetValue(__instance);
            FieldInfo ber_timer_type = typ.GetField("berserkTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var ber_timer_value = ber_timer_type.GetValue(__instance);
            FieldInfo ber_mode_type = typ.GetField("enteringBerserkMode", BindingFlags.NonPublic | BindingFlags.Instance);
            var ber_mode_value = ber_mode_type.GetValue(__instance);
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    mode_last_value = mode_last_type.GetValue(__instance);
                    if (((TurretMode)mode_last_value) != 0)
                    {
                        mode_last_type.SetValue(__instance, TurretMode.Detection);
                        rot_cw_type.SetValue(__instance, false);
                        //__instance.turretModeLastFrame = TurretMode.Detection;
                        //__instance.rotatingClockwise = false;
                        __instance.mainAudio.Stop();
                        __instance.farAudio.Stop();
                        __instance.berserkAudio.Stop();
                        fade_cor_value = fade_cor_type.GetValue(__instance);
                        if (((Coroutine)fade_cor_value) != null)
                        {
                            __instance.StopCoroutine(((Coroutine)fade_cor_value));
                        }
                        MethodInfo los_method = typ.GetMethod("FadeBulletAudio", BindingFlags.NonPublic | BindingFlags.Instance);
                        IEnumerator enu = (IEnumerator)los_method.Invoke(__instance, null);
                        fade_cor_type.SetValue(__instance, __instance.StartCoroutine(enu));
                        //__instance.fadeBulletAudioCoroutine = __instance.StartCoroutine(__instance.FadeBulletAudio());
                        __instance.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                        __instance.rotationSpeed = 28f;
                        //__instance.rotatingSmoothly = true;
                        rot_s_type.SetValue(__instance, true);
                        __instance.turretAnimator.SetInteger("TurretMode", 0);
                        inter_type.SetValue(__instance, UnityEngine.Random.Range(0f, 0.15f));
                        //__instance.turretInterval = UnityEngine.Random.Range(0f, 0.15f);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (((float)rot_timer_value) >= 7f)
                    {
                        rot_timer_type.SetValue(__instance, 0f);
                        rot_timer_value = rot_timer_type.GetValue(__instance);
                        //__instance.switchRotationTimer = 0f;
                        bool setRotateRight = !(bool)rot_r_type.GetValue(__instance);
                        __instance.SwitchRotationClientRpc(setRotateRight);
                        __instance.SwitchRotationOnInterval(setRotateRight);
                    }
                    else
                    {
                        rot_timer_type.SetValue(__instance, (((float)rot_timer_value) + Time.deltaTime));
                        //__instance.switchRotationTimer += Time.deltaTime;
                    }
                    inter_value = inter_type.GetValue(__instance);
                    if (((float)inter_value) >= 0.25f)
                    {
                        inter_type.SetValue(__instance, 0f);
                        //__instance.turretInterval = 0f;
                        PlayerControllerB playerControllerB = __instance.CheckForPlayersInLineOfSight(1.35f, angleRangeCheck: true);
                        List<EnemyAI> enemies = GetActualTargets(__instance, GetTargets(__instance));
                        if (playerControllerB != null && !playerControllerB.isPlayerDead)
                        {
                            __instance.targetPlayerWithRotation = playerControllerB;
                            MethodInfo mode_s_method = typ.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                            mode_s_method.Invoke(__instance, new object[] { 1 });
                            //__instance.SwitchTurretMode(1);
                            __instance.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, setModeToCharging: true);
                        }
                        else
                        {
                            if (enemies.Any())
                            {
                                __instance.targetPlayerWithRotation = null;
                                turret.targetWithRotation = enemies[0];
                                MethodInfo mode_s_method = typ.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                                mode_s_method.Invoke(__instance, new object[] { 1 });
                                //__instance.SwitchTurretMode(1);
                                turret.SwitchedTargetedEnemyClientRpc(__instance, enemies[0], setModeToCharging: true);
                            }
                        }
                    }
                    else
                    {
                        inter_value = inter_type.GetValue(__instance);
                        inter_type.SetValue(__instance, ((float)inter_value) + Time.deltaTime);
                        //__instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Charging:
                    mode_last_value = mode_last_type.GetValue(__instance);
                    if (((TurretMode)mode_last_value) != TurretMode.Charging)
                    {
                        mode_last_type.SetValue(__instance, TurretMode.Charging);
                        //__instance.turretModeLastFrame = TurretMode.Charging;
                        rot_cw_type.SetValue(__instance, false);
                        //__instance.rotatingClockwise = false;
                        __instance.mainAudio.PlayOneShot(__instance.detectPlayerSFX);
                        __instance.berserkAudio.Stop();
                        WalkieTalkie.TransmitOneShotAudio(__instance.mainAudio, __instance.detectPlayerSFX);
                        __instance.rotationSpeed = 95f;
                        rot_s_type.SetValue (__instance, false);
                        //__instance.rotatingSmoothly = false;
                        los_timer_type.SetValue (__instance, 0f);
                        //__instance.lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    inter_value = inter_type.GetValue(__instance);
                    if (((float)inter_value) >= 1.5f)
                    {
                        inter_type.SetValue(__instance, 0f);
                        //__instance.turretInterval = 0f;
                        Debug.Log("Charging timer is up, setting to firing mode");
                        if (!(bool)has_los_value)
                        {
                            Debug.Log("hasLineOfSight is false");
                            __instance.targetPlayerWithRotation = null;
                            turret.targetWithRotation = null;
                            __instance.RemoveTargetedPlayerClientRpc();
                            turret.RemoveTargetedEnemyClientRpc();
                        }
                        else
                        {
                            MethodInfo mode_s_method = typ.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                            mode_s_method.Invoke(__instance, new object[] { 2 });
                            //__instance.SwitchTurretMode(2);
                            __instance.SetToModeClientRpc(2);
                        }
                    }
                    else
                    {
                        inter_value = inter_type.GetValue(__instance);
                        inter_type.SetValue(__instance, ((float)inter_value) + Time.deltaTime);
                        //__instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Firing:
                    mode_last_value = mode_last_type.GetValue(__instance);
                    if (((TurretMode)mode_last_value) != TurretMode.Firing)
                    {
                        mode_last_type.SetValue(__instance, TurretMode.Firing);
                        //__instance.turretModeLastFrame = TurretMode.Firing;
                        __instance.berserkAudio.Stop();
                        __instance.mainAudio.clip = __instance.firingSFX;
                        __instance.mainAudio.Play();
                        __instance.farAudio.clip = __instance.firingFarSFX;
                        __instance.farAudio.Play();
                        __instance.bulletParticles.Play(withChildren: true);
                        __instance.bulletCollisionAudio.Play();
                        fade_cor_value = fade_cor_type.GetValue(__instance);
                        if (((Coroutine)fade_cor_value) != null)
                        {
                            __instance.StopCoroutine(((Coroutine)fade_cor_value));
                        }
                        __instance.bulletCollisionAudio.volume = 1f;
                        rot_s_type.SetValue(__instance, false);
                        //__instance.rotatingSmoothly = false;
                        los_timer_type.SetValue(__instance, 0f);
                        //__instance.lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 2);
                    }
                    inter_value = inter_type.GetValue(__instance);
                    if (((float)inter_value) >= 0.21f)
                    {
                        inter_type.SetValue(__instance, 0f);
                        Plugin.AttackTargets(__instance.aimPoint.position, __instance.aimPoint.forward, 30f);
                        ray_type.SetValue(__instance, new Ray(__instance.aimPoint.position, __instance.aimPoint.forward));
                        //__instance.shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(((Ray)ray_value), out RaycastHit rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            hit_type.SetValue(__instance, rayHit);
                            Ray shoot_ray = (Ray)ray_value;
                            hit_value = hit_type.GetValue(__instance);
                            __instance.bulletCollisionAudio.transform.position = shoot_ray.GetPoint(((RaycastHit)hit_value).distance - 0.5f);
                        }
                    }
                    else
                    {
                        inter_value = inter_type.GetValue(__instance);
                        inter_type.SetValue(__instance, ((float)inter_value) + Time.deltaTime);
                        //__instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Berserk:
                    mode_last_value = mode_last_type.GetValue(__instance);
                    if (((TurretMode)mode_last_value) != TurretMode.Berserk)
                    {
                        mode_last_type.SetValue(__instance, TurretMode.Berserk);
                        //__instance.turretModeLastFrame = TurretMode.Berserk;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                        ber_timer_type.SetValue(__instance, 1.3f);
                        //__instance.berserkTimer = 1.3f;
                        __instance.berserkAudio.Play();
                        __instance.rotationSpeed = 77f;
                        ber_mode_type.SetValue(__instance, true);
                        //__instance.enteringBerserkMode = true;
                        rot_s_type.SetValue(__instance, true);
                        //__instance.rotatingSmoothly = true;
                        los_timer_type.SetValue(__instance, 0f);
                        //__instance.lostLOSTimer = 0f;
                        was_target_type.SetValue(__instance, false);
                        //__instance.wasTargetingPlayerLastFrame = false;
                        __instance.targetPlayerWithRotation = null;
                        turret.targetWithRotation = null;
                    }
                    ber_mode_value = ber_mode_type.GetValue(__instance);
                    if (((bool)ber_mode_value))
                    {
                        ber_timer_value = ber_timer_type.GetValue(__instance);
                        ber_timer_type.SetValue(__instance, ((float)ber_timer_value) - Time.deltaTime);
                        //__instance.berserkTimer -= Time.deltaTime;
                        ber_timer_value = ber_timer_type.GetValue(__instance);
                        if (((float)ber_timer_value) <= 0f)
                        {
                            ber_mode_type.SetValue(__instance, false);
                            //__instance.enteringBerserkMode = false;
                            rot_cw_type.SetValue(__instance, true);
                            //__instance.rotatingClockwise = true;
                            ber_timer_type.SetValue (__instance, 9f);
                            //__instance.berserkTimer = 9f;
                            __instance.turretAnimator.SetInteger("TurretMode", 2);
                            __instance.mainAudio.clip = __instance.firingSFX;
                            __instance.mainAudio.Play();
                            __instance.farAudio.clip = __instance.firingFarSFX;
                            __instance.farAudio.Play();
                            __instance.bulletParticles.Play(withChildren: true);
                            __instance.bulletCollisionAudio.Play();
                            fade_cor_value = fade_cor_type.GetValue(__instance);
                            if (((Coroutine)fade_cor_value) != null)
                            {
                                __instance.StopCoroutine(((Coroutine)fade_cor_value));
                            }
                            __instance.bulletCollisionAudio.volume = 1f;
                        }
                        break;
                    }
                    inter_value = inter_type.GetValue(__instance);
                    if (((float)inter_value) >= 0.21f)
                    {
                        inter_type.SetValue(__instance, 0f);
                        //__instance.turretInterval = 0f;
                        Plugin.AttackTargets(__instance.aimPoint.position, __instance.aimPoint.forward, 30f);
                        ray_type.SetValue(__instance, new Ray(__instance.aimPoint.position, __instance.aimPoint.forward));
                        //__instance.shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        ray_value = ray_type.GetValue(__instance);
                        if (Physics.Raycast(((Ray)ray_value), out RaycastHit rayHit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            ray_value = ray_type.GetValue(__instance);
                            hit_type.SetValue(__instance, rayHit);
                            hit_value = ray_type.GetValue(__instance);
                            __instance.bulletCollisionAudio.transform.position = ((Ray)ray_value).GetPoint(((RaycastHit)hit_value).distance - 0.5f);
                        }
                    }
                    else
                    {
                        inter_value = inter_type.GetValue(__instance);
                        inter_type.SetValue(__instance, ((float)inter_value) - Time.deltaTime);
                        //__instance.turretInterval += Time.deltaTime;
                    }
                    if (__instance.IsServer)
                    {
                        ber_timer_value = ber_timer_type.GetValue(__instance);
                        ber_timer_type.SetValue(__instance, ((float)ber_timer_value) - Time.deltaTime);
                        //__instance.berserkTimer -= Time.deltaTime;
                        ber_timer_value = ber_timer_type.GetValue(__instance);
                        if (((float)ber_timer_value) <= 0f)
                        {
                            MethodInfo mode_s_method = typ.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                            mode_s_method.Invoke(__instance, new object[] { 0 });
                            //__instance.SwitchTurretMode(0);
                            __instance.SetToModeClientRpc(0);
                        }
                    }
                    break;
            }
            rot_cw_value = rot_cw_type.GetValue(__instance);
            if (((bool)rot_cw_value))
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, __instance.turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
                __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
                return false;
            }
            rot_s_value = rot_s_type.GetValue(__instance);
            if (((bool)rot_s_value))
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(__instance.targetRotation, 0f - __instance.rotationRange, __instance.rotationRange), 180f);
            }
            __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
            return false;
        }

        public static bool PatchSetTargetToPlayerBody(ref Turret __instance)
        {
            System.Type typ = typeof(Turret);
            FieldInfo target_dead_type = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            var target_dead_value = target_dead_type.GetValue(__instance);
            if (__instance.targetPlayerWithRotation != null)
            {
                if (__instance.targetPlayerWithRotation.isPlayerDead)
                {
                    target_dead_value = target_dead_type.GetValue(__instance);
                    if (!(bool)target_dead_value)
                    {
                        target_dead_type.SetValue(__instance, true);
                        //__instance.targetingDeadPlayer = true;
                    }
                    if (__instance.targetPlayerWithRotation.deadBody != null)
                    {
                        __instance.targetTransform = __instance.targetPlayerWithRotation.deadBody.bodyParts[5].transform;
                    }
                    FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                    if (turret.targetWithRotation != null)
                    {
                        target_dead_value = target_dead_type.GetValue(__instance);
                        if (!(bool)target_dead_value)
                        {
                            target_dead_type.SetValue (__instance, true);
                            //__instance.targetingDeadPlayer = true;
                        }
                        target_dead_value = target_dead_type.GetValue(__instance);
                        if (!turret.targetWithRotation.GetComponent<EnemyAI>().isEnemyDead)
                        {
                            target_dead_type.SetValue(__instance, false);
                            //__instance.targetingDeadPlayer = false;
                            __instance.targetTransform = turret.targetWithRotation.transform;
                        }
                    }
                }
                else
                {
                    target_dead_type.SetValue(__instance, false);
                    //__instance.targetingDeadPlayer = false;
                    __instance.targetTransform = __instance.targetPlayerWithRotation.gameplayCamera.transform;
                }
            }
            else
            {
                FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                if (turret.targetWithRotation != null)
                {
                    target_dead_value = target_dead_type.GetValue(__instance);
                    if (!(bool)target_dead_value)
                    {
                        target_dead_type.SetValue(__instance, true);
                        //__instance.targetingDeadPlayer = true;
                    }
                    target_dead_value = target_dead_type.GetValue(__instance);
                    if (!turret.targetWithRotation.GetComponent<EnemyAI>().isEnemyDead)
                    {
                        target_dead_type.SetValue(__instance, false);
                        //__instance.targetingDeadPlayer = false;
                        __instance.targetTransform = turret.targetWithRotation.transform;
                    }
                }
            }
            return false;
        }

        public static bool PatchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            if (TurnTowardsTargetEnemyIfHasLOS(__instance))
            {
                return false;
            }
            bool flag = true;
            System.Type typ = typeof(Turret);
            FieldInfo target_dead_type = typ.GetField("targetingDeadPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            var target_dead_value = target_dead_type.GetValue(__instance);
            FieldInfo has_los_type = typ.GetField("hasLineOfSight", BindingFlags.NonPublic | BindingFlags.Instance);
            var has_los_value = target_dead_type.GetValue(__instance);
            FieldInfo los_timer_type = typ.GetField("lostLOSTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var los_timer_value = target_dead_type.GetValue(__instance);
            if (((bool)target_dead_value) || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
            {
                flag = false;
            }
            if (Physics.Linecast(__instance.aimPoint.position, __instance.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                flag = false;
            }
            if (flag)
            {
                has_los_type.SetValue(__instance, true);
                //__instance.hasLineOfSight = true;
                los_timer_type.SetValue(__instance, 0f);
                //__instance.lostLOSTimer = 0f;
                __instance.tempTransform.position = __instance.targetTransform.position;
                __instance.tempTransform.position -= Vector3.up * 0.15f;
                __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                return false;
            }
            has_los_value = has_los_type.GetValue(__instance);
            if (((bool)has_los_value))
            {
                has_los_type.SetValue(__instance, false);
                los_timer_type.SetValue(__instance, 0f);
                //__instance.hasLineOfSight = false;
                //__instance.lostLOSTimer = 0f;
            }
            if (!__instance.IsServer)
            {
                return false;
            }
            los_timer_value = los_timer_type.GetValue(__instance);
            los_timer_type.SetValue(__instance, ((float)los_timer_value) + Time.deltaTime);
            //__instance.lostLOSTimer += Time.deltaTime;
            los_timer_value = los_timer_type.GetValue(__instance);
            if (((float)los_timer_value) >= 2f)
            {
                //__instance.lostLOSTimer = 0f;
                los_timer_type.SetValue(__instance, 0f);
                Debug.Log("Turret: LOS timer ended on server. checking for new player target");
                PlayerControllerB playerControllerB = __instance.CheckForPlayersInLineOfSight();
                List<EnemyAI> enemies = GetActualTargets(__instance, GetTargets(__instance));
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
            return false;
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
            List<EnemyAI> list = GetActualTargets(turret, GetTargets(turret));
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
                        ai.targetWithRotation = list[0];
                    }
                    turret.tempTransform.position = ai.targetWithRotation.transform.position;
                    turret.tempTransform.position -= Vector3.up * 0.15f;
                    turret.turnTowardsObjectCompass.LookAt(turret.tempTransform);
                }
                return flag;
            }

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
                return false;
            }

            //lostLOSTimer += Time.deltaTime;
            FAIR_AI aim = turret.gameObject.GetComponent<FAIR_AI>();
            List<EnemyAI> enemies = GetActualTargets(turret, GetTargets(turret));
            if (enemies.Any())
            {
                aim.targetWithRotation = enemies[0];
                aim.SwitchedTargetedEnemyClientRpc(turret, enemies[0]);
                return true;
            }
            else
            {
                aim.targetWithRotation = null;
                aim.RemoveTargetedEnemyClientRpc();
            }
            return false;
        }

        public static List<EnemyAI> GetActualTargets(Turret turret, List<EnemyAI> targets)
        {
            List<EnemyAI> enemies = new List<EnemyAI>();
            List<EnemyAI> newTargets = targets;
            if (newTargets != null)
            {
                newTargets.RemoveAll(t => t == null);
                if (newTargets.Any())
                {
                    foreach (EnemyAI target in newTargets)
                    {
                        if (target != null)
                        {
                            if ((!target.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.enemyType.enemyName)))
                            {
                                enemies.Add(target);
                            }
                        }
                    }
                    /*
                    newTargets.ForEach(target =>
                    {
                        EnemyAI enemy = target.GetComponent<EnemyAICollisionDetect>().mainScript;
                        if (enemy == null)
                        {
                            enemy = target.GetComponent<EnemyAI>();
                        }
                        if (enemy != null)
                        {
                            if ((!enemy.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemy.enemyType.enemyName)))
                            {
                                enemies.Add(enemy);
                            }
                        }
                    });
                    */
                }
            }
            return enemies;
        }

        static List<EnemyAI> GetTargets(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            List<Transform> targets = FindVisibleTargets(turret);
            List<EnemyAI> en = new List<EnemyAI>();
            if (targets.Any())
            {
                targets.ForEach(e =>
                {
                    EnemyAICollisionDetect enemy = e.GetComponent<EnemyAICollisionDetect>();
                    EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                    if (enemy != null)
                    {
                        enemyAI = enemy.mainScript;
                    }
                    if (enemyAI != null && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemyAI.enemyType.enemyName))
                    {
                        en.Add(enemyAI);
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
        /*
        public static void PatchUpdateBefore(ref Turret __instance)
        {
            FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
            if (turret == null)
            {
                turret = __instance.gameObject.AddComponent<FAIR_AI>();
            }
            if (turret != null)
            {
                if (!turret.hasChecked)
                {
                    if (__instance.turretModeLastFrame == TurretMode.Detection)
                    {
                        if (HasTargets(__instance))
                        {
                            __instance.turretMode = TurretMode.Charging;
                        }
                    }
                    else if (__instance.turretModeLastFrame == TurretMode.Charging)
                    {
                        if (HasTargets(__instance))
                        {
                            __instance.turretMode = TurretMode.Firing;
                        }
                    }
                    turret.StartCoroutine(turret.WaitForCheck(0.25f));
                }
                if (__instance.turretMode == TurretMode.Firing || __instance.turretMode == TurretMode.Berserk)
                {
                    if (!HitEnemies(__instance)
                        && (__instance.turretModeLastFrame == TurretMode.Firing || __instance.turretModeLastFrame == TurretMode.Berserk))
                    {
                        if (!HasTargets(__instance))
                        {
                            __instance.turretMode = TurretMode.Detection;
                        }
                    }

                    if (!HitEnemies(__instance)
                        && (__instance.turretModeLastFrame == TurretMode.Firing || __instance.turretModeLastFrame == TurretMode.Berserk))
                    {
                        __instance.turretMode = TurretMode.Detection;
                    }
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
        }

        [HarmonyPatch("TurnTowardsTargetIfHasLOS")]
        [HarmonyPrefix]
        public static void PatchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            FAIR_AI.TurretTarget enemies = GetActualTargets(__instance, GetTargets(__instance));
            if (HasTargets(__instance))
            {
                foreach (Transform enemy in enemies.GetAllTargets())
                {
                    __instance.hasLineOfSight = true;
                    __instance.lostLOSTimer = 0f;
                    __instance.tempTransform.position = enemy.transform.position;
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

        public static bool HasTargets(Turret turret)
        {
            if (GetActualTargets(turret, GetTargets(turret)).GetAllTargets().Any())
            {
                return true;
            }
            return false;
        }

        public static bool HitEnemies(Turret turret)
        {
            Vector3 direction = Quaternion.Euler(0f, (int)(0f - turret.rotationRange) / 2f, 0f) * turret.aimPoint.forward;
            Vector3 startPos = turret.forwardFacingPos.position;
            return FairAIUtilities.AttackTargets(startPos, direction, 30f);
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

        public static FAIR_AI.TurretTarget GetActualTargets(Turret turret, List<Transform> targets)
        {
            List<EnemyAI> enemies = new List<EnemyAI>();
            List<PlayerControllerB> players = new List<PlayerControllerB>();
            targets.ForEach(target => {
                EnemyAI enemy = target.GetComponent<EnemyAICollisionDetect>().mainScript;
                if (enemy == null)
                {
                    enemy = target.GetComponent<EnemyAI>();
                }
                if ((!enemy.isEnemyDead && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemy.enemyType.enemyName)) && enemy != null)
                {
                    enemies.Add(enemy);
                }
                if (target.GetComponent<PlayerControllerB>() != null)
                {
                    if (!target.GetComponent<PlayerControllerB>().isPlayerDead)
                    {
                        players.Add(target.GetComponent<PlayerControllerB>());
                    }
                }
            });
            turret.gameObject.GetComponent<FAIR_AI>().targets.SetPlayers(players);
            turret.gameObject.GetComponent<FAIR_AI>().targets.SetEnemies(enemies);
            return turret.gameObject.GetComponent<FAIR_AI>().targets;
        }

        static List<Transform> GetTargets(Turret turret, float radius = 2f, bool angleRangeCheck = false)
        {
            List<EnemyAICollisionDetect> enemies = new List<EnemyAICollisionDetect>();
            List<Transform> targets = FindVisibleTargets(turret);
            List<Transform> players = FindVisiblePlayerTargets(turret);
            List<PlayerControllerB> pl = new List<PlayerControllerB>();
            List<EnemyAI> en = new List<EnemyAI>();
            targets.ForEach(e => {
                EnemyAI enemy = e.GetComponent<EnemyAICollisionDetect>().mainScript;
                if (enemy == null)
                {
                    enemy = e.GetComponent<EnemyAI>();
                }
                if (enemy != null && Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", enemy.enemyType.enemyName))
                {
                    en.Add(enemy);
                }
            });
            players.ForEach(p => {
                pl.Add(p.GetComponent<PlayerControllerB>());
            });
            turret.gameObject.GetComponent<FAIR_AI>().targets.SetPlayers(pl);
            turret.gameObject.GetComponent<FAIR_AI>().targets.SetEnemies(en);
            return turret.gameObject.GetComponent<FAIR_AI>().targets.GetAllTargets();
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
            Collider[] targetsInViewRadius = Physics.OverlapSphere(turret.aimPoint.position, viewRadius, FairAIUtilities.enemyMask);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                Transform target = targetsInViewRadius[i].transform;
                Vector3 dirToTarget = (target.position - turret.aimPoint.position).normalized;
                if (Vector3.Angle(turret.aimPoint.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(turret.aimPoint.position, target.position);

                    if (!Physics.Raycast(turret.aimPoint.position, dirToTarget, dstToTarget, ~FairAIUtilities.enemyMask))
                    {
                        if (target.GetComponent<EnemyAICollisionDetect>() != null || target.GetComponent<EnemyAI>() != null)
                        {
                            if (Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.GetComponent<EnemyAICollisionDetect>().mainScript.enemyType.enemyName) ||
                                Plugin.CanMob("TurretTargetAllMobs", ".Turret Target", target.GetComponent<EnemyAI>().enemyType.enemyName))
                            {
                                targets.Add(target);
                            }
                        }
                    }
                }
            }
            return targets;
        }

        public static List<Transform> FindVisiblePlayerTargets(Turret turret)
        {
            Collider[] targetsInViewRadius = Physics.OverlapSphere(turret.aimPoint.position, viewRadius, StartOfRound.Instance.playersMask);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                Transform target = targetsInViewRadius[i].transform;
                Vector3 dirToTarget = (target.position - turret.aimPoint.position).normalized;
                if (Vector3.Angle(turret.aimPoint.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(turret.aimPoint.position, target.position);

                    if (!Physics.Raycast(turret.aimPoint.position, dirToTarget, dstToTarget, ~StartOfRound.Instance.playersMask))
                    {
                        if (target.GetComponent<PlayerControllerB>() != null)
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
            return targets;
        }
        */
    }
}
