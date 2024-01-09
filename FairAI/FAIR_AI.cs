using System.Reflection;
using Unity.Netcode;

namespace FairAI
{
    internal class FAIR_AI : NetworkBehaviour
    {
        public EnemyAI targetWithRotation;

        [ClientRpc]
        public void SwitchedTargetedEnemyClientRpc(Turret turret, EnemyAI enemy, bool setModeToCharging = false)
        {
            targetWithRotation = enemy;
            if (setModeToCharging)
            {
                System.Type typ = typeof(Turret);
                MethodInfo mode_s_method = typ.GetMethod("SwitchTurretMode", BindingFlags.NonPublic | BindingFlags.Instance);
                mode_s_method.Invoke(turret, new object[] { 1 });
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
