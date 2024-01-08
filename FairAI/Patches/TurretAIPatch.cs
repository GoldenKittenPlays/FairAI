using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
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
            if (!__instance.turretActive)
            {
                __instance.wasTargetingPlayerLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
                __instance.targetPlayerWithRotation = null;
                turret.targetWithRotation = null;
                return false;
            }
            if (__instance.targetPlayerWithRotation != null || turret.targetWithRotation != null)
            {
                if (!__instance.wasTargetingPlayerLastFrame)
                {
                    __instance.wasTargetingPlayerLastFrame = true;
                    if (__instance.turretMode == TurretMode.Detection)
                    {
                        __instance.turretMode = TurretMode.Charging;
                    }
                }
                __instance.SetTargetToPlayerBody();
                __instance.TurnTowardsTargetIfHasLOS();
            }
            else if (__instance.wasTargetingPlayerLastFrame)
            {
                __instance.wasTargetingPlayerLastFrame = false;
                __instance.turretMode = TurretMode.Detection;
            }
            switch (__instance.turretMode)
            {
                case TurretMode.Detection:
                    if (__instance.turretModeLastFrame != 0)
                    {
                        __instance.turretModeLastFrame = TurretMode.Detection;
                        __instance.rotatingClockwise = false;
                        __instance.mainAudio.Stop();
                        __instance.farAudio.Stop();
                        __instance.berserkAudio.Stop();
                        if (__instance.fadeBulletAudioCoroutine != null)
                        {
                            __instance.StopCoroutine(__instance.fadeBulletAudioCoroutine);
                        }
                        __instance.fadeBulletAudioCoroutine = __instance.StartCoroutine(__instance.FadeBulletAudio());
                        __instance.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                        __instance.rotationSpeed = 28f;
                        __instance.rotatingSmoothly = true;
                        __instance.turretAnimator.SetInteger("TurretMode", 0);
                        __instance.turretInterval = UnityEngine.Random.Range(0f, 0.15f);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (__instance.switchRotationTimer >= 7f)
                    {
                        __instance.switchRotationTimer = 0f;
                        bool setRotateRight = !__instance.rotatingRight;
                        __instance.SwitchRotationClientRpc(setRotateRight);
                        __instance.SwitchRotationOnInterval(setRotateRight);
                    }
                    else
                    {
                        __instance.switchRotationTimer += Time.deltaTime;
                    }
                    if (__instance.turretInterval >= 0.25f)
                    {
                        __instance.turretInterval = 0f;
                        PlayerControllerB playerControllerB = __instance.CheckForPlayersInLineOfSight(1.35f, angleRangeCheck: true);
                        List<EnemyAI> enemies = GetActualTargets(__instance, GetTargets(__instance));
                        if (playerControllerB != null && !playerControllerB.isPlayerDead)
                        {
                            __instance.targetPlayerWithRotation = playerControllerB;
                            __instance.SwitchTurretMode(1);
                            __instance.SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, setModeToCharging: true);
                        }
                        else
                        {
                            if (enemies.Any())
                            {
                                __instance.targetPlayerWithRotation = null;
                                turret.targetWithRotation = enemies[0];
                                __instance.SwitchTurretMode(1);
                                turret.SwitchedTargetedEnemyClientRpc(__instance, enemies[0], setModeToCharging: true);
                            }
                        }
                    }
                    else
                    {
                        __instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Charging:
                    if (__instance.turretModeLastFrame != TurretMode.Charging)
                    {
                        __instance.turretModeLastFrame = TurretMode.Charging;
                        __instance.rotatingClockwise = false;
                        __instance.mainAudio.PlayOneShot(__instance.detectPlayerSFX);
                        __instance.berserkAudio.Stop();
                        WalkieTalkie.TransmitOneShotAudio(__instance.mainAudio, __instance.detectPlayerSFX);
                        __instance.rotationSpeed = 95f;
                        __instance.rotatingSmoothly = false;
                        __instance.lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                    }
                    if (!__instance.IsServer)
                    {
                        break;
                    }
                    if (__instance.turretInterval >= 1.5f)
                    {
                        __instance.turretInterval = 0f;
                        Debug.Log("Charging timer is up, setting to firing mode");
                        if (!__instance.hasLineOfSight)
                        {
                            Debug.Log("hasLineOfSight is false");
                            __instance.targetPlayerWithRotation = null;
                            turret.targetWithRotation = null;
                            __instance.RemoveTargetedPlayerClientRpc();
                            turret.RemoveTargetedEnemyClientRpc();
                        }
                        else
                        {
                            __instance.SwitchTurretMode(2);
                            __instance.SetToModeClientRpc(2);
                        }
                    }
                    else
                    {
                        __instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Firing:
                    if (__instance.turretModeLastFrame != TurretMode.Firing)
                    {
                        __instance.turretModeLastFrame = TurretMode.Firing;
                        __instance.berserkAudio.Stop();
                        __instance.mainAudio.clip = __instance.firingSFX;
                        __instance.mainAudio.Play();
                        __instance.farAudio.clip = __instance.firingFarSFX;
                        __instance.farAudio.Play();
                        __instance.bulletParticles.Play(withChildren: true);
                        __instance.bulletCollisionAudio.Play();
                        if (__instance.fadeBulletAudioCoroutine != null)
                        {
                            __instance.StopCoroutine(__instance.fadeBulletAudioCoroutine);
                        }
                        __instance.bulletCollisionAudio.volume = 1f;
                        __instance.rotatingSmoothly = false;
                        __instance.lostLOSTimer = 0f;
                        __instance.turretAnimator.SetInteger("TurretMode", 2);
                    }
                    if (__instance.turretInterval >= 0.21f)
                    {
                        __instance.turretInterval = 0f;
                        Plugin.AttackTargets(__instance.aimPoint.position, __instance.aimPoint.forward, 30f);
                        __instance.shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(__instance.shootRay, out __instance.hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            __instance.bulletCollisionAudio.transform.position = __instance.shootRay.GetPoint(__instance.hit.distance - 0.5f);
                        }
                    }
                    else
                    {
                        __instance.turretInterval += Time.deltaTime;
                    }
                    break;
                case TurretMode.Berserk:
                    if (__instance.turretModeLastFrame != TurretMode.Berserk)
                    {
                        __instance.turretModeLastFrame = TurretMode.Berserk;
                        __instance.turretAnimator.SetInteger("TurretMode", 1);
                        __instance.berserkTimer = 1.3f;
                        __instance.berserkAudio.Play();
                        __instance.rotationSpeed = 77f;
                        __instance.enteringBerserkMode = true;
                        __instance.rotatingSmoothly = true;
                        __instance.lostLOSTimer = 0f;
                        __instance.wasTargetingPlayerLastFrame = false;
                        __instance.targetPlayerWithRotation = null;
                        turret.targetWithRotation = null;
                    }
                    if (__instance.enteringBerserkMode)
                    {
                        __instance.berserkTimer -= Time.deltaTime;
                        if (__instance.berserkTimer <= 0f)
                        {
                            __instance.enteringBerserkMode = false;
                            __instance.rotatingClockwise = true;
                            __instance.berserkTimer = 9f;
                            __instance.turretAnimator.SetInteger("TurretMode", 2);
                            __instance.mainAudio.clip = __instance.firingSFX;
                            __instance.mainAudio.Play();
                            __instance.farAudio.clip = __instance.firingFarSFX;
                            __instance.farAudio.Play();
                            __instance.bulletParticles.Play(withChildren: true);
                            __instance.bulletCollisionAudio.Play();
                            if (__instance.fadeBulletAudioCoroutine != null)
                            {
                                __instance.StopCoroutine(__instance.fadeBulletAudioCoroutine);
                            }
                            __instance.bulletCollisionAudio.volume = 1f;
                        }
                        break;
                    }
                    if (__instance.turretInterval >= 0.21f)
                    {
                        __instance.turretInterval = 0f;
                        Plugin.AttackTargets(__instance.aimPoint.position, __instance.aimPoint.forward, 30f);
                        __instance.shootRay = new Ray(__instance.aimPoint.position, __instance.aimPoint.forward);
                        if (Physics.Raycast(__instance.shootRay, out __instance.hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                        {
                            __instance.bulletCollisionAudio.transform.position = __instance.shootRay.GetPoint(__instance.hit.distance - 0.5f);
                        }
                    }
                    else
                    {
                        __instance.turretInterval += Time.deltaTime;
                    }
                    if (__instance.IsServer)
                    {
                        __instance.berserkTimer -= Time.deltaTime;
                        if (__instance.berserkTimer <= 0f)
                        {
                            __instance.SwitchTurretMode(0);
                            __instance.SetToModeClientRpc(0);
                        }
                    }
                    break;
            }
            if (__instance.rotatingClockwise)
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, __instance.turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
                __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
                return false;
            }
            if (__instance.rotatingSmoothly)
            {
                __instance.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(__instance.targetRotation, 0f - __instance.rotationRange, __instance.rotationRange), 180f);
            }
            __instance.turretRod.rotation = Quaternion.RotateTowards(__instance.turretRod.rotation, __instance.turnTowardsObjectCompass.rotation, __instance.rotationSpeed * Time.deltaTime);
            return false;
        }

        public static bool PatchSetTargetToPlayerBody(ref Turret __instance)
        {
            if (__instance.targetPlayerWithRotation != null)
            {
                if (__instance.targetPlayerWithRotation.isPlayerDead)
                {
                    if (!__instance.targetingDeadPlayer)
                    {
                        __instance.targetingDeadPlayer = true;
                    }
                    if (__instance.targetPlayerWithRotation.deadBody != null)
                    {
                        __instance.targetTransform = __instance.targetPlayerWithRotation.deadBody.bodyParts[5].transform;
                    }
                    FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                    if (turret.targetWithRotation != null)
                    {
                        if (!__instance.targetingDeadPlayer)
                        {
                            __instance.targetingDeadPlayer = true;
                        }
                        if (!turret.targetWithRotation.GetComponent<EnemyAI>().isEnemyDead)
                        {
                            __instance.targetingDeadPlayer = false;
                            __instance.targetTransform = turret.targetWithRotation.transform;
                        }
                    }
                }
                else
                {
                    __instance.targetingDeadPlayer = false;
                    __instance.targetTransform = __instance.targetPlayerWithRotation.gameplayCamera.transform;
                }
            }
            else
            {
                FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                if (turret.targetWithRotation != null)
                {
                    if (!__instance.targetingDeadPlayer)
                    {
                        __instance.targetingDeadPlayer = true;
                    }
                    if (!turret.targetWithRotation.GetComponent<EnemyAI>().isEnemyDead)
                    {
                        __instance.targetingDeadPlayer = false;
                        __instance.targetTransform = turret.targetWithRotation.transform;
                    }
                }
            }
            return false;
        }

        public static bool PatchTurnTowardsTargetIfHasLOS(ref Turret __instance)
        {
            bool flag = true;
            if (__instance.targetingDeadPlayer || Vector3.Angle(__instance.targetTransform.position - __instance.centerPoint.position, __instance.forwardFacingPos.forward) > __instance.rotationRange)
            {
                flag = false;
            }
            if (Physics.Linecast(__instance.aimPoint.position, __instance.targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                flag = false;
            }
            if (flag)
            {
                __instance.hasLineOfSight = true;
                __instance.lostLOSTimer = 0f;
                __instance.tempTransform.position = __instance.targetTransform.position;
                __instance.tempTransform.position -= Vector3.up * 0.15f;
                __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
                return false;
            }
            if (__instance.hasLineOfSight)
            {
                __instance.hasLineOfSight = false;
                __instance.lostLOSTimer = 0f;
            }
            if (!__instance.IsServer)
            {
                return false;
            }
            __instance.lostLOSTimer += Time.deltaTime;
            if (__instance.lostLOSTimer >= 2f)
            {
                __instance.lostLOSTimer = 0f;
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
                    FAIR_AI turret = __instance.gameObject.GetComponent<FAIR_AI>();
                    if (enemies.Any())
                    {
                        turret.targetWithRotation = enemies[0];
                        turret.SwitchedTargetedEnemyClientRpc(__instance, enemies[0]);
                    }
                    else
                    {
                        turret.targetWithRotation = null;
                        turret.RemoveTargetedEnemyClientRpc();
                    }
                }
            }
            return false;
        }

        public static List<EnemyAI> GetActualTargets(Turret turret, List<EnemyAI> targets)
        {
            List<EnemyAI> enemies = new List<EnemyAI>();
            List<EnemyAI> newTargets = new List<EnemyAI>();
            newTargets.RemoveAll(t => t == null);
            if (newTargets.Any())
            {
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
