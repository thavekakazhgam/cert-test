namespace L2Cache.Abstractions
{
    /// <summary>Controls which subscriptions the bus starts.</summary>
    public enum BroadcastBusMode
    {
        Cache = 0,     // Eviction + ClearAll only
        Messaging = 1, // MessageSubject only
        Both = 2       // All three
    }
}