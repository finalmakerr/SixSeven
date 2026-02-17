using UnityEngine;

namespace GameCore
{
    public class BottomRowHazardEffect : MonoBehaviour
    {
        [SerializeField] private GameObject poisonEffect;
        [SerializeField] private GameObject fireEffect;
        [SerializeField] private GameObject iceEffect;

        public void SetHazard(HazardType type)
        {
            poisonEffect.SetActive(type == HazardType.Poison);
            fireEffect.SetActive(type == HazardType.Fire);
            iceEffect.SetActive(type == HazardType.Ice);
        }
    }
}
