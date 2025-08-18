using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace FairAI
{
    public class FAIR_AI : NetworkBehaviour
    {
        public EnemyAI targetWithRotation;
        public Dictionary<int, GameObject> targets;

        private void Awake()
        {
            targetWithRotation = null;
        }

        [ClientRpc]
        public void SwitchedTargetedEnemyClientRpc(Turret turret, EnemyAI enemy, bool setModeToCharging = false)
        {
            targetWithRotation = enemy;
            if (setModeToCharging)
            {
                System.Type turretType = typeof(Turret);
                MethodInfo mode_s_method = turretType.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                mode_s_method.Invoke(turret, new object[] { 2 });
                //turret.SwitchTurretMode(1);
            }
        }

        [ClientRpc]
        public void RemoveTargetedEnemyClientRpc()
        {
            targetWithRotation = null;
        }
    }
}