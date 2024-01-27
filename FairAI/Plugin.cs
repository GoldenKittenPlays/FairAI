using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FairAI.Patches;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace FairAI
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("Evaisa-LethalThings-0.9.4", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.FairAI", modName = "Fair AI", modVersion = "1.0.0";

        private Harmony harmony = new Harmony(modGUID);

        public static Plugin Instance;

        public static ManualLogSource logger;

        public static List<EnemyType> enemies;

        public static List<Item> items;

        public static bool playersEnteredInside = false;
        public static int wallsAndEnemyLayerMask = 524288;
        public static int enemyMask = (1 << 19);
        public static int allHittablesMask;
        static float onMeshThreshold = 3;

        private void Awake() 
        {
            if (Instance == null)
            {
                Instance = this;
            }
            harmony = new Harmony(modGUID);
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            harmony.PatchAll(typeof(Plugin));
            logger.LogInfo("Fair AI initiated!");
            CreateHarmonyPatch(harmony, typeof(RoundManager), "Start", null, typeof(RoundManagerPatch), nameof(RoundManagerPatch.PatchStart), false);
            CreateHarmonyPatch(harmony, typeof(StartOfRound), "Start", null, typeof(StartOfRoundPatch), nameof(StartOfRoundPatch.PatchStart), false);
            CreateHarmonyPatch(harmony, typeof(StartOfRound), "Update", null, typeof(StartOfRoundPatch), nameof(StartOfRoundPatch.PatchUpdate), false);
            CreateHarmonyPatch(harmony, typeof(Turret), "Start", null, typeof(TurretAIPatch), nameof(TurretAIPatch.PatchStart), false);
            CreateHarmonyPatch(harmony, typeof(Turret), "Update", null, typeof(TurretAIPatch), nameof(TurretAIPatch.PatchUpdate), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "SetTargetToPlayerBody", null, typeof(TurretAIPatch), nameof(TurretAIPatch.PatchSetTargetToPlayerBody), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "TurnTowardsTargetIfHasLOS", null, typeof(TurretAIPatch), nameof(TurretAIPatch.PatchTurnTowardsTargetIfHasLOS), true);
            CreateHarmonyPatch(harmony, typeof(Landmine), "SpawnExplosion", new[] { typeof(Vector3), typeof(bool), typeof(float), typeof(float) }, typeof(MineAIPatch), nameof(MineAIPatch.PatchSpawnExplosion), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "OnTriggerEnter", null, typeof(MineAIPatch), nameof(MineAIPatch.PatchOnTriggerEnter), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "OnTriggerExit", null, typeof(MineAIPatch), nameof(MineAIPatch.PatchOnTriggerExit), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "Detonate", null, typeof(MineAIPatch), nameof(MineAIPatch.DetonatePatch), false);
            if (FindType("LethalThings.RoombaAI") != null)
            {
                CreateHarmonyPatch(harmony, FindType("LethalThings.RoombaAI"), "Start", null, typeof(BoombaPatch), nameof(BoombaPatch.PatchStart), false);
                CreateHarmonyPatch(harmony, FindType("LethalThings.RoombaAI"), "DoAIInterval", null, typeof(BoombaPatch), nameof(BoombaPatch.PatchDoAIInterval), false);
            }
        }

        public static List<PlayerControllerB> GetActivePlayers()
        {
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            List<PlayerControllerB> list = new List<PlayerControllerB>();
            foreach (PlayerControllerB val in players)
            {
                if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && !val.isPlayerDead && ((Behaviour)val).isActiveAndEnabled && val.isPlayerControlled)
                {
                    list.Add(val);
                }
            }
            return list;
        }

        public static bool AllowFairness()
        {
            if (StartOfRound.Instance != null)
            {
                if (Can("CheckForPlayersInside"))
                {
                    logger.LogInfo("Players Inside?: " + playersEnteredInside.ToString());
                    return playersEnteredInside;
                }
            }
            logger.LogInfo("Players Inside Check Skipped!");
            return true;
        }

        public static bool CanMob(string parentIdentifier, string identifier, string mobName)
        {
            string mob = RemoveInvalidCharacters(mobName).ToUpper();
            if (Instance.Config[new ConfigDefinition("Mobs", parentIdentifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                foreach (ConfigDefinition entry in Instance.Config.Keys)
                {
                    if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(RemoveInvalidCharacters(mob + identifier.ToUpper())))
                    {
                        return Instance.Config[entry].BoxedValue.ToString().ToUpper().Equals("TRUE");
                    }
                }
                return false;
            }
            return false;
        }

        public static bool Can(string identifier)
        {
            foreach (ConfigDefinition entry in Instance.Config.Keys)
            {
                if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(RemoveInvalidCharacters(identifier.ToUpper())))
                {
                    return Instance.Config[entry].BoxedValue.ToString().ToUpper().Equals("TRUE");
                }
            }
            return false;
        }

        public static string RemoveWhitespaces(string source)
        {
            return string.Join("", source.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        public static string RemoveSpecialCharacters(string source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in source)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string RemoveInvalidCharacters(string source)
        {
            return RemoveWhitespaces(RemoveSpecialCharacters(source));
        }

        /// <summary>
        /// Looks in all loaded assemblies for the given type.
        /// </summary>
        /// <param name="fullName">
        /// The full name of the type.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/> found; null if not found.
        /// </returns>
        public static Type FindType(string fullName)
        {
            try
            {
                if (AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName.Equals(fullName)) != null)
                {
                    return AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName.Equals(fullName));
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public static void CreateHarmonyPatch(Harmony harmony, Type typeToPatch, string methodToPatch, Type[] parameters, Type patchType, string patchMethod, bool isPrefix)
        {
            if (typeToPatch == null || patchType == null)
            {
                logger.LogInfo("Type is either incorrect or does not exist!");
                return;
            }
            MethodInfo Method = AccessTools.Method(typeToPatch, methodToPatch, parameters, null);
            MethodInfo Patch_Method = AccessTools.Method(patchType, patchMethod, null, null);

            if (isPrefix)
            {
                harmony.Patch(Method, new HarmonyMethod(Patch_Method), null, null, null, null);
            }
            else
            {
                harmony.Patch(Method, null, new HarmonyMethod(Patch_Method), null, null, null);
            }
        }

        public static bool IsAgentOnNavMesh(GameObject agentObject)
        {
            Vector3 agentPosition = agentObject.transform.position;
            NavMeshHit hit;

            // Check for nearest point on navmesh to agent, within onMeshThreshold
            if (NavMesh.SamplePosition(agentPosition, out hit, onMeshThreshold, NavMesh.AllAreas))
            {
                // Check if the positions are vertically aligned
                if (Mathf.Approximately(agentPosition.x, hit.position.x)
                    && Mathf.Approximately(agentPosition.z, hit.position.z))
                {
                    // Lastly, check if object is below navmesh
                    return agentPosition.y >= hit.position.y;
                }
            }

            return false;
        }

        public static bool AttackTargets(Vector3 aimPoint, Vector3 forward, float range)
        {
            return HitTargets(GetTargets(aimPoint, forward, range), forward);
        }

        public static List<GameObject> GetTargets(Vector3 aimPoint, Vector3 forward, float range)
        {
            List<GameObject> targets = new List<GameObject>();
            Ray ray = new Ray(aimPoint, forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, range, allHittablesMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 end = aimPoint + forward * range;
            for (int j = 0; j < hits.Length; j++)
            {
                GameObject obj = hits[j].transform.gameObject;
                if (obj.TryGetComponent(out IHittable hittable))
                {
                    EnemyAI ai = null;
                    if (hittable is EnemyAICollisionDetect detect) ai = detect.mainScript;
                    if (ai != null)
                    {
                        if (ai.isEnemyDead || ai.enemyHP <= 0 || !ai.enemyType.canDie) continue; // skip dead things
                    }
                    if (hittable is PlayerControllerB) targets.Add(obj);
                    else if (ai != null) targets.Add(obj);
                    else continue; // enemy hit something else (webs?)
                    end = hits[j].point;
                    break;
                }
                else
                {
                    // precaution: hit enemy without hitting hittable (immune to shovels?)
                    if (hits[j].collider.TryGetComponent(out EnemyAI ai))
                    {
                        if (!ai.isEnemyDead && ai.enemyHP > 0 && ai.enemyType.canDie)
                        {
                            targets.Add(ai.gameObject);
                            end = hits[j].point;
                            break;
                        }
                        else continue;
                    }
                    end = hits[j].point;
                    break; // wall or other obstruction
                }
            }
            return targets;
            //VisualiseShot(shotgunPosition, end);
        }

        public static bool HitTargets(List<GameObject> targets, Vector3 forward)
        {
            bool hits = false;
            if (!targets.Any())
            {
                return hits;
            }
            else
            {
                targets.ForEach(t =>
                {
                    if (t != null)
                    {
                        if (t.GetComponent<PlayerControllerB>() != null)
                        {
                            PlayerControllerB player = t.GetComponent<PlayerControllerB>();
                            // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                            int damage = 20;
                            hits = true;
                            player.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, forward);
                        }
                        else if (t.GetComponent<EnemyAICollisionDetect>() != null)
                        {
                            EnemyAICollisionDetect enemy = t.GetComponent<EnemyAICollisionDetect>();
                            int damage = 1;
                            if (!enemy.mainScript.isEnemyDead && enemy.mainScript.IsOwner)
                            {
                                if (Plugin.CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.mainScript.enemyType.enemyName))
                                {
                                    if (enemy.mainScript is NutcrackerEnemyAI)
                                    {
                                        if (((NutcrackerEnemyAI)enemy.mainScript).currentBehaviourStateIndex > 0)
                                        {
                                            enemy.mainScript.HitEnemyOnLocalClient(damage);
                                            hits = true;
                                        }
                                    }
                                    else
                                    {
                                        enemy.mainScript.HitEnemyOnLocalClient(damage);
                                        hits = true;
                                    }
                                }
                                else
                                {
                                    //enemy.mainScript.HitEnemyOnLocalClient(damage);
                                    //hits = true;
                                }
                            }
                        }
                        else if (t.GetComponent<EnemyAI>() != null)
                        {
                            EnemyAI enemy = t.GetComponent<EnemyAI>();
                            if (enemy.IsOwner)
                            {
                                int damage = 1;
                                if (Plugin.CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.enemyType.enemyName))
                                {
                                    if (enemy is NutcrackerEnemyAI)
                                    {
                                        if (((NutcrackerEnemyAI)enemy).currentBehaviourStateIndex > 0)
                                        {
                                            enemy.HitEnemyOnLocalClient(damage);
                                            hits = true;
                                        }
                                    }
                                    else
                                    {
                                        enemy.HitEnemyOnLocalClient(damage);
                                        hits = true;
                                    }
                                }
                            }
                            else
                            {
                                //enemy.HitEnemyOnLocalClient(damage);
                                ///hits = true;
                            }
                        }
                        else if (t.GetComponent<IHittable>() != null)
                        {
                            IHittable hit = t.GetComponent<IHittable>();
                            if (hit is EnemyAICollisionDetect)
                            {
                                EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                                int damage = 1;
                                if (enemy.mainScript.IsOwner)
                                {
                                    if (Plugin.CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.mainScript.enemyType.enemyName))
                                    {
                                        if (enemy.mainScript is NutcrackerEnemyAI)
                                        {
                                            if (((NutcrackerEnemyAI)enemy.mainScript).currentBehaviourStateIndex > 0)
                                            {
                                                enemy.mainScript.HitEnemyOnLocalClient(damage);
                                                hits = true;
                                            }
                                        }
                                        else
                                        {
                                            enemy.mainScript.HitEnemyOnLocalClient(damage);
                                            hits = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //enemy.mainScript.HitEnemyOnLocalClient(damage);
                                    ///hits = true;
                                }
                            }
                            else if (hit is PlayerControllerB)
                            {
                                PlayerControllerB player = (PlayerControllerB)hit;
                                // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                                int damage = 33;
                                hits = true;
                                player.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, forward);
                            }
                            else
                            {
                                hit.Hit(1, forward, null, true);
                                hits = true;
                            }
                        }
                    }
                });
            }
            return hits;
        }
    }
}
