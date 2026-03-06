using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.GameLoop
{
    [RequireComponent(typeof(Collider))]
    public class ExtractionPoint : MonoBehaviour, IExtractDecision
    {
        private int _nodeIndex;
        private MapData _mapData;
        private GameLoopController _controller;
        private MapEventBus _eventBus;
        private GameLoopUI _gameLoopUI;

        public float triggerRadius = 4f;

        public void Initialize(int nodeIndex, MapData mapData,
            GameLoopController controller, MapEventBus eventBus, GameLoopUI gameLoopUI)
        {
            _nodeIndex = nodeIndex;
            _mapData = mapData;
            _controller = controller;
            _eventBus = eventBus;
            _gameLoopUI = gameLoopUI;

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
            if (!other.CompareTag("Player")) return;
            _gameLoopUI?.ShowExtractionDecision(
                _controller.State,
                onExtract: OnExtract,
                onContinue: OnContinue
            );
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _gameLoopUI?.HideExtractionDecision();
        }

        public bool ShouldExtract(MapData context, int collectedValue)
        {
            return collectedValue > 0;
        }

        public void OnExtract()
        {
            _eventBus?.Publish(new ExtractionDecisionEvent
            {
                nodeIndex = _nodeIndex,
                extracted = true,
                finalValue = _controller.State.collectedValue
            });
            _controller.HandleExtraction(true);
        }

        public void OnContinue()
        {
            _eventBus?.Publish(new ExtractionDecisionEvent
            {
                nodeIndex = _nodeIndex,
                extracted = false,
                finalValue = _controller.State.collectedValue
            });
            _gameLoopUI?.HideExtractionDecision();
        }
    }
}
