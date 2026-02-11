using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Implements match-created bombs with a 2-turn fuse, pickup checks,
    /// and an explosion damage falloff map.
    /// </summary>
    public static class BombSystem
    {
        public const int DefaultBombTimerTurns = 2;
        public const int ExplosionRadius = 2;

        private static readonly Dictionary<int, Vector2Int> MatchDamageRanges = new Dictionary<int, Vector2Int>
        {
            { 4, new Vector2Int(1, 2) },
            { 5, new Vector2Int(3, 4) },
            { 6, new Vector2Int(5, 6) },
            { 7, new Vector2Int(7, 8) }
        };

        public static bool TryCreateBombFromMatch(int matchLength, Vector2Int position, System.Random rng, out ActiveBomb bomb)
        {
            bomb = default;
            if (!MatchDamageRanges.TryGetValue(matchLength, out var damageRange))
            {
                return false;
            }

            var random = rng ?? new System.Random();
            var damage = random.Next(damageRange.x, damageRange.y + 1);
            bomb = new ActiveBomb(position, damage, DefaultBombTimerTurns);
            return true;
        }

        public static Vector2Int GetDamageRangeForMatch(int matchLength)
        {
            return MatchDamageRanges.TryGetValue(matchLength, out var range)
                ? range
                : Vector2Int.zero;
        }

        /// <summary>
        /// Decrement all bomb timers by one turn and return bombs that should explode now.
        /// </summary>
        public static List<ActiveBomb> TickBombs(List<ActiveBomb> bombs)
        {
            var explosions = new List<ActiveBomb>();
            if (bombs == null || bombs.Count == 0)
            {
                return explosions;
            }

            for (var i = bombs.Count - 1; i >= 0; i--)
            {
                var ticked = bombs[i].WithTimer(bombs[i].TurnsRemaining - 1);
                bombs[i] = ticked;
                if (ticked.TurnsRemaining > 0)
                {
                    continue;
                }

                explosions.Add(ticked);
                bombs.RemoveAt(i);
            }

            return explosions;
        }

        /// <summary>
        /// Pickup rule: if player moves onto a bomb tile before detonation, the bomb is removed.
        /// </summary>
        public static bool TryPickupBomb(Vector2Int playerDestination, List<ActiveBomb> bombs, out ActiveBomb pickedUpBomb)
        {
            pickedUpBomb = default;
            if (bombs == null || bombs.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < bombs.Count; i++)
            {
                if (bombs[i].Position != playerDestination)
                {
                    continue;
                }

                pickedUpBomb = bombs[i];
                bombs.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Build a full/half damage map around the bomb center.
        /// Distance 0-1 => full damage, distance 2 => half damage.
        /// </summary>
        public static Dictionary<Vector2Int, int> BuildExplosionDamageMap(Vector2Int center, int fullDamage)
        {
            var damageMap = new Dictionary<Vector2Int, int>();
            var halfDamage = Mathf.Max(1, fullDamage / 2);

            for (var dx = -ExplosionRadius; dx <= ExplosionRadius; dx++)
            {
                for (var dy = -ExplosionRadius; dy <= ExplosionRadius; dy++)
                {
                    var distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distance > ExplosionRadius)
                    {
                        continue;
                    }

                    var position = new Vector2Int(center.x + dx, center.y + dy);
                    damageMap[position] = distance <= 1 ? fullDamage : halfDamage;
                }
            }

            return damageMap;
        }
    }

    [Serializable]
    public struct ActiveBomb
    {
        public ActiveBomb(Vector2Int position, int damage, int turnsRemaining)
        {
            Position = position;
            Damage = Mathf.Max(0, damage);
            TurnsRemaining = Mathf.Max(0, turnsRemaining);
        }

        public Vector2Int Position;
        public int Damage;
        public int TurnsRemaining;

        public ActiveBomb WithTimer(int turnsRemaining)
        {
            return new ActiveBomb(Position, Damage, turnsRemaining);
        }
    }
}
