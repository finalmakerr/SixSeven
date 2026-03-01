using System.Collections.Generic;

namespace SixSeven.Core
{
    public sealed class StatusContainer
    {
        private readonly HashSet<StatusType> activeStatuses = new();

        public bool Has(StatusType status) => activeStatuses.Contains(status);

        public void Add(StatusType status)
        {
            activeStatuses.Add(status);
        }

        public void Remove(StatusType status)
        {
            activeStatuses.Remove(status);
        }

        public void Clear()
        {
            activeStatuses.Clear();
        }

        public IReadOnlyCollection<StatusType> All => activeStatuses;
    }
}
