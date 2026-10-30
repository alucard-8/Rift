using System.Collections;
using Rift.Audio;
using Rift.Player;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>
    /// A collectible that isn't. It looks like a soul until you get close, then bolts to a new ledge.
    /// After a few escapes it gives up and winks out.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FakeSoul : MonoBehaviour
    {
        static readonly int FadeId = Shader.PropertyToID("_Fade");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int WobbleId = Shader.PropertyToID("_Wobble");

        [SerializeField] private float _triggerRadius = 2.8f;
        [SerializeField] private float _hopDistance = 7f;
        [SerializeField] private float _hopTime = 0.5f;
        [SerializeField] private float _cooldown = 0.9f;
        [SerializeField] private int _escapesBeforeGone = 3;
        [SerializeField] private SoundBank _bank;

        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Transform _player;
        private Vector3 _home;
        private bool _busy;
        private int _escapes;
        private float _bobOffset;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _block = new MaterialPropertyBlock();
            _home = transform.position;
            _bobOffset = Random.value * 10f;
            _block.SetFloat("_Fake", 1f);
            _renderer.SetPropertyBlock(_block);
            PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
            _player = motor != null ? motor.transform : null;
        }

        private void Update()
        {
            if (_busy || _player == null) return;

            Vector3 p = _home;
            p.y += Mathf.Sin(Time.time * 2.4f + _bobOffset) * 0.14f;
            p.x += Mathf.Sin(Time.time * 1.7f + _bobOffset) * 0.05f;
            transform.position = p;

            if (Vector2.Distance(_player.position, transform.position) > _triggerRadius) return;
            StartCoroutine(Flee());
        }

        private IEnumerator Flee()
        {
            _busy = true;
            _escapes++;

            Vector3 from = transform.position;
            float away = Mathf.Sign(from.x - _player.position.x);
            if (Mathf.Approximately(away, 0f)) away = Random.value > 0.5f ? 1f : -1f;

            float targetX = from.x + away * _hopDistance;
            float targetY = SurfaceAt(targetX, from.y + 6f);
            Vector3 to = new(targetX, targetY + 0.9f, from.z);

            SfxPlayer sfx = FindFirstObjectByType<SfxPlayer>();
            if (sfx != null && _bank != null) sfx.Play(_bank.glitchMaterialize, 0.45f);

            float t = 0f;
            while (t < _hopTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _hopTime);
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 1.6f;
                transform.position = p;
                transform.localScale = Vector3.one * (0.55f * (1f + Mathf.Sin(k * Mathf.PI) * 0.35f));
                _block.SetFloat(PulseId, k);
                _block.SetFloat(WobbleId, 2f);
                _renderer.SetPropertyBlock(_block);
                yield return null;
            }

            _home = to;
            transform.position = to;
            transform.localScale = Vector3.one * 0.55f;

            if (_escapes >= _escapesBeforeGone)
            {
                yield return Vanish();
                yield break;
            }

            yield return new WaitForSeconds(_cooldown);
            _busy = false;
        }

        private IEnumerator Vanish()
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float k = t / 0.4f;
                _block.SetFloat(FadeId, 1f - k);
                _block.SetFloat(PulseId, 1f + k * 2f);
                _renderer.SetPropertyBlock(_block);
                transform.localScale = Vector3.one * (0.55f * (1f + k * 0.8f));
                yield return null;
            }
            Destroy(gameObject);
        }

        private static float SurfaceAt(float x, float fromY)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, 30f);
            float best = float.NegativeInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                best = Mathf.Max(best, hit.point.y);
            }
            return best > float.NegativeInfinity ? best : fromY - 3f;
        }
    }
}
