namespace FairAI.Patches
{
    internal class LevelGenManPatch
    {
        public static void PatchAwake(ref LevelGenerationManager __instance)
        {
            Utils.SetupConfig();
        }
    }
}
