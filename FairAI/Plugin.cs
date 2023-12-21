using BepInEx;
using BepInEx.Logging;
using FairAI.Patches;
using HarmonyLib;

namespace FairAI
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.FairAI", modName = "Fair AI", modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static Plugin Instance;

        public static ManualLogSource logger;

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
        }
    }
}
