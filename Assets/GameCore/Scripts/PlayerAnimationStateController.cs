using UnityEngine;

namespace GameCore
{
    public enum PlayerAnimationState
    {
        Idle = 0,
        Happy = 1,
        Excited = 2,
        Stunned = 3,
        Worried = 4,
        Meditating = 5,
        Shielded = 6,
        Tired = 7
    }

    public class PlayerAnimationStateController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Game State Flags")]
        [SerializeField] private bool isHappy;
        [SerializeField] private bool isExcited;
        [SerializeField] private bool isStunned;
        [SerializeField] private bool isWorried;
        [SerializeField] private bool isMeditating;
        [SerializeField] private bool isShielded;
        [SerializeField] private bool isTired;

        public PlayerAnimationState CurrentState { get; private set; } = PlayerAnimationState.Idle;

        private static readonly int StateParam = Animator.StringToHash("State");

        public bool IsHappy
        {
            get => isHappy;
            set => isHappy = value;
        }

        public bool IsExcited
        {
            get => isExcited;
            set => isExcited = value;
        }

        public bool IsStunned
        {
            get => isStunned;
            set => isStunned = value;
        }

        public bool IsWorried
        {
            get => isWorried;
            set => isWorried = value;
        }

        public bool IsMeditating
        {
            get => isMeditating;
            set => isMeditating = value;
        }

        public bool IsShielded
        {
            get => isShielded;
            set => isShielded = value;
        }

        public bool IsTired
        {
            get => isTired;
            set => isTired = value;
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            UpdateAnimatorState();
        }

        private void Update()
        {
            UpdateAnimatorState();
        }

        private void UpdateAnimatorState()
        {
            var nextState = ResolveStateFromFlags();
            if (nextState == CurrentState)
            {
                return;
            }

            CurrentState = nextState;
            if (animator != null)
            {
                animator.SetInteger(StateParam, (int)CurrentState);
            }
        }

        public void SetStateFlags(
            bool happy,
            bool excited,
            bool stunned,
            bool worried,
            bool meditating,
            bool shielded,
            bool tired)
        {
            isHappy = happy;
            isExcited = excited;
            isStunned = stunned;
            isWorried = worried;
            isMeditating = meditating;
            isShielded = shielded;
            isTired = tired;
        }

        private PlayerAnimationState ResolveStateFromFlags()
        {
            if (isStunned)
            {
                return PlayerAnimationState.Stunned;
            }

            if (isShielded)
            {
                return PlayerAnimationState.Shielded;
            }

            if (isMeditating)
            {
                return PlayerAnimationState.Meditating;
            }

            if (isExcited)
            {
                return PlayerAnimationState.Excited;
            }

            if (isHappy)
            {
                return PlayerAnimationState.Happy;
            }

            if (isWorried)
            {
                return PlayerAnimationState.Worried;
            }

            if (isTired)
            {
                return PlayerAnimationState.Tired;
            }

            return PlayerAnimationState.Idle;
        }
    }
}
