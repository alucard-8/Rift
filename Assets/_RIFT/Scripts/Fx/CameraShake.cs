using Rift.Levels;
using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// Trauma-based camera shake as a local offset on the camera (its parent rig does the following).
    /// Reacts to hard landings, deaths and reaching the goal.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerRespawn _respawn;
        [SerializeField] private Goal _goal;
        [SerializeField] private float _maxOffset = 0.45f;
        [SerializeField] private float _maxRoll = 1.5f;
        [SerializeField] private float _frequency = 22f;
        [SerializeField] private float _decay = 1.6f;
        [Tooltip("Landings slower than this don't shake.")]
        [SerializeField] private float _landingThreshold = 14f;

        private float _trauma;
        private float _seed;

        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        private void Awake() => _seed = Random.value * 100f;

        private void OnEnable()
        {
            if (_motor != null) _motor.Landed += OnLanded;
            if (_respawn != null) _respawn.Died += OnDied;
            if (_goal != null) _goal.Reached += OnGoal;
        }

        private void OnDisable()
        {
            if (_motor != null) _motor.Landed -= OnLanded;
            if (_respawn != null) _respawn.Died -= OnDied;
            if (_goal != null) _goal.Reached -= OnGoal;
        }

        private void OnLanded(float speed)
        {
            if (speed > _landingThreshold) AddTrauma(Mathf.InverseLerp(_landingThreshold, 24f, speed) * 0.35f + 0.1f);
        }

        private void OnDied() => AddTrauma(0.6f);
        private void OnGoal() => AddTrauma(0.45f);

        private void LateUpdate()
        {
            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * _frequency;
            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 1f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(_seed + 2f, t) - 0.5f) * 2f;

            transform.localPosition = new Vector3(x, y, 0f) * (_maxOffset * shake);
            transform.localRotation = Quaternion.Euler(0f, 0f, r * _maxRoll * shake);
            _trauma = Mathf.Max(0f, _trauma - _decay * Time.unscaledDeltaTime);
        }
    }
}
