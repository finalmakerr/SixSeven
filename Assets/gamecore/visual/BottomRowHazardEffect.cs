using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class BottomRowHazardEffect : MonoBehaviour
    {
        [SerializeField] private GameObject poisonEffect;
        [SerializeField] private GameObject fireEffect;
        [SerializeField] private GameObject iceEffect;
        private Coroutine pulseRoutine;

        public void SetHazard(HazardType type)
        {
            poisonEffect.SetActive(type == HazardType.Poison);
            fireEffect.SetActive(type == HazardType.Fire);
            iceEffect.SetActive(type == HazardType.Ice);
        }

        public void Pulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float scaleUp = 1.05f;
            Vector3 originalScale = transform.localScale;

            transform.localScale = originalScale * scaleUp;
            yield return new WaitForSeconds(0.2f);
            transform.localScale = originalScale;
            pulseRoutine = null;
        }
    }
}
