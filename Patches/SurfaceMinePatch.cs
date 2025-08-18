using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FairAI.Patches
{
    public static class SurfacedMinePatch
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Plugin.surfacedModID);
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void PatchBerthaOnTriggerEnter(ref Bertha __instance, Collider other)
        {
            Type berthaType = typeof(Bertha);
            if (__instance.hasExploded)
            {
                return;
            }
            if (Plugin.AllowFairness(__instance.transform.position))
            {
                EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (component != null && !component.mainScript.isEnemyDead)
                {
                    if (Plugin.CanMob("BerthaAllMobs", ".Bertha", component.mainScript.enemyType.enemyName.ToUpper()))
                    {
                        MethodInfo TriggerMineOnLocalClientByExiting = berthaType.GetMethod("TriggerMineOnLocalClientByExiting", BindingFlags.NonPublic | BindingFlags.Instance);
                        TriggerMineOnLocalClientByExiting.Invoke(__instance, new object[] { -1 });
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void PatchOnTriggerEnter(ref Seamine __instance, Collider other)
        {
            Type seaMineType = typeof(Seamine);
            FieldInfo mineActivated = seaMineType.GetField("mineActivated", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.hasExploded || !(bool)mineActivated.GetValue(__instance))
            {
                return;
            }
            if (Plugin.AllowFairness(__instance.transform.position))
            {
                EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (component != null && !component.mainScript.isEnemyDead)
                {
                    if (Plugin.CanMob("SeamineAllMobs", ".Seamine", component.mainScript.enemyType.enemyName.ToUpper()))
                    {
                        MethodInfo TriggerMineOnLocalClientByExiting = seaMineType.GetMethod("TriggerMineOnLocalClientByExiting", BindingFlags.NonPublic | BindingFlags.Instance);
                        TriggerMineOnLocalClientByExiting.Invoke(__instance, new object[] { });
                    }
                }
            }
        }
    }
}
