using System;
using UnityEngine;
using UnityEngine.UI;

namespace SixSeven.Systems
{
    /// <summary>
    /// Bridges spell casting input with UI feedback and energy/cooldown validation.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SpellButtonController : MonoBehaviour
    {
        [Header("Spell Setup")]
        [SerializeField] private string spellId = "spell.fireball";
        [SerializeField] private int energyCost = 1;
        [SerializeField] private float cooldownDuration = 3f;
        [SerializeField] private bool startsLocked;

        [Header("References")]
        [SerializeField] private SpellIconUIController spellIconUIController;
        [SerializeField] private Button button;

        private bool isLocked;
        private bool isDisabled;
        private bool isOnCooldown;
        private float cooldownRemaining;

        public string SpellId => spellId;
        public int EnergyCost => energyCost;
        public float CooldownDuration => cooldownDuration;
        public bool StartsLocked => startsLocked;

        public event Action<string> SpellCast;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (spellIconUIController == null)
            {
                spellIconUIController = GetComponent<SpellIconUIController>();
            }

            isLocked = startsLocked;
            spellIconUIController?.SetLocked(isLocked);
            spellIconUIController?.SetDisabled(false);
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        private void Update()
        {
            if (!isOnCooldown)
            {
                return;
            }

            cooldownRemaining -= Time.unscaledDeltaTime;
            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownRemaining = 0f;
            }
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            spellIconUIController?.SetLocked(locked);
        }

        public void SetDisabled(bool disabled)
        {
            isDisabled = disabled;
            if (button != null)
            {
                button.interactable = !disabled;
            }

            spellIconUIController?.SetDisabled(disabled);
        }

        public bool TryCastSpell()
        {
            if (isDisabled || isLocked || isOnCooldown)
            {
                return false;
            }

            var energySystem = EnergySystem.Instance;
            if (energySystem == null)
            {
                Debug.LogWarning($"Spell '{spellId}' cannot cast because EnergySystem.Instance is null.", this);
                return false;
            }

            if (!energySystem.TrySpendEnergy(energyCost))
            {
                spellIconUIController?.PlayInsufficientEnergyFeedback();
                return false;
            }

            StartCooldown();
            SpellCast?.Invoke(spellId);
            return true;
        }

        private void OnButtonClicked()
        {
            TryCastSpell();
        }

        private void StartCooldown()
        {
            if (cooldownDuration <= 0f)
            {
                isOnCooldown = false;
                cooldownRemaining = 0f;
                return;
            }

            isOnCooldown = true;
            cooldownRemaining = cooldownDuration;
            spellIconUIController?.StartCooldown(cooldownDuration);
        }
    }
}
