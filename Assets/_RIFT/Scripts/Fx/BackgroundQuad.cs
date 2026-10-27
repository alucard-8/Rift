using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Keeps a background quad filling the camera view, behind everything.</summary>
    [ExecuteAlways]
    public class BackgroundQuad : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _depth = 20f;
        [SerializeField] private float _overscan = 1.05f;

        private void LateUpdate()
        {
            if (_camera == null) return;

            float h = _camera.orthographicSize * 2f * _overscan;
            transform.position = _camera.transform.position + new Vector3(0f, 0f, _depth);
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(h * _camera.aspect, h, 1f);
        }
    }
}
