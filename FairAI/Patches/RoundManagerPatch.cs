namespace FairAI.Patches
{
    internal class RoundManagerPatch
    {
        public static void PatchStart(ref RoundManager __instance)
        {
            Utils.SetupConfig();
        }
    }
}