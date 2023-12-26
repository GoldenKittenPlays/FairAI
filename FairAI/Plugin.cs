using BepInEx;
using BepInEx.Logging;
using FairAI.Patches;
using HarmonyLib;
using System.Reflection;

namespace FairAI
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("TheFluff-EverythingCanDie-1.1.0", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Evaisa-LethalThings-0.8.7", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.FairAI", modName = "Fair AI", modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static Plugin Instance;

        public static ManualLogSource logger;

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
            harmony.PatchAll(typeof(MineAIPatch));
            harmony.PatchAll(typeof(TurretAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(BoombaPatch));
            //Namespace.Type1.Type2:MethodName
            //MethodInfo Turret_HasLOS_Method = AccessTools.Method(typeof(Turret), "TurnTowardsTargetIfHasLOS", null, null);
            //MethodInfo Turret_LOS_Patch_Method = AccessTools.Method(typeof(TurretAIPatch), nameof(TurretAIPatch.patchTurnTowardsTargetIfHasLOS), null, null);
            //harmony.Patch(Turret_HasLOS_Method, new HarmonyMethod(Turret_LOS_Patch_Method), null, null, null, null);
            MethodInfo AI_KillEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient), null, null);
            MethodInfo KillEnemy_Patch_Method = AccessTools.Method(typeof(EnemyAIPatch), nameof(EnemyAIPatch.patchKillEnemyOnOwnerClient), null, null);
            harmony.Patch(AI_KillEnemy_Method, new HarmonyMethod(KillEnemy_Patch_Method), null, null, null, null);
        }
    }
}
