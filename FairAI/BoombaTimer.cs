using System.Collections;
using UnityEngine;

namespace FairAI.Component
{
    internal class BoombaTimer : MonoBehaviour
    {
        private bool isActiveBomb = false;
        private void Start()
        {
            StartCoroutine(StartBombTimer());
            Plugin.logger.LogInfo("Boomba has been set active.");
        }

        public IEnumerator StartBombTimer()
        {
            SetActiveBomb(false);
            yield return new WaitForSeconds(3);
            SetActiveBomb(true);
        }

        public void SetActiveBomb(bool isActive)
        {
            isActiveBomb = isActive;
        }

        public bool IsActiveBomb() 
        {
            return isActiveBomb; 
        }
    }
}
