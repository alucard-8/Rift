using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Dust puffs at the player's feet on jump and landing (bigger for harder landings).</summary>
    public class PlayerDust : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private Vector2 _feetOffset = new(0f, -0.5f);
        [SerializeField] private int _jumpCount = 6;
        [SerializeField] private Vector2Int _landCount = new(3, 22);
        [SerializeField] private float _hardLandingSpeed = 20f;

        private void OnEnable()
        {
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
        }

        private void OnDisable()
        {
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
        }

        private void OnJumped() => Puff(_jumpCount, 1.2f);

        private void OnLanded(float speed)
        {
            float t = Mathf.Clamp01(speed / _hardLandingSpeed);
            Puff(Mathf.RoundToInt(Mathf.Lerp(_landCount.x, _landCount.y, t)), Mathf.Lerp(1.5f, 4f, t));
        }

        private void Puff(int count, float spread)
        {
            Vector3 feet = _motor.transform.position + (Vector3)_feetOffset;
            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                emit.position = feet + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
                emit.velocity = new Vector3(side * Random.Range(0.4f, 1f) * spread, Random.Range(0.2f, 1.2f), 0f);
                emit.startSize = Random.Range(0.08f, 0.2f);
                _dust.Emit(emit, 1);
            }
        }
    }
}
