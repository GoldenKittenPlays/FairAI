namespace FairAI.Patches
{
    internal class StartOfRoundPatch
    {
        public static void PatchStart(ref StartOfRound __instance)
        {
            Utils.SetupConfig();
        }

        public static void PatchUpdate(ref StartOfRound __instance)
        {
            if (Plugin.Can("CheckForPlayersInside"))
            {
                if(__instance.shipIsLeaving)
                {
                    Plugin.playersEnteredInside = false;
                }
                else
                {
                    Plugin.playersEnteredInside = Plugin.IsAPlayerInsideDungeon();
                }
            }
            if (__instance.shipHasLanded && !Plugin.roundHasStarted)
            {
                Plugin.speeds = [];
                Plugin.roundHasStarted = true;
            }

            if (__instance.shipIsLeaving)
            {
                Plugin.roundHasStarted = false;
            }
            //Plugin.ImmortalAffected();
        }
    }
}