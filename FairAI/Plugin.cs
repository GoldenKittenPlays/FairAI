using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FairAI.Patches;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace FairAI
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("Evaisa-LethalThings-0.8.7", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.FairAI", modName = "Fair AI", modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static Plugin Instance;

        public static ManualLogSource logger;

        public static List<EnemyType> enemies;

        public static List<Item> items;

        public static int wallsAndEnemyLayerMask = 524288;

        private void Awake() 
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            logger.LogInfo("Fair AI initiated!");

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(MineAIPatch));
            harmony.PatchAll(typeof(TurretAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(BoombaPatch));
            //Namespace.Type1.Type2:MethodName
            //MethodInfo Turret_HasLOS_Method = AccessTools.Method(typeof(Turret), "TurnTowardsTargetIfHasLOS", null, null);
            //MethodInfo Turret_LOS_Patch_Method = AccessTools.Method(typeof(TurretAIPatch), nameof(TurretAIPatch.patchTurnTowardsTargetIfHasLOS), null, null);
            //harmony.Patch(Turret_HasLOS_Method, new HarmonyMethod(Turret_LOS_Patch_Method), null, null, null, null);
            MethodInfo AI_KillEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient), null, null);
            MethodInfo KillEnemy_Patch_Method = AccessTools.Method(typeof(EnemyAIPatch), nameof(EnemyAIPatch.PatchKillEnemyOnOwnerClient), null, null);
            harmony.Patch(AI_KillEnemy_Method, new HarmonyMethod(KillEnemy_Patch_Method), null, null, null, null);
        }

        public static bool CanMob(string parentIdentifier, string identifier, string mobName)
        {
            string mob = FairAIUtilities.RemoveInvalidCharacters(mobName).ToUpper();
            Plugin.logger.LogInfo("Mob Name: " + mob);
            if (Instance.Config[new ConfigDefinition("Mobs", parentIdentifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                foreach (ConfigDefinition entry in Instance.Config.Keys)
                {
                    if (FairAIUtilities.RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(FairAIUtilities.RemoveInvalidCharacters(mob + identifier.ToUpper())))
                    {
                        logger.LogInfo("Value of mob: " + Instance.Config[entry].BoxedValue.ToString());
                        return Instance.Config[entry].BoxedValue.ToString().ToUpper().Equals("TRUE");
                    }
                }
                logger.LogInfo(identifier + ": No mob found!");
                return false;
            }
            else
            {
                logger.LogInfo(parentIdentifier + ": All mobs disabled!");
            }
            return false;
        }
    }
}
