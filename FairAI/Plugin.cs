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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace FairAI
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(ltModID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(surfacedModID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.FairAI", modName = "Fair AI", modVersion = "1.5.1";

        private Harmony harmony = new Harmony(modGUID);

        public static Plugin Instance;

        public static ManualLogSource logger;

        public static List<EnemyType> enemies;

        public static List<Item> items;

        public static List<EnemyType> enemyList;

        public static List<Item> itemList;

        public static List<float> turretSettings;

        public static Dictionary<string, float[]> speeds;

        public static Dictionary<int, Vector3> positions;

        public static Dictionary<int, float> sinkingValues;

        public static Assembly surfacedAssembly;

        public static Assembly turretSettingsAssembly;

        public static PropertyInfo tsBoundConfig;

        public const string ltModID = "evaisa.lethalthings";
        public const string surfacedModID = "Surfaced";

        public static bool playersEnteredInside = false;
        public static bool surfacedEnabled = false;
        public static bool lethalThingsEnabled = false;
        public static bool turretSettingsEnabled = false;
        public static bool lethalConfigEnabled = false;
        public static bool roundHasStarted = false;

        public static int wallsAndEnemyLayerMask = 524288;
        public static int enemyMask = (1 << 19);
        public static int allHittablesMask;

        static float onMeshThreshold = 3;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            enemies = new List<EnemyType>();
            items = new List<Item>();
            itemList = new List<Item>();
            enemyList = new List<EnemyType>();
            speeds = new Dictionary<string, float[]>();
            positions = new Dictionary<int, Vector3>();
            turretSettings = new List<float>();
            sinkingValues = new Dictionary<int, float>();
            surfacedAssembly = null;
            harmony = new Harmony(modGUID);
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            harmony.PatchAll(typeof(Plugin));
            CreateHarmonyPatch(harmony, typeof(RoundManager), "Start", null, typeof(RoundManagerPatch), nameof(RoundManagerPatch.PatchStart), false);
            CreateHarmonyPatch(harmony, typeof(StartOfRound), "Start", null, typeof(StartOfRoundPatch), nameof(StartOfRoundPatch.PatchStart), false);
            CreateHarmonyPatch(harmony, typeof(StartOfRound), "Update", null, typeof(StartOfRoundPatch), nameof(StartOfRoundPatch.PatchUpdate), false);
            CreateHarmonyPatch(harmony, typeof(QuicksandTrigger), "OnTriggerStay", new[] { typeof(Collider) }, typeof(QuickSandPatch), nameof(QuickSandPatch.OnTriggerStayPatch), true);
            CreateHarmonyPatch(harmony, typeof(QuicksandTrigger), "OnTriggerExit", new[] { typeof(Collider)}, typeof(QuickSandPatch), nameof(QuickSandPatch.OnTriggerExitPatch), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "Update", null, typeof(TurretAIPatch), nameof(TurretAIPatch.PatchUpdate), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "CheckForPlayersInLineOfSight", new[] { typeof(float), typeof(bool) }, typeof(TurretAIPatch), nameof(TurretAIPatch.CheckForTargetsInLOS), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "SetTargetToPlayerBody", null, typeof(TurretAIPatch), nameof(TurretAIPatch.SetTargetToEnemyBody), true);
            CreateHarmonyPatch(harmony, typeof(Turret), "TurnTowardsTargetIfHasLOS", null, typeof(TurretAIPatch), nameof(TurretAIPatch.TurnTowardsTargetEnemyIfHasLOS), true);
            CreateHarmonyPatch(harmony, typeof(Landmine), "SpawnExplosion", new[] { typeof(Vector3), typeof(bool), typeof(float), typeof(float), typeof(int), typeof(float), typeof(GameObject), typeof(bool) }, typeof(MineAIPatch), nameof(MineAIPatch.PatchSpawnExplosion), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "OnTriggerEnter", null, typeof(MineAIPatch), nameof(MineAIPatch.PatchOnTriggerEnter), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "OnTriggerExit", null, typeof(MineAIPatch), nameof(MineAIPatch.PatchOnTriggerExit), false);
            CreateHarmonyPatch(harmony, typeof(Landmine), "Detonate", null, typeof(MineAIPatch), nameof(MineAIPatch.DetonatePatch), false);
            await WaitForProcess(1);
            GetTurretSettings();
            logger.LogInfo("Fair AI initiated!");
        }

        public static void ImmortalAffected()
        {
            if(!GetBool("General", "ImmortalAffected"))
            {
                enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null && e.canDie).ToList();
            }
        }

        public static float[] SpeedAndAccelerationEnemyList(EnemyAICollisionDetect enemy)
        {
            if (enemy != null)
            {
                EnemyAI ai = enemy.mainScript;
                if (enemy.mainScript != null)
                {
                    EnemyType eType = ai.enemyType;
                    string name = eType.enemyName;
                    if (eType != null)
                    {
                        if (GetSpeeds(enemy) == null)
                        {
                            NavMeshAgent agent = ai.GetComponentInChildren<NavMeshAgent>();
                            if (agent != null)
                            {
                                speeds.Add(eType.enemyName, new float[] { agent.speed, agent.acceleration });
                                return speeds[eType.enemyName];
                            }
                            else
                            {
                                speeds.Add(eType.enemyName, new float[] { 1, 1 });
                                return speeds[eType.enemyName];
                            }
                        }
                        else
                        {
                            return speeds[eType.enemyName];
                        }
                    }
                }
                else
                {
                    logger.LogWarning("No EnemyAI To Get Speeds!");
                }
            }
            else
            {
                logger.LogWarning("No EnemyCollision To Get Speeds!");
            }
            return new float[] { 1, 1 };
        }

        public static float[] GetSpeeds(EnemyAICollisionDetect enemy)
        {
            EnemyType eType = enemy.mainScript.enemyType;
            if (speeds.TryGetValue(eType.enemyName, out float[] values))
            {
                return values;
            }
            else
            {
                logger.LogWarning($"Speeds missing on: {eType.enemyName}");
                return null;
            }
        }

        public static void GetTurretSettings()
        {
            if (turretSettingsEnabled)
            {
                System.Object bc = tsBoundConfig.GetValue(null);
                System.Type bcType = null;
                if (bc != null)
                {
                    bcType = bc.GetType();
                    if (bcType == null)
                    {
                        logger.LogInfo("Bound Config Type not gotten!");
                    }
                }
                else
                {
                    logger.LogInfo("Unable to get BoundConfig");
                }
                FieldInfo turretDamageField = bcType.GetField("turretDamage", BindingFlags.Instance | BindingFlags.Public);

                if (turretDamageField != null)
                {
                    object turretDamageEntry = turretDamageField.GetValue(bc);
                    PropertyInfo valueProperty = turretDamageEntry?.GetType().GetProperty("Value");
                    float turretDamage = (int)valueProperty?.GetValue(turretDamageEntry);
                    turretSettings.Add(turretDamage);
                }
                FieldInfo turretFireRateField = bcType.GetField("turretFireRate", BindingFlags.Instance | BindingFlags.Public);

                if (turretFireRateField != null)
                {
                    object turretFireRateEntry = turretFireRateField.GetValue(bc);
                    PropertyInfo valueProperty = turretFireRateEntry?.GetType().GetProperty("Value");
                    float turretFireRate = (float)valueProperty?.GetValue(turretFireRateEntry);
                    turretSettings.Add(turretFireRate);
                }

                FieldInfo turretWarmupTimeField = bcType.GetField("turretWarmupTime", BindingFlags.Instance | BindingFlags.Public);

                if (turretWarmupTimeField != null)
                {
                    object turretWarmupTimeEntry = turretWarmupTimeField.GetValue(bc);
                    PropertyInfo valueProperty = turretWarmupTimeEntry?.GetType().GetProperty("Value");
                    float turretWarmupTime = (float)valueProperty?.GetValue(turretWarmupTimeEntry);
                    turretSettings.Add(turretWarmupTime);
                }

                FieldInfo turretRotateTimerField = bcType.GetField("turretRotateTimer", BindingFlags.Instance | BindingFlags.Public);

                if (turretRotateTimerField != null)
                {
                    object turretRotateTimerEntry = turretRotateTimerField.GetValue(bc);
                    PropertyInfo valueProperty = turretRotateTimerEntry?.GetType().GetProperty("Value");
                    float turretRotateTimer = (float)valueProperty?.GetValue(turretRotateTimerEntry);
                    turretSettings.Add(turretRotateTimer);
                }

                FieldInfo turretRotationRangeField = bcType.GetField("turretRotationRange", BindingFlags.Instance | BindingFlags.Public);

                if (turretRotationRangeField != null)
                {
                    object turretRotationRangeEntry = turretRotationRangeField.GetValue(bc);
                    PropertyInfo valueProperty = turretRotationRangeEntry?.GetType().GetProperty("Value");
                    float turretRotationRange = (float)valueProperty?.GetValue(turretRotationRangeEntry);
                    turretSettings.Add(turretRotationRange);
                }

                FieldInfo turretIdleRotationSpeedField = bcType.GetField("turretIdleRotationSpeed", BindingFlags.Instance | BindingFlags.Public);

                if (turretIdleRotationSpeedField != null)
                {
                    object turretIdleRotationSpeedEntry = turretIdleRotationSpeedField.GetValue(bc);
                    PropertyInfo valueProperty = turretIdleRotationSpeedEntry?.GetType().GetProperty("Value");
                    float turretIdleRotationSpeed = (float)valueProperty?.GetValue(turretIdleRotationSpeedEntry);
                    turretSettings.Add(turretIdleRotationSpeed);
                }

                FieldInfo turretFiringRotationSpeedField = bcType.GetField("turretFiringRotationSpeed", BindingFlags.Instance | BindingFlags.Public);

                if (turretFiringRotationSpeedField != null)
                {
                    object turretFiringRotationSpeedEntry = turretFiringRotationSpeedField.GetValue(bc);
                    PropertyInfo valueProperty = turretFiringRotationSpeedEntry?.GetType().GetProperty("Value");
                    float turretFiringRotationSpeed = (float)valueProperty?.GetValue(turretFiringRotationSpeedEntry);
                    turretSettings.Add(turretFiringRotationSpeed);
                }

                FieldInfo turretBerzerkRotationSpeedField = bcType.GetField("turretBerzerkRotationSpeed", BindingFlags.Instance | BindingFlags.Public);

                if (turretBerzerkRotationSpeedField != null)
                {
                    object turretBerzerkRotationSpeedEntry = turretBerzerkRotationSpeedField.GetValue(bc);
                    PropertyInfo valueProperty = turretBerzerkRotationSpeedEntry?.GetType().GetProperty("Value");
                    float turretBerzerkRotationSpeed = (float)valueProperty?.GetValue(turretBerzerkRotationSpeedEntry);
                    turretSettings.Add(turretBerzerkRotationSpeed);
                }
                logger.LogInfo("Got Turret Settings: " + turretSettings.Count);
            }
            else
            {
                turretSettings = new List<float>();
            }
        }

        public static async Task<IEnumerable<int>> WaitForProcess(int waitTime)
        {
            await Task.Delay(waitTime);
            bool done = false;
            while (!done)
            {
                await Instance.DelayedInitialization();
                done = true;
            }
            return new List<int>() { };
        }

        private async Task DelayedInitialization()
        {
            await Task.Run(() =>
            {
                TryLoadLethalThings();
                TryLoadSurfaced();
                TryLoadTurretSettings();
                TryLoadLethalConfig();
                logger.LogInfo("Optional Components initiated!");
            });
        }

        private void TryLoadLethalThings()
        {
            try
            {
                // Get all loaded assemblies
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly lethalThingsAssembly = null;

                // Find the LethalThings assembly
                foreach (var assembly in loadedAssemblies)
                {
                    if (assembly.GetName().Name == "LethalThings")
                    {
                        lethalThingsAssembly = assembly;
                        break;
                    }
                }

                if (lethalThingsAssembly != null)
                {
                    Type lethalThingsType = lethalThingsAssembly.GetType("LethalThings.RoombaAI");

                    if (lethalThingsType != null)
                    {
                        if (BoombaPatch.enabled)
                        {
                            CreateHarmonyPatch(harmony, lethalThingsType, "Start", null, typeof(BoombaPatch), nameof(BoombaPatch.PatchStart), false);
                            CreateHarmonyPatch(harmony, lethalThingsType, "DoAIInterval", null, typeof(BoombaPatch), nameof(BoombaPatch.PatchDoAIInterval), false);
                            lethalThingsEnabled = true;
                            logger.LogInfo("LethalThings Component Initiated!");
                        }
                    }
                }
                else
                {
                    logger.LogWarning("LethalThings assembly not found. Skipping optional patch.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"An error occurred while trying to apply patches for LethalThings: {e.Message}");
            }
        }

        private void TryLoadSurfaced()
        {
            try
            {
                // Get all loaded assemblies
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                // Find the LethalThings assembly
                foreach (var assembly in loadedAssemblies)
                {
                    if (assembly.GetName().Name == "Surfaced")
                    {
                        surfacedAssembly = assembly;
                        break;
                    }
                }
                if (surfacedAssembly != null)
                {
                    Type surfacedType = surfacedAssembly.GetType("Seamine");
                    Type berthaType = surfacedAssembly.GetType("Bertha");
                    if (surfacedType != null)
                    {
                        if (SurfacedMinePatch.enabled)
                        {
                            CreateHarmonyPatch(harmony, surfacedType, "OnTriggerEnter", new[] { typeof(Collider) }, typeof(SurfacedMinePatch), nameof(SurfacedMinePatch.PatchOnTriggerEnter), false);
                            surfacedEnabled = true;
                            logger.LogInfo("Surfaced Component Seamine Initiated!");
                        }
                    }
                    else
                    {
                        logger.LogInfo("Surfaced Component Seamine Not Found!");
                    }

                    if (berthaType != null)
                    {
                        if (SurfacedMinePatch.enabled)
                        {
                            CreateHarmonyPatch(harmony, berthaType, "OnTriggerEnter", new[] { typeof(Collider) }, typeof(SurfacedMinePatch), nameof(SurfacedMinePatch.PatchBerthaOnTriggerEnter), false);
                            surfacedEnabled = true;
                            logger.LogInfo("Surfaced Component Bertha Initiated!");
                        }
                    }
                    else
                    {
                        logger.LogInfo("Surfaced Component Bertha Not Found!");
                    }
                }
                else
                {
                    logger.LogWarning("Surfaced assembly not found. Skipping optional patch.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"An error occurred while trying to apply patches for Surfaced: {e.Message}");
            }
        }

        private void TryLoadTurretSettings()
        {
            try
            {
                Assembly tsAssembly = Assembly.Load("TheNameIsTyler.TurretSettings");
                if (tsAssembly != null)
                {
                    turretSettingsAssembly = tsAssembly;
                    Type turretSettingsType = turretSettingsAssembly.GetType("TurretSettings.TurretSettings");
                    if (turretSettingsType != null)
                    {
                        PropertyInfo boundConfigField = turretSettingsType.GetProperty("BoundConfig", BindingFlags.Static | BindingFlags.NonPublic);
                        if (boundConfigField != null)
                        {
                            tsBoundConfig = boundConfigField;
                            turretSettingsEnabled = true;
                            logger.LogInfo("TurretSettings Component Initiated!");
                        }
                        else
                        {
                            logger.LogWarning("TurretSettings config not found. Skipping optional patch.");
                        }
                    }
                    else
                    {
                        logger.LogWarning("TurretSettings type not found. Skipping optional patch.");
                    }
                }
                else
                {
                    logger.LogWarning("TurretSettings assembly not found. Skipping optional patch.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"An error occurred while trying to apply patches for TurretSettings: {e.Message}");
            }
        }

        private void TryLoadLethalConfig()
        {
            try
            {
                // Get all loaded assemblies
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly lethalConfigAssembly = null;
                // Find the LethalThings assembly
                foreach (var assembly in loadedAssemblies)
                {
                    if (assembly.GetName().Name == "LethalConfig")
                    {
                        lethalConfigAssembly = assembly;
                        break;
                    }
                }

                if (lethalConfigAssembly != null)
                {
                    Type lethalConfigType = lethalConfigAssembly.GetType("LethalConfig.LethalConfigManager");

                    if (lethalConfigType != null)
                    {
                        lethalConfigEnabled = true;
                        logger.LogInfo("LethalConfig Component Initiated!");
                    }
                }
                else
                {
                    logger.LogWarning("LethalConfig assembly not found. Skipping optional patch.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"An error occurred while trying to apply patches for LethalConfig: {e.Message}");
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

        public static bool AllowFairness(Vector3 position)
        {
            if (StartOfRound.Instance != null)
            {
                if (Can("CheckForPlayersInside"))
                {
                    if (StartOfRound.Instance.shipHasLanded)
                    {
                        if (IsAPlayersOutside() && (position.y > -80f || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(position)))
                        {
                            return true;
                        }
                        else
                        {
                            return playersEnteredInside;
                        }
                    }
                }
            }
            return true;
        }

        public static bool IsAPlayersOutside()
        {
            if (StartOfRound.Instance.shipHasLanded)
            {
                List<PlayerControllerB> list = Plugin.GetActivePlayers();
                for (int i = 0; i < list.Count; i++)
                {
                    PlayerControllerB player = list[i];
                    if (!player.isInsideFactory)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAPlayerInsideShip()
        {
            if (StartOfRound.Instance.shipHasLanded)
            {
                List<PlayerControllerB> list = Plugin.GetActivePlayers();
                for (int i = 0; i < list.Count; i++)
                {
                    PlayerControllerB player = list[i];
                    if (player.isInHangarShipRoom)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAPlayerInsideDungeon()
        {
            if (StartOfRound.Instance.shipHasLanded)
            {
                List<PlayerControllerB> list = Plugin.GetActivePlayers();
                for (int i = 0; i < list.Count; i++)
                {
                    PlayerControllerB player = list[i];
                    if (player.isInsideFactory)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CanMob(string parentIdentifier, string identifier, string mobName)
        {
            string mob = RemoveInvalidCharacters(mobName).ToUpper();
            if (Instance.Config[new ConfigDefinition("Mobs", parentIdentifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                foreach (ConfigDefinition entry in Instance.Config.Keys)
                {
                    if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(RemoveInvalidCharacters(mob + identifier).ToUpper()))
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

        public static int GetInt(string parentIdentifier, string identifier)
        {
            try
            {
                int.TryParse(Instance.Config[parentIdentifier, identifier].BoxedValue.ToString(), out int result);
                return result;
            }
            catch
            {
                return 1;
            }
        }

        public static float GetFloat(string parentIdentifier, string identifier)
        {
            try
            {
                float.TryParse(Instance.Config[parentIdentifier, identifier].BoxedValue.ToString(), out float result);
                return result;
            }
            catch
            {
                return 1;
            }
        }

        public static bool GetBool(string parentIdentifier, string identifier)
        {
            string result = RemoveInvalidCharacters(Instance.Config[parentIdentifier, identifier].BoxedValue.ToString()).ToUpper();
            if (result.Equals("TRUE"))
            {
                return true;
            }
            else
            {
                return false;
            }
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

        public static void CreateHarmonyPatch(Harmony harmony, Type typeToPatch, string methodToPatch, Type[] parameters, Type patchType, string patchMethod, bool isPrefix, bool isTranspiler = false)
        {
            if (typeToPatch == null || patchType == null)
            {
                logger.LogInfo("Type is either incorrect or does not exist!");
                return;
            }
            MethodInfo Method = AccessTools.Method(typeToPatch, methodToPatch, parameters, null);
            MethodInfo Patch_Method = AccessTools.Method(patchType, patchMethod, null, null);
            if (isTranspiler)
            {
                harmony.Patch(Method, null, null, new HarmonyMethod(Patch_Method), null, null);
            }
            else
            {
                if (isPrefix)
                {
                    harmony.Patch(Method, new HarmonyMethod(Patch_Method), null, null, null, null);
                }
                else
                {
                    harmony.Patch(Method, null, new HarmonyMethod(Patch_Method), null, null, null);
                }
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

        public static bool AttackTargets(FAIR_AI turret_ai, Vector3 aimPoint, Vector3 forward, float range)
        {
            return HitTargets(GetTargets(turret_ai, aimPoint, forward, range), forward);
        }

        public static List<GameObject> GetTargets(FAIR_AI turret_ai, Vector3 aimPoint, Vector3 forward, float range)
        {
            List<GameObject> targets = new List<GameObject>();
            Ray ray = new Ray(aimPoint, forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, range, -5, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 end = aimPoint + forward * range;
            for (int j = 0; j < hits.Length; j++)
            {
                GameObject obj = hits[j].transform.gameObject;
                Transform hit = hits[j].transform;
                if (hit.TryGetComponent(out IHittable hittable))
                {
                    EnemyAI ai = null;
                    if (hittable is EnemyAICollisionDetect detect)
                    {
                        ai = detect.mainScript;
                    }
                    if (ai != null)
                    {
                        if (!ai.isEnemyDead && ai.enemyHP > 0)
                        {
                            targets.Add(hit.gameObject);
                        }
                    }
                    if (!(hittable is EnemyAICollisionDetect) && !(hittable is PlayerControllerB) && !(hittable is SandSpiderWebTrap))
                    {
                        if (!(hittable is Turret))
                        {
                            hittable.Hit(1, end);
                        }
                        else
                        {
                            if (GetBool("TurretConfig", "HitOtherTurrets"))
                            {
                                hittable.Hit(1, end);
                            }
                        }
                    }
                    end = hits[j].point;
                }
                else
                {
                    //Precaution: hit enemy without hitting hittable (immune to shovels?)
                    if (hit.TryGetComponent(out EnemyAI ai))
                    {
                        if (!ai.isEnemyDead && ai.enemyHP > 0)
                        {
                            targets.Add(ai.gameObject);
                            end = hits[j].point;
                        }
                    }
                    end = hits[j].point;
                }
            }
            return targets;
        }

        public static List<EnemyAICollisionDetect> GetEnemyTargets(List<GameObject> originalTargets)
        {
            List<EnemyAICollisionDetect> hits = new List<EnemyAICollisionDetect>();
            originalTargets.ForEach(t =>
            {
                if (t != null)
                {
                    if (t.GetComponent<IHittable>() != null)
                    {
                        IHittable hit = t.GetComponent<IHittable>();
                        if (hit is EnemyAICollisionDetect)
                        {
                            EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                            hits.Add(enemy);
                        }
                    }
                }
            });
            return hits;
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
                        if (t.GetComponent<EnemyAI>() != null)
                        {
                            EnemyAI enemy = t.GetComponent<EnemyAI>();
                            int damage = GetInt("TurretConfig", "Enemy Damage");
                            if (CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.enemyType.enemyName))
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
                        else if (t.GetComponent<IHittable>() != null)
                        {
                            IHittable hit = t.GetComponent<IHittable>();
                            if (hit is EnemyAICollisionDetect)
                            {
                                EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                                int damage = GetInt("TurretConfig", "Enemy Damage");
                                if (CanMob("TurretDamageAllMobs", ".Turret Damage", enemy.mainScript.enemyType.enemyName))
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
                            else if (hit is PlayerControllerB)
                            {
                                //hits = true;
                            }
                            else
                            {
                                if (!(hit is Turret))
                                {
                                    hit.Hit(1, forward, null, true);
                                    hits = true;
                                }
                                else
                                {
                                    if (GetBool("TurretConfig", "HitOtherTurrets"))
                                    {
                                        hit.Hit(1, forward, null, true);
                                        hits = true;
                                    }
                                }
                            }
                        }
                    }
                });
            }
            return hits;
        }
    }
}