using UnityEngine;

namespace SixSeven.Systems
{
    /// <summary>
    /// Example flow controller for HP/Energy UI + action economy.
    /// </summary>
    public class VitalsPresenter : MonoBehaviour
    {
        [SerializeField] private VitalsBarUIController vitalsUI;
        [SerializeField] private PlayerVitalsSystem vitals = new PlayerVitalsSystem();

        public PlayerVitalsSystem Vitals => vitals;

        private void Start()
        {
            // Start from base max values.
            vitals.RefillHpToBaseMaximum();
            vitals.RefillEnergyToMaximum();
            RefreshUI();
        }

        public bool TryPerformAction(int energyCost)
        {
            // Energy does not auto-regenerate; only explicit calls to GainEnergy restore it.
            if (!vitals.TrySpendEnergy(energyCost))
            {
                return false;
            }

            RefreshUI();
            return true;
        }

        public void GainEnergyFromPickup(int amount)
        {
            vitals.GainEnergy(amount);
            RefreshUI();
        }

        public void AddShieldLayers(int amount)
        {
            vitals.AddShield(amount);
            RefreshUI();
        }

        public PlayerVitalsSystem.DamageResolution ApplyIncomingDamage(int amount)
        {
            var result = vitals.ApplyDamage(amount);
            RefreshUI();
            return result;
        }

        public void IncreaseHpCap(int addedHearts)
        {
            vitals.SetUnlockedHearts(vitals.UnlockedHearts + addedHearts);
            RefreshUI();
        }

        public void IncreaseEnergyCap(int addedHearts)
        {
            vitals.SetEnergyUnlockedHearts(vitals.EnergyUnlockedHearts + addedHearts);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (vitalsUI != null)
            {
                vitalsUI.Refresh(vitals);
            }
        }
    }
}
