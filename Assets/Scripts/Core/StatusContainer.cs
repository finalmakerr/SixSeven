using System.Collections.Generic;

namespace SixSeven.Core
{
    public sealed class StatusContainer
    {
        private readonly Dictionary<StatusType, int> activeStatuses = new();

        public bool Has(StatusType status) => activeStatuses.ContainsKey(status);

        public void Add(StatusType status, int duration)
        {
            activeStatuses[status] = duration;
        }

        public void Remove(StatusType status)
        {
            activeStatuses.Remove(status);
        }

        public void Clear()
        {
            activeStatuses.Clear();
        }

        public int GetRemaining(StatusType status)
        {
            return activeStatuses.TryGetValue(status, out var remaining) ? remaining : 0;
        }

        public void Tick()
        {
            if (activeStatuses.Count == 0)
            {
                return;
            }

            var expired = new List<StatusType>();
            var keys = new List<StatusType>(activeStatuses.Keys);
            foreach (var status in keys)
            {
                var remaining = activeStatuses[status];
                if (remaining < 0)
                {
                    continue;
                }

                var next = remaining - 1;
                activeStatuses[status] = next;
                if (next <= 0)
                {
                    expired.Add(status);
                }
            }

            foreach (var status in expired)
            {
                activeStatuses.Remove(status);
            }
        }

        public IReadOnlyCollection<StatusType> All => activeStatuses.Keys;
    }
}
