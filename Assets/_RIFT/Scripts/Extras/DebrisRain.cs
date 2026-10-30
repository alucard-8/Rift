using System.Collections;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>
    /// The ceiling lets go. Drops debris into a stretch of the level, with a warning glow
    /// on the ground so the fall is fair. Only runs while the player is nearby.
    /// </summary>
    public class DebrisRain : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _debrisMaterial;
        [SerializeField] private Material _markerMaterial;
        [SerializeField] private float _width = 7f;
        [SerializeField] private float _height = 9f;
        [SerializeField] private float _interval = 1.6f;
        [SerializeField] private float _warning = 0.55f;
        [SerializeField] private float _size = 0.8f;
        [SerializeField] private float _activeRadius = 26f;

        private Transform _player;
        private float _timer;

        private void Awake()
        {
            var motor = FindFirstObjectByType<Rift.Player.PlayerMotor>();
            _player = motor != null ? motor.transform : null;
            _timer = Random.Range(0.2f, _interval);
        }

        private void Update()
        {
            if (_player == null) return;
            if (Vector2.Distance(_player.position, transform.position) > _activeRadius) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _interval;
            StartCoroutine(Drop());
        }

        private IEnumerator Drop()
        {
            float x = transform.position.x + Random.Range(-_width * 0.5f, _width * 0.5f);
            float spawnY = transform.position.y + _height;
            float groundY = GroundY(x, spawnY + 2f);

            GameObject marker = MakeMarker(x, groundY);
            yield return new WaitForSeconds(_warning);
            if (marker != null) Destroy(marker);

            var go = new GameObject("Debris");
            go.transform.position = new Vector3(x, spawnY, 0f);
            go.transform.localScale = Vector3.one * _size;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _debrisMaterial;
            renderer.sortingLayerName = "World";
            renderer.sortingOrder = 32;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.2f;
            body.freezeRotation = false;
            body.angularVelocity = Random.Range(-160f, 160f);
            body.linearVelocity = new Vector2(Random.Range(-0.6f, 0.6f), 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.8f);

            var debris = go.AddComponent<FallingDebris>();
            debris.SetImpact(groundY);
        }

        private GameObject MakeMarker(float x, float groundY)
        {
            var go = new GameObject("Debris Warning");
            go.transform.position = new Vector3(x, groundY + 0.08f, 0f);
            go.transform.localScale = new Vector3(1.7f, 0.12f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _markerMaterial;
            renderer.sortingLayerName = "World";
            renderer.sortingOrder = 33;

            var block = new MaterialPropertyBlock();
            block.SetFloat("_On", 0.9f);
            block.SetFloat("_Charge", 1f);
            renderer.SetPropertyBlock(block);
            return go;
        }

        private static float GroundY(float x, float fromY)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, 45f);
            float best = float.NegativeInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                best = Mathf.Max(best, hit.point.y);
            }
            return best > float.NegativeInfinity ? best : fromY - 12f;
        }
    }
}
