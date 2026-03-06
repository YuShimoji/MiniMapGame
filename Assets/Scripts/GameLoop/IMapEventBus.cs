using System;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// ScriptableObject-based event channel for loose coupling between MonoBehaviours.
    /// </summary>
    public interface IMapEventBus
    {
        void Publish<T>(T eventData);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }
}
