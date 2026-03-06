using UnityEngine;

namespace MiniMapGame.GameLoop
{
    [RequireComponent(typeof(Collider))]
    public class ValueObjectBehaviour : MonoBehaviour, IValueObject
    {
        private string _objectId;
        private int _value;
        private GameLoopController _controller;
        private MapEventBus _eventBus;
        private bool _collected;
        private float _baseY;

        public string ObjectId => _objectId;
        public int Value => _value;

        public void Initialize(string objectId, int value,
            GameLoopController controller, MapEventBus eventBus)
        {
            _objectId = objectId;
            _value = value;
            _controller = controller;
            _eventBus = eventBus;
            _baseY = transform.position.y;

            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void Update()
        {
            if (_collected) return;
            var pos = transform.position;
            pos.y = _baseY + Mathf.Sin(Time.time * 2f) * 0.3f;
            transform.position = pos;
            transform.Rotate(Vector3.up, 60f * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag("Player")) return;
            OnCollect();
        }

        public void OnCollect()
        {
            if (_collected) return;
            _collected = true;

            _controller.State.RecordCollection(_objectId, _value);

            _eventBus?.Publish(new ValueCollectedEvent
            {
                objectId = _objectId,
                value = _value,
                totalValue = _controller.State.collectedValue
            });

            var r = GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            Destroy(gameObject, 0.1f);
        }
    }
}
