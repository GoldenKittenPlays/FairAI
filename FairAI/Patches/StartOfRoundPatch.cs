using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairAI.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void patchStart()
        {
            //This happens at the end of waiting for entrance teleport spawn
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "AllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "AllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the mine or not?"); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                Plugin.logger.LogInfo("MobName: " + enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", enemy.enemyName)))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                                             enemy.enemyName, // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the mine or not?"); // Description
                }
            }
        }
    }
}
