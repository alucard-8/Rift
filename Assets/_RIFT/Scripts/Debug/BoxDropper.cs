using UnityEngine;
using UnityEngine.InputSystem;

namespace Rift.DebugTools
{
    /// <summary>Sandbox helper: left click drops a test box at the cursor. Remove once the player exists.</summary>
    public class BoxDropper : MonoBehaviour
    {
        [SerializeField] private GameObject _boxPrefab;
        [SerializeField] private Camera _camera;

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector3 p = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            Instantiate(_boxPrefab, new Vector3(p.x, p.y, 0f), Quaternion.identity);
        }
    }
}
