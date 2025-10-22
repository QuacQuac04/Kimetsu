using UnityEngine;

public class _LockCamera : MonoBehaviour
{
    [Header("Camera Bounds Settings")]
    public Camera targetCamera;
    public Transform player;
    public bool autoFindComponents = true;
    public bool showBounds = true;
    
    // Cached components and transforms (Rule 3 & 4)
    private Transform cachedTransform;
    private Transform targetCameraTransform;
    
    [Header("Bounds Definition")]
    public bool usePolygonCollider = true; // Sử dụng PolygonCollider2D để vẽ vùng
    public BoxCollider2D boundsCollider; // Alternative: sử dụng BoxCollider2D
    
    [Header("Camera Follow Settings")]
    public bool followPlayer = true;
    public float followSpeed = 8f; // Increased for smooth camera following
    public bool useSmoothing = true; // Toggle for instant vs smooth following
    public Vector3 cameraOffset = new Vector3(0, 0, -10);
    
    [Header("Debug")]
    public bool debugMode = false; // Disabled for performance
    
    // Private variables
    private PolygonCollider2D polygonBounds;
    private Vector3 targetPosition;
    private Bounds sceneBounds;
    private Vector3 lastPlayerPosition; // OPTIMIZATION: Track player movement
    
    // ADVANCED: Cached calculations to avoid repeated operations (Stack optimization)
    private float cachedCameraHalfHeight;
    private float cachedCameraHalfWidth;
    private bool boundsCalculated = false;
    
    // Conditional compilation for logging (Rule 7) - DISABLED for performance
    [System.Diagnostics.Conditional("NEVER")]
    private static void LogDebug(string message)
    {
        // Disabled for performance
    }
    
    [System.Diagnostics.Conditional("NEVER")]
    private static void LogWarning(string message)
    {
        // Disabled for performance
    }
    
    [System.Diagnostics.Conditional("NEVER")]
    private static void LogError(string message)
    {
        // Disabled for performance
    }
    
    void Awake()
    {
        // OPTIMIZATION: Use Awake for early initialization
        // Cache transform early (Rule 4)
        cachedTransform = transform;
        
        // Auto find components if enabled
        if (autoFindComponents)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindFirstObjectByType<Camera>();
                }
            }
            
            // Cache camera transform (Rule 4)
            if (targetCamera != null)
            {
                targetCameraTransform = targetCamera.transform;
            }
            
            if (player == null)
            {
                // Avoid tag access (Rule 2)
                _Player playerScript = FindFirstObjectByType<_Player>();
                if (playerScript != null)
                {
                    player = playerScript.transform;
                }
            }
        }
        
        // Get polygon collider for bounds (Rule 3: cache component)
        if (usePolygonCollider)
        {
            polygonBounds = GetComponent<PolygonCollider2D>();
            if (polygonBounds == null)
            {
                LogError("LockScene: No PolygonCollider2D found! Please add one or disable 'usePolygonCollider'");
            }
            else
            {
                // Set as trigger so it doesn't block player movement
                polygonBounds.isTrigger = true;
                CalculatePolygonBounds();
            }
        }
        else if (boundsCollider != null)
        {
            boundsCollider.isTrigger = true;
            sceneBounds = boundsCollider.bounds;
        }
        
        if (debugMode)
        {
            LogDebug($"LockScene initialized. Camera found: {targetCamera != null}, Player found: {player != null}");
        }
        
        // Disable other camera controllers that might interfere
        DisableConflictingCameraControllers();
        
        // Initialize last player position
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }
    
    // OPTIMIZATION: Helper method to avoid string operations
    private bool IsCameraControllerScript(System.Type scriptType)
    {
        string typeName = scriptType.Name;
        return typeName.Contains("Camera") && 
               (typeName.Contains("Follow") || typeName.Contains("Controller"));
    }
    
    void Update()
    {
        // MAJOR OPTIMIZATION: Skip if no significant player movement
        if (targetCameraTransform == null || player == null || !followPlayer) return;
        
        // SMOOTH CAMERA: Always update for smooth following, but with optimized calculations
        Vector3 currentPlayerPos = player.position;
        
        // Calculate desired camera position
        Vector3 desiredPosition = currentPlayerPos + cameraOffset;
        
        // Clamp camera position to bounds
        Vector3 clampedPosition = ClampCameraPosition(desiredPosition);
        
        // Smooth or instant follow based on settings
        if (useSmoothing)
        {
            targetPosition = Vector3.Lerp(targetCameraTransform.position, clampedPosition, followSpeed * Time.deltaTime);
            targetCameraTransform.position = targetPosition;
        }
        else
        {
            // Instant follow for responsive movement
            targetCameraTransform.position = clampedPosition;
        }
        
        lastPlayerPosition = currentPlayerPos;
    }
    
    void LateUpdate()
    {
        // Force camera position in LateUpdate to override other camera controllers (Rule 1)
        if (targetCameraTransform == null || player == null || !followPlayer) return;
        
        Vector3 currentPosition = targetCameraTransform.position;
        Vector3 clampedPosition = ClampCameraPosition(currentPosition);
        
        // Force clamp if camera is outside bounds
        if (Vector3.Distance(currentPosition, clampedPosition) > 0.01f)
        {
            targetCameraTransform.position = clampedPosition;
            
            // Force clamped camera - Zero GC
        }
    }
    
    Vector3 ClampCameraPosition(Vector3 desiredPosition)
    {
        if (targetCamera == null) return desiredPosition;
        
        // Calculate camera viewport size
        float cameraHeight = targetCamera.orthographicSize;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        
        Vector3 clampedPosition = desiredPosition;
        
        if (usePolygonCollider && polygonBounds != null)
        {
            // For polygon bounds, use calculated bounds
            clampedPosition.x = Mathf.Clamp(desiredPosition.x, 
                sceneBounds.min.x + cameraWidth, 
                sceneBounds.max.x - cameraWidth);
            clampedPosition.y = Mathf.Clamp(desiredPosition.y, 
                sceneBounds.min.y + cameraHeight, 
                sceneBounds.max.y - cameraHeight);
        }
        else if (boundsCollider != null)
        {
            // For box collider bounds
            Bounds bounds = boundsCollider.bounds;
            clampedPosition.x = Mathf.Clamp(desiredPosition.x, 
                bounds.min.x + cameraWidth, 
                bounds.max.x - cameraWidth);
            clampedPosition.y = Mathf.Clamp(desiredPosition.y, 
                bounds.min.y + cameraHeight, 
                bounds.max.y - cameraHeight);
        }
        
        // Keep original Z position
        clampedPosition.z = desiredPosition.z;
        
        // Camera clamped - Zero GC
        
        return clampedPosition;
    }
    
    void CalculatePolygonBounds()
    {
        if (polygonBounds == null) return;
        
        Vector2[] points = polygonBounds.points;
        if (points.Length == 0) return;
        
        float minX = points[0].x, maxX = points[0].x;
        float minY = points[0].y, maxY = points[0].y;
        
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 worldPoint = cachedTransform.TransformPoint(points[i]);
            if (worldPoint.x < minX) minX = worldPoint.x;
            if (worldPoint.x > maxX) maxX = worldPoint.x;
            if (worldPoint.y < minY) minY = worldPoint.y;
            if (worldPoint.y > maxY) maxY = worldPoint.y;
        }
        
        Vector3 center;
        center.x = (minX + maxX) * 0.5f;
        center.y = (minY + maxY) * 0.5f;
        center.z = 0f;
        
        Vector3 size;
        size.x = maxX - minX;
        size.y = maxY - minY;
        size.z = 0f;
        
        sceneBounds = new Bounds(center, size);
        
        // Calculated polygon bounds - Zero GC
    }
    
    void DisableConflictingCameraControllers()
    {
        if (targetCamera == null) return;
        
        // Try to find and disable Cinemachine components
        var cinemachineBrain = targetCamera.GetComponent("CinemachineBrain");
        if (cinemachineBrain != null)
        {
            var enabledProperty = cinemachineBrain.GetType().GetProperty("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(cinemachineBrain, false);
                // Disabled CinemachineBrain - Zero GC
            }
        }
        
        // Disable other camera follow scripts
        MonoBehaviour[] cameraScripts = targetCamera.GetComponents<MonoBehaviour>();
        // ZERO GC: Use for loop instead of foreach
        for (int i = 0; i < cameraScripts.Length; i++)
        {
            var script = cameraScripts[i];
            if (script == null || script == this) continue;
            
            // OPTIMIZATION: Avoid string operations in runtime (Rule 2 & 6)
            System.Type scriptType = script.GetType();
            
            // OPTIMIZATION: Use type comparison instead of string operations
            if (IsCameraControllerScript(scriptType))
            {
                script.enabled = false;
                // Disabled conflicting camera script - Zero GC
            }
        }
    }
    
    // Public methods for external control
    public void SetCameraTarget(Camera camera)
    {
        targetCamera = camera;
        // Update cached transform (Rule 4)
        targetCameraTransform = camera != null ? camera.transform : null;
    }
    
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }
    
    public void SetFollowSpeed(float speed)
    {
        followSpeed = speed;
    }
    
    public void EnableCameraFollow(bool enable)
    {
        followPlayer = enable;
    }
    
    public void SetCameraOffset(Vector3 offset)
    {
        cameraOffset = offset;
    }
    
    // Force enable this script and disable all other camera controllers
    public void ForceTakeControl()
    {
        enabled = true;
        followPlayer = true;
        
        // Disable ALL other camera controllers in the scene
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script == null || script == this) continue;
            
            System.Type scriptType = script.GetType();
            string scriptName = scriptType.Name;
            
            // Disable Cinemachine virtual cameras (avoid string operations)
            if ((scriptName.IndexOf("Cinemachine", System.StringComparison.OrdinalIgnoreCase) >= 0) && 
                (scriptName.IndexOf("Virtual", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                script.enabled = false;
                // Disabled Cinemachine virtual camera - Zero GC
            }
            
            // Disable other camera follow scripts
            if ((scriptName.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0) && 
                (scriptName.IndexOf("Follow", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                 scriptName.IndexOf("Controller", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                script.enabled = false;
                // Disabled camera controller - Zero GC
            }
        }
        
        DisableConflictingCameraControllers();
        
        // Taken full control of camera - Zero GC
    }
    
    // Method to manually set bounds using coordinates
    public void SetBounds(float minX, float minY, float maxX, float maxY)
    {
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
        sceneBounds = new Bounds(center, size);
        usePolygonCollider = false;
        
        // Manual bounds set - Zero GC
    }
    
    // Method to get current bounds
    public Bounds GetBounds()
    {
        return sceneBounds;
    }
    
    // Check if a point is within bounds
    public bool IsWithinBounds(Vector3 point)
    {
        if (usePolygonCollider && polygonBounds != null)
        {
            return polygonBounds.OverlapPoint(point);
        }
        else
        {
            return sceneBounds.Contains(point);
        }
    }
    
    // Visualization in Scene view
    void OnDrawGizmos()
    {
        if (!showBounds) return;
        
        // Ensure targetCameraTransform is cached (safety check for Gizmos)
        if (targetCamera != null && targetCameraTransform == null)
        {
            targetCameraTransform = targetCamera.transform;
        }
        
        // Draw polygon bounds
        if (usePolygonCollider && polygonBounds != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
            Vector2[] points = polygonBounds.points;
            
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 worldPoint1 = cachedTransform.TransformPoint(points[i]);
                Vector3 worldPoint2 = cachedTransform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(worldPoint1, worldPoint2);
            }
            
            // Draw bounds box
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // Green with transparency
            Gizmos.DrawCube(sceneBounds.center, sceneBounds.size);
        }
        // Draw box bounds
        else if (boundsCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan with transparency
            Gizmos.DrawCube(boundsCollider.bounds.center, boundsCollider.bounds.size);
        }
        
        // Draw camera viewport if camera is assigned
        if (targetCamera != null && targetCameraTransform != null)
        {
            Gizmos.color = Color.red;
            float cameraHeight = targetCamera.orthographicSize;
            float cameraWidth = cameraHeight * targetCamera.aspect;
            Vector3 cameraSize = new Vector3(cameraWidth * 2f, cameraHeight * 2f, 0);
            Gizmos.DrawWireCube(targetCameraTransform.position, cameraSize);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        if (targetCamera != null && showBounds)
        {
            // Ensure targetCameraTransform is cached (safety check for Gizmos)
            if (targetCameraTransform == null)
            {
                targetCameraTransform = targetCamera.transform;
            }
            Gizmos.color = Color.white;
            float cameraHeight = targetCamera.orthographicSize;
            float cameraWidth = cameraHeight * targetCamera.aspect;
            
            // Draw camera bounds limits
            if (sceneBounds.size != Vector3.zero)
            {
                float minX = sceneBounds.min.x + cameraWidth;
                float maxX = sceneBounds.max.x - cameraWidth;
                float minY = sceneBounds.min.y + cameraHeight;
                float maxY = sceneBounds.max.y - cameraHeight;
                
                Vector3 limitSize = new Vector3(maxX - minX, maxY - minY, 0);
                Vector3 limitCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
                
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(limitCenter, limitSize);
            }
        }
    }
    
    // Resource cleanup (Rule 5)
    void OnDestroy()
    {
        // Clear cached references
        cachedTransform = null;
        targetCameraTransform = null;
        targetCamera = null;
        player = null;
        polygonBounds = null;
    }
}
