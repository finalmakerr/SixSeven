using UnityEngine;

namespace GameCore
{
    public enum SpecialPowerTargetingMode
    {
        Self,
        SingleTile,
        Area,
        Line
    }

    public enum SpecialPowerBossResistance
    {
        None,
        Resistant,
        Immune
    }

    public enum SpecialPowerActivationFailureReason
    {
        None,
        MissingPower,
        LevelEnded,
        PlayerDead,
        PlayerStunned,
        BoardBusy,
        ActionBlocked,
        ActivationInProgress,
        InsufficientEnergy,
        OnCooldown,
        InvalidTarget,
        BossImmune
    }

    public struct SpecialPowerTarget
    {
        public bool HasPosition;
        public Vector2Int Position;
        public bool IsBoss;

        public static SpecialPowerTarget None => default;

        public static SpecialPowerTarget ForPosition(Vector2Int position)
        {
            return new SpecialPowerTarget
            {
                HasPosition = true,
                Position = position
            };
        }

        public static SpecialPowerTarget ForBoss()
        {
            return new SpecialPowerTarget
            {
                IsBoss = true
            };
        }
    }

    public struct SpecialPowerActivationResult
    {
        public bool Success;
        public SpecialPowerActivationFailureReason FailureReason;
        public string FailureMessage;

        public static SpecialPowerActivationResult Failed(
            SpecialPowerActivationFailureReason reason,
            string message = null)
        {
            return new SpecialPowerActivationResult
            {
                Success = false,
                FailureReason = reason,
                FailureMessage = message
            };
        }

        public static SpecialPowerActivationResult Passed()
        {
            return new SpecialPowerActivationResult
            {
                Success = true,
                FailureReason = SpecialPowerActivationFailureReason.None,
                FailureMessage = null
            };
        }
    }

    [CreateAssetMenu(menuName = "GameCore/Special Power")]
    public class SpecialPowerDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private int energyCost;
        [SerializeField] private int cooldownTurns;
        [SerializeField] private SpecialPowerTargetingMode targetingMode;
        [SerializeField] private bool canTargetBosses;
        [SerializeField] private bool ignoresBossImmunity;
        [SerializeField] private bool allowsDirectPlayerHpModification;

        public string Id => id;
        public int EnergyCost => energyCost;
        public int CooldownTurns => cooldownTurns;
        public SpecialPowerTargetingMode TargetingMode => targetingMode;
        public bool CanTargetBosses => canTargetBosses;
        public bool IgnoresBossImmunity => ignoresBossImmunity;
        public bool AllowsDirectPlayerHpModification => allowsDirectPlayerHpModification;
    }
}
