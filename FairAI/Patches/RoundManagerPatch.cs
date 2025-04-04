﻿using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

namespace FairAI.Patches
{
    internal class RoundManagerPatch
    {
        public static void PatchStart(ref StartOfRound __instance)
        {
            //This happens at the end of waiting for entrance teleport spawn
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();
            Plugin.allHittablesMask = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | 2621448 | Plugin.enemyMask;
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "Enemy Damage")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "Enemy Damage", // The key of the configuration option in the configuration file
                                             1f, // The default value
                                             "Damage Turrets will Do To Enemies"); // Description
            }

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "Player Damage")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "Player Damage", // The key of the configuration option in the configuration file
                                             50f, // The default value
                                             "Damage Turrets will Do To Players"); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "HitOtherTurrets")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "HitOtherTurrets", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If turrets can hit other turrets when firing.(Does not make them target other turrets)"); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "ExplodeAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "ExplodeAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Mines."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoombaAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoombaAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Boombas."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "SeamineAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "SeamineAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Seamines."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BerthaAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BerthaAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Big Berthas."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "TurretTargetAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "TurretTargetAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Targeted By Turrets."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "TurretDamageAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "TurretDamageAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Killed By Turrets."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "CheckForPlayersInside")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "CheckForPlayersInside", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "Whether to check for players inside the dungeon before anything else occurs."); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Mine")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Mine", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the landmine or not?"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Seamine")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Seamine", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the Surfaced Seamine or not?"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Bertha")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Bertha", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the Surfaced Big Bertha or not?"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomba")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomba", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Does it set off the LethalThings Boomba or not?"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Turret Target")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Turret Target", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Is it targetable by turrets?"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Turret Damage")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Turret Damage", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Is it damageable by turrets?"); // Description
                }
            }
        }
    }
}
