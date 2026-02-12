using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public enum MiniGoalType
    {
        DestroyTumors = 0,
        SurviveTurns = 1,
        ReachTile = 2,
        ClearPathToBoss = 3
    }

    [Serializable]
    public struct TumorSpawnData
    {
        public Vector2Int position;
        public int tier;
    }

    [Serializable]
    public struct MiniGoalDefinition
    {
        public MiniGoalType type;
        public int targetValue;
        public Vector2Int targetTile;
    }

    [Serializable]
    public struct LevelRunDefinition
    {
        public int seed;
        public int difficultyTier;
        public List<TumorSpawnData> tumors;
        public List<MiniGoalDefinition> miniGoals;
    }

    public static class LevelRunGeneration
    {
        public static LevelRunDefinition BuildRunDefinition(int levelIndex, LevelDefinition level, bool isBossLevel)
        {
            var seed = BuildSeed(levelIndex);
            var random = new System.Random(seed);
            var gridSize = level.gridSize == Vector2Int.zero ? new Vector2Int(7, 7) : level.gridSize;
            var difficultyTier = Mathf.Clamp(level.difficultyTier, 1, 10);
            var tumors = GenerateTumorPlacements(random, gridSize, level.baseTumorCount, difficultyTier);
            var goals = GenerateMiniGoals(random, gridSize, isBossLevel, tumors, difficultyTier);

            return new LevelRunDefinition
            {
                seed = seed,
                difficultyTier = difficultyTier,
                tumors = tumors,
                miniGoals = goals
            };
        }

        private static int BuildSeed(int levelIndex)
        {
            unchecked
            {
                var runSalt = Environment.TickCount;
                var timeSalt = DateTime.UtcNow.Millisecond;
                return (levelIndex * 73856093) ^ (runSalt * 19349663) ^ timeSalt;
            }
        }

        private static List<TumorSpawnData> GenerateTumorPlacements(System.Random random, Vector2Int gridSize, int baseTumorCount, int difficultyTier)
        {
            var tumors = new List<TumorSpawnData>();
            var totalTiles = Mathf.Max(1, gridSize.x * gridSize.y);
            var desiredTumors = Mathf.Clamp(baseTumorCount + difficultyTier - 1 + random.Next(0, 3), 1, Mathf.Max(2, totalTiles / 3));
            var usedPositions = new HashSet<Vector2Int>();
            for (var i = 0; i < desiredTumors; i++)
            {
                var position = FindUniquePosition(random, gridSize, usedPositions);
                if (position == null)
                {
                    break;
                }

                var roll = random.Next(0, 100);
                var tier = roll < (60 - difficultyTier * 3) ? 1 : roll < (90 - difficultyTier) ? 2 : 3;
                tumors.Add(new TumorSpawnData
                {
                    position = position.Value,
                    tier = Mathf.Clamp(tier, 1, 3)
                });
            }

            return tumors;
        }

        private static List<MiniGoalDefinition> GenerateMiniGoals(System.Random random, Vector2Int gridSize, bool isBossLevel, List<TumorSpawnData> tumors, int difficultyTier)
        {
            var goals = new List<MiniGoalDefinition>();
            var addCount = random.Next(1, 3);
            var hasTumors = tumors != null && tumors.Count > 0;

            if (hasTumors)
            {
                goals.Add(new MiniGoalDefinition
                {
                    type = MiniGoalType.DestroyTumors,
                    targetValue = Mathf.Clamp(1 + difficultyTier / 3, 1, tumors.Count)
                });
            }

            goals.Add(new MiniGoalDefinition
            {
                type = MiniGoalType.SurviveTurns,
                targetValue = Mathf.Clamp(2 + difficultyTier + random.Next(0, 3), 2, 20)
            });

            if (random.NextDouble() < 0.7)
            {
                goals.Add(new MiniGoalDefinition
                {
                    type = MiniGoalType.ReachTile,
                    targetTile = new Vector2Int(random.Next(0, gridSize.x), random.Next(0, gridSize.y))
                });
            }

            if (isBossLevel)
            {
                goals.Add(new MiniGoalDefinition
                {
                    type = MiniGoalType.ClearPathToBoss,
                    targetValue = 1
                });
            }

            while (goals.Count > addCount)
            {
                goals.RemoveAt(random.Next(0, goals.Count));
            }

            return goals;
        }

        private static Vector2Int? FindUniquePosition(System.Random random, Vector2Int gridSize, HashSet<Vector2Int> used)
        {
            for (var i = 0; i < 50; i++)
            {
                var candidate = new Vector2Int(random.Next(0, gridSize.x), random.Next(0, gridSize.y));
                if (used.Add(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
