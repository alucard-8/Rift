using System.Collections;
using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Freezes time for a moment when the player dies, so the hit reads.</summary>
    public class HitStop : MonoBehaviour
    {
        [SerializeField] private PlayerRespawn _respawn;
        [SerializeField] private float _duration = 0.06f;
        [SerializeField] private float _timeScale = 0.05f;

        private Coroutine _running;

        private void OnEnable() => _respawn.Died += OnDied;

        private void OnDisable()
        {
            _respawn.Died -= OnDied;
            Time.timeScale = 1f;
        }

        private void OnDied()
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Stop());
        }

        private IEnumerator Stop()
        {
            Time.timeScale = _timeScale;
            yield return new WaitForSecondsRealtime(_duration);
            Time.timeScale = 1f;
            _running = null;
        }
    }
}
