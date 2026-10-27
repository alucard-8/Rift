using Rift.Audio;
using Rift.Gadgets;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Look, particles and sound for every gadget in a level. Listens to gadget events only.</summary>
    public class GadgetFx : MonoBehaviour
    {
        static readonly int ActiveId = Shader.PropertyToID("_Active");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int LitId = Shader.PropertyToID("_Lit");

        [SerializeField] private BouncePad[] _pads;
        [SerializeField] private CrumblingFloor[] _floors;
        [SerializeField] private LensSwitch[] _switches;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private SfxPlayer _sfx;
        [SerializeField] private SoundBank _bank;

        private MaterialPropertyBlock _block;
        private float[] _flash;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _flash = new float[_pads.Length];
        }

        private void OnEnable()
        {
            for (int i = 0; i < _pads.Length; i++) { int k = i; _pads[i].Bounced += () => OnBounce(k); }
            foreach (CrumblingFloor f in _floors) { CrumblingFloor ff = f; f.Cracked += () => _sfx.Play(_bank.land, 0.5f); f.Fell += () => OnFell(ff); }
            foreach (LensSwitch s in _switches) s.Toggled += on => { if (on) _sfx.Play(_bank.checkpoint, 0.7f); };
        }

        private void OnBounce(int i)
        {
            _flash[i] = 1f;
            Burst(_pads[i].transform.position, 18, new Color(2.2f, 1.8f, 1.2f), 5f);
            _sfx.Play(_bank.doubleJump, 1.3f);
        }

        private void OnFell(CrumblingFloor f)
        {
            Burst(f.transform.position, 30, new Color(0.8f, 0.7f, 0.6f), 3f);
            _sfx.Play(_bank.deathThump, 0.6f);
            if (_shake != null) _shake.AddTrauma(0.2f);
        }

        private void Burst(Vector3 at, int count, Color color, float speed)
        {
            var emit = new ParticleSystem.EmitParams { startColor = color };
            for (int i = 0; i < count; i++)
            {
                emit.position = at + (Vector3)(Random.insideUnitCircle * 0.5f);
                emit.velocity = (Random.insideUnitCircle + Vector2.up) * speed * Random.Range(0.4f, 1f);
                emit.startSize = Random.Range(0.06f, 0.16f);
                emit.startLifetime = Random.Range(0.3f, 0.8f);
                _particles.Emit(emit, 1);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _pads.Length; i++)
            {
                _flash[i] = Mathf.MoveTowards(_flash[i], 0f, dt * 3f);
                Renderer r = _pads[i].GetComponentInChildren<Renderer>();
                r.GetPropertyBlock(_block);
                _block.SetFloat(ActiveId, _pads[i].IsActive ? 1f : 0.15f);
                _block.SetFloat(FlashId, _flash[i]);
                r.SetPropertyBlock(_block);
            }
            foreach (LensSwitch s in _switches)
            {
                Renderer r = s.GetComponentInChildren<Renderer>();
                r.GetPropertyBlock(_block);
                _block.SetFloat(LitId, s.IsOn ? 1f : s.Charge01 * 0.6f);
                r.SetPropertyBlock(_block);
            }
            foreach (CrumblingFloor f in _floors)
            {
                var sr = f.GetComponent<SpriteRenderer>();
                if (f.Current == CrumblingFloor.State.Fallen) continue;
                float c = 1f - f.CrackAmount * 0.6f;
                sr.color = new Color(c, c * 0.95f, c * 0.9f, 1f);
            }
        }
    }
}
