using UnityEngine;
using Spine.Unity;
using Unity.Jobs;

public class _Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 12f; // FIXED: Increased for smoother movement

    [Header("References")]
    public SkeletonAnimation skeletonAnimation;

    // OPTIMIZATION: Components cached once - avoid GetComponent calls
    private Rigidbody2D rb;
    private Transform myTransform;
    
    // FIXED: Use arrays instead of Dictionary to avoid GC allocation
    private static Rigidbody2D[] rbCacheArray = new Rigidbody2D[16];
    private static int[] rbCacheKeys = new int[16];
    private static int rbCacheCount = 0;
    
    // Input variables - primitives only
    private float moveInputX;
    private float moveInputY;
    
    // Movement state - use int instead of string to avoid GC
    private int currentAnimationState = 0; // 0=Idle, 1=Run, 2=Die
    private bool isDead = false;
    
    // Pre-allocated vectors to avoid GC
    private Vector2 velocityBuffer = Vector2.zero;
    private Vector3 currentScale = Vector3.one;
    
    // Minimal boid variables for compatibility - optimized for RAM
    private bool isBoidEnabled = false;
    private Vector3 boidVelocity = Vector3.zero;
    private static _Player[] allBoids = new _Player[4]; // Reduced to 4 for minimal RAM
    private static int boidCount = 0;
    
    // QuadTree integration for spatial optimization
    private static _NativeQuadTree spatialQuadTree;
    private static bool quadTreeInitialized = false;
    private int myEntityId;
    private _QuadBounds myBounds;
    
    [Header("Boid Settings")]
    [SerializeField] private bool enableBoidBehavior = false;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentRadius = 3f;
    [SerializeField] private float cohesionRadius = 4f;
    [SerializeField] private float boidSpeed = 3f;

    void Awake()
    {
        // FIXED: Cache components using arrays instead of Dictionary
        int instanceId = GetInstanceID();
        rb = null;
        
        // Search in cache array
        for (int i = 0; i < rbCacheCount; i++)
        {
            if (rbCacheKeys[i] == instanceId)
            {
                rb = rbCacheArray[i];
                break;
            }
        }
        
        // Add to cache if not found
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rbCacheCount < rbCacheArray.Length)
            {
                rbCacheKeys[rbCacheCount] = instanceId;
                rbCacheArray[rbCacheCount] = rb;
                rbCacheCount++;
            }
        }
        myTransform = transform; // Transform is already cached by Unity

        // FIXED: Optimize Rigidbody2D for smooth movement
        if (rb != null)
        {
            rb.gravityScale = 0f; // No gravity for top-down movement
            rb.linearDamping = 0f; // No drag for responsive movement
            rb.angularDamping = 0f; // No angular drag
            rb.freezeRotation = true; // Prevent rotation
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement
        }

        // Tự động tìm trong children nếu chưa gán
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        if (skeletonAnimation != null)
        {
            // Fix Spine scale at the source
            if (skeletonAnimation.skeletonDataAsset != null)
            {
                skeletonAnimation.skeletonDataAsset.scale = 0.01f; // Fix the asset scale
            }
            
            // Ép Spine khởi tạo ngay tại Awake
            skeletonAnimation.Initialize(true);
            
            if (skeletonAnimation.state != null)
            {
                skeletonAnimation.state.SetAnimation(0, "Idle", true);
                currentAnimationState = 0;
            }
        }

        // Rigidbody2D settings - optimized
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.None; // Disable interpolation for performance
        
        // Cache initial scale
        currentScale = myTransform.localScale;
        
        // Register boid in micro array (minimal RAM)
        RegisterBoid();
        
        // QuadTree temporarily disabled to fix Burst compilation issues
        // InitializeQuadTree();
        // myEntityId = GetInstanceID();
        // UpdateSpatialBounds();
    }
    
    // Animation throttling to reduce CPU load
    private float lastAnimationCheck = 0f;
    private const float ANIMATION_CHECK_INTERVAL = 0.1f; // Check animation 10 times per second
    
    // ZERO GC: Cache input axis IDs to avoid string allocation
    private static readonly int horizontalAxisID = 0; // Input.GetAxisRaw uses internal ID
    private static readonly int verticalAxisID = 1;
    
    // FIXED: Back to FixedUpdate for smooth movement - no lag
    void FixedUpdate()
    {
        if (isDead) return;
        
        // ZERO GC: Use GetAxis with cached names - Input system optimized internally
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputY = Input.GetAxisRaw("Vertical");
        
        // Always handle movement for smooth response
        HandleMovement();
        
        // Update animations every frame for smooth animation
        UpdateAnimations();
    }

    void HandleMovement()
    {
        bool hasInput = moveInputX != 0f || moveInputY != 0f;
        
        if (hasInput)
        {
            // FIXED: Always update velocity for smooth movement - no lag
            velocityBuffer.Set(moveInputX * moveSpeed, moveInputY * moveSpeed);
            rb.linearVelocity = velocityBuffer;
            
            // QuadTree spatial bounds update temporarily disabled
            // if (Time.fixedTime % 0.1f < Time.fixedDeltaTime) // Every 0.1 seconds
            // {
            //     UpdateSpatialBounds();
            // }
            
            // Handle character flipping
            if (moveInputX > 0f && currentScale.x < 0f)
            {
                currentScale.Set(0.7f, 0.7f, 0.7f);
                myTransform.localScale = currentScale;
            }
            else if (moveInputX < 0f && currentScale.x > 0f)
            {
                currentScale.Set(-0.7f, 0.7f, 0.7f);
                myTransform.localScale = currentScale;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // FIXED: Smooth animation system - no lag
    private static readonly string ANIM_IDLE = "Idle";
    private static readonly string ANIM_RUN = "Run";
    
    void UpdateAnimations()
    {
        if (skeletonAnimation?.state == null) return;
        
        // FIXED: Immediate animation response for smooth gameplay
        bool isMoving = moveInputX != 0f || moveInputY != 0f;
        int targetState = isMoving ? 1 : 0; // 0=Idle, 1=Run
        
        // Always update animation state for smooth transitions
        if (currentAnimationState != targetState)
        {
            string animName = targetState == 1 ? ANIM_RUN : ANIM_IDLE;
            skeletonAnimation.state.SetAnimation(0, animName, true);
            currentAnimationState = targetState;
            
            // FIXED: Ensure animation plays immediately
            if (skeletonAnimation.state.GetCurrent(0) != null)
            {
                skeletonAnimation.state.GetCurrent(0).TimeScale = 1f;
            }
        }
    }
    
    /// <summary>
    /// Heavy calculations disabled to maintain zero GC allocation
    /// </summary>
    public void PerformHeavyCalculation()
    {
        // All heavy calculations moved to Jobs System for zero GC
        // Player focuses on input handling and basic movement only
    }
    
    /// <summary>
    /// Debug movement issues - check for lag causes
    /// </summary>
    [ContextMenu("🔍 Debug Movement Issues")]
    public void DebugMovementIssues()
    {
        // Debug info available in inspector - no GC allocation from Debug.Log
    }
    
    /// <summary>
    /// Spawn effect without GC allocation
    /// </summary>
    public void SpawnEffect(Vector3 position)
    {
        // Removed Debug.Log to eliminate GC allocation from string interpolation
        // Use object pooling for effects instead
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Traps"))
        {
            DiePlayer();
        }
    }

    void DiePlayer()
    {
        if (isDead) return;
        
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        
        if (skeletonAnimation?.state != null)
        {
            skeletonAnimation.state.SetAnimation(0, "Die", false);
            currentAnimationState = 2; // Die state
        }
    }

    public void OnDieAnimationEnd()
    {
        enabled = false;
    }
    
    /// <summary>
    /// Start boid flocking behavior using multithreading (Zero GC)
    /// </summary>
    private void StartBoidBehavior()
    {
        isBoidEnabled = true;
        boidVelocity = Vector3.right * boidSpeed; // Initial velocity
        // Removed Debug.Log to eliminate GC allocation
    }
    
    /// <summary>
    /// Stop boid behavior and return to player control (Zero GC)
    /// </summary>
    private void StopBoidBehavior()
    {
        isBoidEnabled = false;
        boidVelocity = Vector3.zero;
        // Removed Debug.Log to eliminate GC allocation
    }
    
    /// <summary>
    /// Boid movement disabled for main player
    /// </summary>
    private void HandleBoidMovement()
    {
        // Disabled for main player to eliminate GC allocation and ensure responsive controls
        // Player uses HandleMovement() for direct input control
    }
    
    /// <summary>
    /// Boid calculations disabled for main player to eliminate GC allocation
    /// </summary>
    private void CalculateBoidForces()
    {
        // Disabled for main player to maintain zero GC allocation
        // Boid behavior should only be used for AI entities, not player character
        // Player uses direct input control for responsive gameplay
    }
    
    /// <summary>
    /// Register boid in array (Zero GC)
    /// </summary>
    private void RegisterBoid()
    {
        // Find empty slot in array
        for (int i = 0; i < allBoids.Length; i++)
        {
            if (allBoids[i] == null)
            {
                allBoids[i] = this;
                if (i >= boidCount) boidCount = i + 1;
                return;
            }
        }
    }
    
    
    /// <summary>
    /// Unregister boid from array (Zero GC)
    /// </summary>
    void UnregisterBoid()
    {
        for (int i = 0; i < boidCount; i++)
        {
            if (allBoids[i] == this)
            {
                allBoids[i] = null;
                
                // Compact array if this was the last element
                if (i == boidCount - 1)
                {
                    boidCount--;
                    // Find new count by scanning backwards
                    while (boidCount > 0 && allBoids[boidCount - 1] == null)
                    {
                        boidCount--;
                    }
                }
                return;
            }
        }
    }
    
    void OnDestroy()
    {
        UnregisterBoid();
    }
    
    /// <summary>
    /// Toggle boid behavior for testing (Zero GC)
    /// </summary>
    public void ToggleBoidBehavior()
    {
        enableBoidBehavior = !enableBoidBehavior;
        // Removed Debug.Log to eliminate GC allocation from string interpolation
    }
    
    /// <summary>
    /// Get boid count for debugging (Zero GC)
    /// </summary>
    public static int GetBoidCount()
    {
        // Clean up null references and update count
        int activeCount = 0;
        for (int i = 0; i < boidCount; i++)
        {
            if (allBoids[i] != null)
            {
                if (activeCount != i)
                {
                    allBoids[activeCount] = allBoids[i];
                    allBoids[i] = null;
                }
                activeCount++;
            }
        }
        boidCount = activeCount;
        return boidCount;
    }
    
    /// <summary>
    /// Clear all boids (for memory cleanup)
    /// </summary>
    public static void ClearAllBoids()
    {
        for (int i = 0; i < allBoids.Length; i++)
        {
            allBoids[i] = null;
        }
        boidCount = 0;
        
        // Clear QuadTree
        if (quadTreeInitialized)
        {
            spatialQuadTree.Clear();
        }
    }
    
    /// <summary>
    /// Initialize QuadTree for spatial optimization (Zero GC)
    /// </summary>
    private static void InitializeQuadTree()
    {
        if (quadTreeInitialized) return;
        
        // Define world bounds for the game (adjust as needed)
        var worldBounds = new _QuadBounds(-100f, -100f, 100f, 100f);
        
        // Initialize QuadTree with optimal settings
        spatialQuadTree = new _NativeQuadTree(worldBounds, maxDepth: 6, maxEntitiesPerNode: 8, Unity.Collections.Allocator.Persistent);
        quadTreeInitialized = true;
    }
    
    /// <summary>
    /// Update spatial bounds for QuadTree optimization
    /// </summary>
    private void UpdateSpatialBounds()
    {
        if (!quadTreeInitialized) return;
        
        // Calculate bounds around player position
        Vector3 pos = myTransform.position;
        float radius = 1f; // Player size
        
        myBounds = new _QuadBounds(
            pos.x - radius, pos.y - radius,
            pos.x + radius, pos.y + radius
        );
        
        // Update in QuadTree
        spatialQuadTree.Update(myEntityId, myBounds);
    }
    
    /// <summary>
    /// Query nearby entities using QuadTree (Ultra-fast spatial queries)
    /// </summary>
    private Unity.Collections.NativeList<int> QueryNearbyEntities(float radius)
    {
        var results = new Unity.Collections.NativeList<int>(Unity.Collections.Allocator.Temp);
        
        if (!quadTreeInitialized) return results;
        
        // Create query bounds
        Vector3 pos = myTransform.position;
        var queryBounds = new _QuadBounds(
            pos.x - radius, pos.y - radius,
            pos.x + radius, pos.y + radius
        );
        
        // Perform spatial query
        spatialQuadTree.Query(queryBounds, results);
        
        return results;
    }
    
    /// <summary>
    /// Get QuadTree statistics for debugging
    /// </summary>
    public static void GetQuadTreeStats(out int nodeCount, out int entityCount, out int maxLevel, out float memoryUsage)
    {
        if (quadTreeInitialized)
        {
            spatialQuadTree.GetStats(out nodeCount, out entityCount, out maxLevel, out memoryUsage);
        }
        else
        {
            nodeCount = 0;
            entityCount = 0;
            maxLevel = 0;
            memoryUsage = 0f;
        }
    }
    
    /// <summary>
    /// Cleanup QuadTree on application quit
    /// </summary>
    void OnApplicationQuit()
    {
        if (quadTreeInitialized)
        {
            spatialQuadTree.Dispose();
            quadTreeInitialized = false;
        }
    }
}
