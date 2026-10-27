using UnityEngine;

namespace Rift.Gadgets
{
    /// <summary>A heavy door that slides up while its switch is on. It belongs to neither world, so the lens can't cut it.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SwitchDoor : MonoBehaviour
    {
        [SerializeField] private LensSwitch _switch;
        [SerializeField] private float _openHeight = 5f;
        [SerializeField] private float _speed = 9f;

        private Rigidbody2D _body;
        private Vector2 _closed;

        public bool IsOpen => Vector2.Distance(_body.position, _closed) > _openHeight * 0.9f;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _closed = _body.position;
        }

        private void FixedUpdate()
        {
            Vector2 target = _closed + (_switch.IsOn ? Vector2.up * _openHeight : Vector2.zero);
            _body.MovePosition(Vector2.MoveTowards(_body.position, target, _speed * Time.fixedDeltaTime));
        }
    }
}
