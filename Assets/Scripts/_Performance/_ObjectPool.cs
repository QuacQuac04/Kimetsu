using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ultra-optimized Object Pool - Zero GC allocation, maximum performance
/// </summary>
public class _ObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialPoolSize = 4; // Zero GC - ultra minimal
    [SerializeField] private int maxPoolSize = 8; // Zero GC - ultra minimal
    [SerializeField] private bool autoExpand = true;
    
    // ZERO GC - Pre-allocated fixed arrays with safety checks
    private GameObject[] availableObjects;
    private int availableCount = 0;
    private GameObject[] activeObjects;
    private int activeCount = 0;
    private Transform poolParent;
    
    // FIXED: Use static arrays to avoid Dictionary allocations
    private static _ObjectPool[] poolsByTypeArray = new _ObjectPool[16]; // Fixed size array
    private static System.Type[] poolTypeKeys = new System.Type[16];
    private static int poolCount = 0;
    
    // Performance counters
    private int totalCreated = 0;
    private int currentActive = 0;
    
    void Awake()
    {
        if (prefab != null) // Only auto-init if prefab assigned
        {
            InitializePoolInternal();
        }
    }
    
    public void InitializePool(GameObject customPrefab = null, int customInitialSize = -1, int customMaxSize = -1)
    {
        // Allow custom parameters
        if (customPrefab != null) prefab = customPrefab;
        if (customInitialSize > 0) initialPoolSize = customInitialSize;
        if (customMaxSize > 0) maxPoolSize = customMaxSize;
        
        InitializePoolInternal();
    }
    
    private void InitializePoolInternal()
    {
        // Skip initialization if no prefab assigned
        if (prefab == null)
        {
            // Create parent object with generic name
            poolParent = new GameObject("Pool_Empty").transform;
            poolParent.SetParent(transform);
            
            // Initialize arrays but don't create objects
            availableObjects = new GameObject[maxPoolSize];
            activeObjects = new GameObject[maxPoolSize];
            availableCount = 0;
            activeCount = 0;
            return;
        }
        
        // Create parent object for organization - ZERO GC: avoid string interpolation
        poolParent = new GameObject("Pool_" + prefab.name).transform;
        poolParent.SetParent(transform);
        
        // Zero GC - initialize fixed arrays
        availableObjects = new GameObject[maxPoolSize];
        activeObjects = new GameObject[maxPoolSize];
        availableCount = 0;
        activeCount = 0;
        
        // Pre-create objects
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
        
        // FIXED: Register this pool using arrays instead of Dictionary
        var poolableComponent = prefab.GetComponent<IPoolable>();
        if (poolableComponent != null && poolCount < poolsByTypeArray.Length)
        {
            System.Type componentType = poolableComponent.GetType();
            
            // Check if type already exists
            bool found = false;
            for (int i = 0; i < poolCount; i++)
            {
                if (poolTypeKeys[i] == componentType)
                {
                    poolsByTypeArray[i] = this; // Update existing
                    found = true;
                    break;
                }
            }
            
            // Add new if not found
            if (!found)
            {
                poolTypeKeys[poolCount] = componentType;
                poolsByTypeArray[poolCount] = this;
                poolCount++;
            }
        }
        
        // Removed Debug.Log to eliminate GC allocation from string interpolation
    }
    
    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, poolParent);
        obj.SetActive(false);
        
        // Add pool reference to object
        var poolItem = obj.GetComponent<_PoolItem>();
        if (poolItem == null)
        {
            poolItem = obj.AddComponent<_PoolItem>();
        }
        poolItem.SetPool(this);
        
        // FIXED: Add safety check to prevent array overflow
        if (availableCount < availableObjects.Length) {
            availableObjects[availableCount] = obj;
            availableCount++;
        } else {
            // Pool is full - destroy excess object to prevent memory leak
            DestroyImmediate(obj);
            return null;
        }
        totalCreated++;
        
        return obj;
    }
    
    /// <summary>
    /// Get object from pool - Zero GC allocation
    /// </summary>
    public GameObject Get()
    {
        GameObject obj = null;
        
        // Zero GC - get from fixed array instead of Queue
        if (availableCount > 0)
        {
            availableCount--;
            obj = availableObjects[availableCount];
            availableObjects[availableCount] = null; // Clear reference
        }
        // Create new if pool empty and expansion allowed
        else if (autoExpand && totalCreated < maxPoolSize)
        {
            obj = CreateNewObject();
            // Object is already added to available array in CreateNewObject, so get it
            if (availableCount > 0) {
                availableCount--;
                obj = availableObjects[availableCount];
                availableObjects[availableCount] = null;
            }
        }
        
        if (obj != null)
        {
            obj.SetActive(true);
            // FIXED: Add safety check to prevent array overflow
            if (activeCount < activeObjects.Length) {
                activeObjects[activeCount] = obj;
                activeCount++;
                currentActive++;
            } else {
                // Active array is full - return object to pool immediately
                obj.SetActive(false);
                return null;
            }
            
            // Notify object it was spawned
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawnFromPool();
        }
        
        return obj;
    }
    
    /// <summary>
    /// Return object to pool - Zero GC allocation
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        // Zero GC - find in fixed array instead of HashSet.Contains
        int objIndex = -1;
        for (int i = 0; i < activeCount; i++) {
            if (activeObjects[i] == obj) {
                objIndex = i;
                break;
            }
        }
        if (objIndex == -1) return;
        
        // Notify object it's being returned
        var poolable = obj.GetComponent<IPoolable>();
        poolable?.OnReturnToPool();
        
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        
        // Zero GC - remove from active array and add to available array
        activeObjects[objIndex] = activeObjects[activeCount - 1]; // Move last to current position
        activeObjects[activeCount - 1] = null; // Clear last position
        activeCount--;
        
        // FIXED: Add safety check to prevent array overflow
        if (availableCount < availableObjects.Length) {
            availableObjects[availableCount] = obj;
            availableCount++;
            currentActive--;
        } else {
            // Available array is full - destroy object to prevent memory leak
            DestroyImmediate(obj);
        }
    }
    
    /// <summary>
    /// Static method to get object from any pool - ZERO GC VERSION
    /// </summary>
    public static GameObject GetFromPool<T>() where T : IPoolable
    {
        // FIXED: Use array lookup instead of Dictionary
        System.Type targetType = typeof(T);
        for (int i = 0; i < poolCount; i++)
        {
            if (poolTypeKeys[i] == targetType)
            {
                return poolsByTypeArray[i]?.Get();
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get pool statistics
    /// </summary>
    public void GetPoolStats(out int active, out int available, out int total)
    {
        active = currentActive;
        available = availableCount; // Zero GC - use count instead of .Count
        total = totalCreated;
    }
    
    // Debug GUI removed to eliminate GC allocation from UI rendering
}

/// <summary>
/// Interface for poolable objects
/// </summary>
public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}

/// <summary>
/// Component attached to pooled objects
/// </summary>
public class _PoolItem : MonoBehaviour
{
    private _ObjectPool parentPool;
    
    public void SetPool(_ObjectPool pool)
    {
        parentPool = pool;
    }
    
    public void ReturnToPool()
    {
        parentPool?.Return(gameObject);
    }
    
    // Auto-return after delay
    public void ReturnToPoolAfterDelay(float delay)
    {
        StartCoroutine(ReturnAfterDelay(delay));
    }
    
    private System.Collections.IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }
}
