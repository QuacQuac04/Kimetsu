using UnityEngine;

/// <summary>
/// Script xử lý click vào màn hình để chọn target
/// Attach script này vào GameObject trong scene
/// </summary>
public class _ClickToTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float clickRadius = 1.5f; // Bán kính để detect target khi click
    [SerializeField] private LayerMask targetLayerMask = -1; // Layer của targets (để optimize raycast)

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // Kiểm tra click chuột hoặc touch
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector3 inputPosition = Input.mousePosition;
            
            // Nếu là touch, lấy vị trí touch
            if (Input.touchCount > 0)
            {
                inputPosition = Input.GetTouch(0).position;
            }

            HandleClick(inputPosition);
        }
    }

    private void HandleClick(Vector3 screenPosition)
    {
        if (mainCamera == null || _Focus.Instance == null) return;

        // Chuyển screen position sang world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Đảm bảo z = 0 cho 2D

        // Gửi tới Focus system để chọn target
        _Focus.Instance.SelectTargetAtPosition(worldPosition, clickRadius);
    }

    // Vẽ gizmos để debug (optional)
    void OnDrawGizmos()
    {
        if (mainCamera == null) return;

        // Vẽ vòng tròn tại vị trí chuột để debug click radius
        if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldPos, clickRadius);
        }
    }
}
