using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class AttackTelegraphSystem : MonoBehaviour
    {
        public static AttackTelegraphSystem Instance { get; private set; }

        private readonly Dictionary<int, GameObject> activeTelegraphs = new();
        private GameObject telegraphPrefab;
        private Board board;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            telegraphPrefab = Resources.Load<GameObject>("AttackTelegraphIcon");
            board = FindObjectOfType<Board>();
        }

        public void SpawnTelegraph(int monsterId, Vector2Int targetTile)
        {
            if (monsterId == 0 || activeTelegraphs.ContainsKey(monsterId))
            {
                return;
            }

            if (telegraphPrefab == null)
            {
                telegraphPrefab = Resources.Load<GameObject>("AttackTelegraphIcon");
                if (telegraphPrefab == null)
                {
                    return;
                }
            }

            if (board == null)
            {
                board = FindObjectOfType<Board>();
                if (board == null)
                {
                    return;
                }
            }

            var worldPosition = board.GridToWorld(targetTile.x, targetTile.y);
            var telegraphInstance = Instantiate(telegraphPrefab, worldPosition, Quaternion.identity);
            activeTelegraphs[monsterId] = telegraphInstance;
        }

        public void RemoveTelegraph(int monsterId)
        {
            if (!activeTelegraphs.TryGetValue(monsterId, out var telegraph))
            {
                return;
            }

            if (telegraph != null)
            {
                Destroy(telegraph);
            }

            activeTelegraphs.Remove(monsterId);
        }

        public void ClearAllTelegraphs()
        {
            foreach (var telegraph in activeTelegraphs.Values)
            {
                if (telegraph != null)
                {
                    Destroy(telegraph);
                }
            }

            activeTelegraphs.Clear();
        }
    }
}
