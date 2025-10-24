using UnityEngine;

/// <summary>
/// Lock Ground system - Chan player khong cho di qua Lock Ground boundary
/// Gan script nay vao GameObject co BoxCollider2D hoac PolygonCollider2D (Is Trigger = true)
/// </summary>
public class _LockGround : MonoBehaviour
{
    [Header("Lock Ground Settings")]
    [SerializeField] private float blockDistance = 1.5f; // Khoang cach bat dau chan (unit)
    [SerializeField] private bool enableBlocking = true; // Bat/tat he thong
    [SerializeField] private bool showDebugLogs = true; // Hien thi debug logs
    
    private Collider2D lockGroundCollider;
    
    void Awake()
    {
        // Tu dong lay collider (BoxCollider2D hoac PolygonCollider2D)
        lockGroundCollider = GetComponent<Collider2D>();
        
        if (lockGroundCollider == null)
        {
            Debug.LogError("[LockGround] Khong tim thay Collider2D! Can BoxCollider2D hoac PolygonCollider2D.");
            enabled = false;
            return;
        }
        
        // KHONG bat Is Trigger - de chan that su
        if (lockGroundCollider.isTrigger)
        {
            Debug.LogWarning("[LockGround] Collider.isTrigger = true. Chuyen thanh false de chan that su.");
            lockGroundCollider.isTrigger = false;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LockGround] Initialized with {lockGroundCollider.GetType().Name} (Solid Collision)");
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        if (!enableBlocking) return;
        
        Collider2D other = collision.collider;
        
        // Debug: Hien thi tat ca tag de kiem tra
        if (showDebugLogs && Time.frameCount % 60 == 0) // Moi 60 frame hien 1 lan
        {
            Debug.Log($"[LockGround] Collision with: {other.gameObject.name}, Tag: {other.tag}");
        }
        
        // Chi xu ly Player
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // CRITICAL: Kiem tra player co dang NHAY khong
        _Player player = other.GetComponent<_Player>();
        if (player != null && player.IsJumping)
        {
            // Player dang nhay -> TAT COLLISION tam thoi
            Collider2D playerCollider = other;
            Physics2D.IgnoreCollision(lockGroundCollider, playerCollider, true);
            
            if (showDebugLogs)
            {
                Debug.Log($"[LockGround] Player jumping - ignoring collision temporarily");
            }
            
            // Bat lai collision sau khi player khong nhay nua
            StartCoroutine(ReenableCollisionWhenNotJumping(player, playerCollider));
            
            return;
        }
        
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[LockGround] BLOCKING player (not jumping)");
        }
    }
    
    // Coroutine de bat lai collision sau khi player khong con nhay
    private System.Collections.IEnumerator ReenableCollisionWhenNotJumping(_Player player, Collider2D playerCollider)
    {
        // Doi cho den khi player khong con nhay
        while (player != null && player.IsJumping)
        {
            yield return null;
        }
        
        // Bat lai collision
        if (lockGroundCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(lockGroundCollider, playerCollider, false);
            
            if (showDebugLogs)
            {
                Debug.Log($"[LockGround] Collision re-enabled");
            }
        }
    }
    
    // Visualize Lock Ground boundary trong Editor
    void OnDrawGizmosSelected()
    {
        if (lockGroundCollider == null)
            lockGroundCollider = GetComponent<Collider2D>();
        
        if (lockGroundCollider == null) return;
        
        // Ve vien do quanh Lock Ground
        Gizmos.color = Color.red;
        
        if (lockGroundCollider is BoxCollider2D boxCollider)
        {
            // Ve BoxCollider2D
            Vector2 size = boxCollider.size;
            Vector2 offset = boxCollider.offset;
            Vector3 center = transform.position + (Vector3)offset;
            
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
        else if (lockGroundCollider is PolygonCollider2D polyCollider)
        {
            // Ve PolygonCollider2D
            for (int pathIndex = 0; pathIndex < polyCollider.pathCount; pathIndex++)
            {
                Vector2[] points = polyCollider.GetPath(pathIndex);
                
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 worldPoint1 = transform.TransformPoint(points[i]);
                    Vector2 worldPoint2 = transform.TransformPoint(points[(i + 1) % points.Length]);
                    
                    Gizmos.DrawLine(worldPoint1, worldPoint2);
                }
            }
        }
        
        // Ve vung block distance (mau vang)
        Gizmos.color = Color.yellow;
        Bounds bounds = lockGroundCollider.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * blockDistance * 2f);
    }
}
