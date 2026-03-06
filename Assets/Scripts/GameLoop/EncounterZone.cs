using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.GameLoop
{
    [RequireComponent(typeof(Collider))]
    public class EncounterZone : MonoBehaviour, IEncounterTrigger
    {
        private int _edgeIndex;
        private MapEdge _chokeEdge;
        private MapData _mapData;
        private GameLoopController _controller;
        private MapEventBus _eventBus;
        private bool _triggered;

        public float triggerRadius = 5f;

        public void Initialize(int edgeIndex, MapEdge chokeEdge, MapData mapData,
            GameLoopController controller, MapEventBus eventBus)
        {
            _edgeIndex = edgeIndex;
            _chokeEdge = chokeEdge;
            _mapData = mapData;
            _controller = controller;
            _eventBus = eventBus;

            var col = GetComponent<Collider>();
            if (col is SphereCollider sphere)
            {
                sphere.isTrigger = true;
                sphere.radius = triggerRadius;
            }
            else if (col != null)
            {
                col.isTrigger = true;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;
            OnEncounter(_chokeEdge, _mapData);
        }

        public void OnEncounter(MapEdge chokeEdge, MapData context)
        {
            _triggered = true;

            _controller.State.RecordEncounter();

            _eventBus?.Publish(new EncounterTriggeredEvent
            {
                edgeIndex = _edgeIndex,
                chokeEdge = chokeEdge,
                encounterNumber = _controller.State.encounterCount
            });

            _controller.gameLoopUI?.ShowEncounterMessage(
                $"Encounter #{_controller.State.encounterCount}!");

            var r = GetComponent<Renderer>();
            if (r != null)
                r.material.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        }
    }
}
