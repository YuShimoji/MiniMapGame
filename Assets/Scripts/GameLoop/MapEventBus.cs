using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.GameLoop
{
    [CreateAssetMenu(fileName = "MapEventBus", menuName = "MiniMapGame/MapEventBus")]
    public class MapEventBus : ScriptableObject, IMapEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public void Publish<T>(T eventData)
        {
            if (_handlers.TryGetValue(typeof(T), out var del))
                ((Action<T>)del)?.Invoke(eventData);
        }

        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing)) return;
            var result = Delegate.Remove(existing, handler);
            if (result == null)
                _handlers.Remove(type);
            else
                _handlers[type] = result;
        }

        private void OnDisable()
        {
            _handlers.Clear();
        }
    }
}
