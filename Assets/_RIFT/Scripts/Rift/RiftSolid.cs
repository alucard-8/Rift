using System.Collections.Generic;
using UnityEngine;

namespace Rift
{
    /// <summary>
    /// Makes a platform physically exist only where its world is real.
    /// The sprite's bounds are split into small collider cells that switch on/off with the lens.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RiftSolid : MonoBehaviour
    {
        [SerializeField] private RiftWorld _world = RiftWorld.Static;
        [SerializeField] private RiftLens _lens;
        [SerializeField] private string _protectedTag = "Player";

        private readonly List<BoxCollider2D> _cells = new();
        private readonly List<Vector2> _centers = new();
        private Vector2 _cellSize;
        private readonly Collider2D[] _overlaps = new Collider2D[4];
        private ContactFilter2D _filter;

        public RiftWorld World => _world;

        private void Awake()
        {
            _filter = ContactFilter2D.noFilter;
            BuildCells();
        }

        private void BuildCells()
        {
            Bounds bounds = GetComponent<SpriteRenderer>().bounds;
            float size = _lens.Config.cellSize;
            int cols = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / size));
            int rows = Mathf.Max(1, Mathf.RoundToInt(bounds.size.y / size));
            Vector2 cell = new(bounds.size.x / cols, bounds.size.y / rows);
            _cellSize = cell;

            var root = new GameObject("Cells").transform;
            root.SetParent(transform, false);

            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                Vector3 world = bounds.min + new Vector3((x + 0.5f) * cell.x, (y + 0.5f) * cell.y, 0f);
                var box = root.gameObject.AddComponent<BoxCollider2D>();
                box.offset = root.InverseTransformPoint(world);
                box.size = root.InverseTransformVector(cell);
                box.size = new Vector2(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y));
                _cells.Add(box);
                _centers.Add(world);
            }
        }

        private void FixedUpdate()
        {
            // Disabled colliders report empty bounds, so cell centres are cached at build time.
            for (int i = 0; i < _cells.Count; i++)
            {
                BoxCollider2D box = _cells[i];
                bool real = _lens.IsReal(_world, _centers[i]);
                if (real == box.enabled) continue;

                // Never materialise solid ground inside the player.
                if (real && Blocked(_centers[i])) continue;
                box.enabled = real;
            }
        }

        private bool Blocked(Vector2 center)
        {
            int count = Physics2D.OverlapBox(center, _cellSize * 0.9f, 0f, _filter, _overlaps);
            for (int i = 0; i < count; i++)
                if (_overlaps[i].attachedRigidbody != null && _overlaps[i].CompareTag(_protectedTag)) return true;
            return false;
        }
    }
}
