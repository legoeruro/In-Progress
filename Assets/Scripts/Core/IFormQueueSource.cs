using System;

public interface IFormQueueSource
{
    event Action<int> PendingSpawnQueueChanged;
    int PendingSpawnCount { get; }
    bool TryOpenNextIncomingForm();
}
