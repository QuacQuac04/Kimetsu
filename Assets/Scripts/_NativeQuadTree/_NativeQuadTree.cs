using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using System;

/// <summary>
/// _NativeQuadTree - Ultra-optimized spatial partitioning system
/// Uses Morton codes for 2D to 1D conversion and minimal memory usage
/// Designed for maximum performance with zero GC allocation
/// </summary>
public struct _NativeQuadTree : IDisposable
{
    // Core data structures - all native for zero GC
    private NativeHashMap<uint, QuadNode> nodes;
    private NativeList<uint> mortonCodes;
    private NativeList<int> entityIds;
    private NativeList<_QuadBounds> entityBounds;
    
    // World configuration
    private _QuadBounds worldBounds;
    private float2 worldMin;
    private float2 worldSize;
    private int maxDepth;
    private int maxEntitiesPerNode;
    
    // Performance tracking
    private int totalNodes;
    private int totalEntities;
    private bool isInitialized;
    
    /// <summary>
    /// QuadTree node structure - optimized for memory and performance
    /// </summary>
    public struct QuadNode
    {
        public uint mortonCode;
        public _QuadBounds bounds;
        public NativeList<int> entities;
        public bool isLeaf;
        public int level;
        public int entityCount;
        
        public QuadNode(uint morton, _QuadBounds bounds, int level, Allocator allocator)
        {
            this.mortonCode = morton;
            this.bounds = bounds;
            this.level = level;
            this.isLeaf = true;
            this.entityCount = 0;
            this.entities = new NativeList<int>(allocator);
        }
    }
    
    /// <summary>
    /// Initialize the QuadTree with world bounds and configuration
    /// </summary>
    public _NativeQuadTree(_QuadBounds worldBounds, int maxDepth = 4, int maxEntitiesPerNode = 4, Allocator allocator = Allocator.Persistent)
    {
        this.worldBounds = worldBounds;
        this.worldMin = worldBounds.min;
        this.worldSize = worldBounds.size;
        this.maxDepth = maxDepth;
        this.maxEntitiesPerNode = maxEntitiesPerNode;
        this.totalNodes = 0;
        this.totalEntities = 0;
        this.isInitialized = true;
        
        // Zero GC - ultra minimal capacity
        int initialCapacity = 16; // Ultra minimal for zero GC
        nodes = new NativeHashMap<uint, QuadNode>(initialCapacity, allocator);
        mortonCodes = new NativeList<uint>(initialCapacity, allocator);
        entityIds = new NativeList<int>(initialCapacity, allocator);
        entityBounds = new NativeList<_QuadBounds>(initialCapacity, allocator);
        
        // Create root node
        uint rootMorton = 0;
        var rootNode = new QuadNode(rootMorton, worldBounds, 0, allocator);
        nodes.Add(rootMorton, rootNode);
        totalNodes = 1;
    }
    
    /// <summary>
    /// Insert entity into QuadTree using Morton code optimization
    /// </summary>
    public void Insert(int entityId, _QuadBounds bounds)
    {
        if (!isInitialized) return;
        
        // Calculate Morton code for entity center
        uint morton = _LookupTable.GetBoundsMorton(bounds, worldMin, worldSize);
        
        // Store entity data
        mortonCodes.Add(morton);
        entityIds.Add(entityId);
        entityBounds.Add(bounds);
        totalEntities++;
        
        // Insert into appropriate node
        InsertIntoNode(morton, entityId, bounds, 0);
    }
    
    /// <summary>
    /// Insert entity into specific node (recursive)
    /// </summary>
    private void InsertIntoNode(uint morton, int entityId, _QuadBounds bounds, int level)
    {
        uint nodeMorton = GetNodeMorton(morton, level);
        
        // Get or create node
        if (!nodes.TryGetValue(nodeMorton, out QuadNode node))
        {
            _QuadBounds nodeBounds = GetNodeBounds(nodeMorton, level);
            node = new QuadNode(nodeMorton, nodeBounds, level, Allocator.Persistent);
            nodes.Add(nodeMorton, node);
            totalNodes++;
        }
        
        // Add entity to node
        node.entities.Add(entityId);
        node.entityCount++;
        nodes[nodeMorton] = node;
        
        // Subdivide if necessary
        if (node.isLeaf && node.entityCount > maxEntitiesPerNode && level < maxDepth)
        {
            SubdivideNode(nodeMorton);
        }
    }
    
    /// <summary>
    /// Subdivide node into 4 children
    /// </summary>
    private void SubdivideNode(uint parentMorton)
    {
        if (!nodes.TryGetValue(parentMorton, out QuadNode parent)) return;
        
        // Mark parent as non-leaf
        parent.isLeaf = false;
        nodes[parentMorton] = parent;
        
        // Create 4 child nodes
        _LookupTable.GetChildrenMorton(parentMorton, out uint nw, out uint ne, out uint sw, out uint se);
        
        parent.bounds.GetQuadrants(out _QuadBounds nwBounds, out _QuadBounds neBounds,
                                  out _QuadBounds swBounds, out _QuadBounds seBounds);
        
        CreateChildNode(nw, nwBounds, parent.level + 1);
        CreateChildNode(ne, neBounds, parent.level + 1);
        CreateChildNode(sw, swBounds, parent.level + 1);
        CreateChildNode(se, seBounds, parent.level + 1);
        
        // Redistribute entities to children
        RedistributeEntities(parentMorton);
    }
    
    /// <summary>
    /// Create child node
    /// </summary>
    private void CreateChildNode(uint morton, _QuadBounds bounds, int level)
    {
        var childNode = new QuadNode(morton, bounds, level, Allocator.Persistent);
        nodes.Add(morton, childNode);
        totalNodes++;
    }
    
    /// <summary>
    /// Redistribute entities from parent to children
    /// </summary>
    private void RedistributeEntities(uint parentMorton)
    {
        if (!nodes.TryGetValue(parentMorton, out QuadNode parent)) return;
        
        // Move entities to appropriate children
        for (int i = 0; i < parent.entities.Length; i++)
        {
            int entityId = parent.entities[i];
            int entityIndex = FindEntityIndex(entityId);
            if (entityIndex >= 0)
            {
                _QuadBounds bounds = entityBounds[entityIndex];
                uint morton = mortonCodes[entityIndex];
                
                // Find appropriate child
                uint childMorton = GetNodeMorton(morton, parent.level + 1);
                if (nodes.TryGetValue(childMorton, out QuadNode child))
                {
                    child.entities.Add(entityId);
                    child.entityCount++;
                    nodes[childMorton] = child;
                }
            }
        }
        
        // Clear parent entities
        parent.entities.Clear();
        parent.entityCount = 0;
        nodes[parentMorton] = parent;
    }
    
    /// <summary>
    /// Query entities within bounds using Morton code optimization
    /// </summary>
    public void Query(_QuadBounds queryBounds, NativeList<int> results)
    {
        if (!isInitialized) return;
        
        results.Clear();
        
        // Calculate Morton range for query
        uint centerMorton = _LookupTable.GetBoundsMorton(queryBounds, worldMin, worldSize);
        
        // Query nodes that intersect with bounds
        QueryNode(0, queryBounds, results); // Start from root
    }
    
    /// <summary>
    /// Query specific node recursively
    /// </summary>
    private void QueryNode(uint nodeMorton, _QuadBounds queryBounds, NativeList<int> results)
    {
        if (!nodes.TryGetValue(nodeMorton, out QuadNode node)) return;
        
        // Check if node bounds intersect with query bounds
        if (!node.bounds.Intersects(queryBounds)) return;
        
        if (node.isLeaf)
        {
            // Add entities that intersect with query bounds
            for (int i = 0; i < node.entities.Length; i++)
            {
                int entityId = node.entities[i];
                int entityIndex = FindEntityIndex(entityId);
                if (entityIndex >= 0)
                {
                    _QuadBounds entityBounds = this.entityBounds[entityIndex];
                    if (entityBounds.Intersects(queryBounds))
                    {
                        results.Add(entityId);
                    }
                }
            }
        }
        else
        {
            // Query children
            _LookupTable.GetChildrenMorton(nodeMorton, out uint nw, out uint ne, out uint sw, out uint se);
            QueryNode(nw, queryBounds, results);
            QueryNode(ne, queryBounds, results);
            QueryNode(sw, queryBounds, results);
            QueryNode(se, queryBounds, results);
        }
    }
    
    /// <summary>
    /// Remove entity from QuadTree
    /// </summary>
    public bool Remove(int entityId)
    {
        int entityIndex = FindEntityIndex(entityId);
        if (entityIndex < 0) return false;
        
        uint morton = mortonCodes[entityIndex];
        
        // Remove from all levels
        for (int level = 0; level <= maxDepth; level++)
        {
            uint nodeMorton = GetNodeMorton(morton, level);
            if (nodes.TryGetValue(nodeMorton, out QuadNode node))
            {
                for (int i = 0; i < node.entities.Length; i++)
                {
                    if (node.entities[i] == entityId)
                    {
                        node.entities.RemoveAtSwapBack(i);
                        node.entityCount--;
                        nodes[nodeMorton] = node;
                        break;
                    }
                }
            }
        }
        
        // Remove from entity arrays
        mortonCodes.RemoveAtSwapBack(entityIndex);
        entityIds.RemoveAtSwapBack(entityIndex);
        entityBounds.RemoveAtSwapBack(entityIndex);
        totalEntities--;
        
        return true;
    }
    
    /// <summary>
    /// Update entity position (remove and re-insert)
    /// </summary>
    public void Update(int entityId, _QuadBounds newBounds)
    {
        Remove(entityId);
        Insert(entityId, newBounds);
    }
    
    /// <summary>
    /// Clear all entities from QuadTree - ZERO GC VERSION
    /// </summary>
    public void Clear()
    {
        if (!isInitialized) return;
        
        // FIXED: Use enumerator instead of GetKeyArray to avoid GC allocation
        var enumerator = nodes.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kvp = enumerator.Current;
            if (kvp.Value.entities.IsCreated)
            {
                kvp.Value.entities.Dispose();
            }
        }
        enumerator.Dispose();
        
        nodes.Clear();
        mortonCodes.Clear();
        entityIds.Clear();
        entityBounds.Clear();
        
        // Recreate root node
        uint rootMorton = 0;
        var rootNode = new QuadNode(rootMorton, worldBounds, 0, Allocator.Persistent);
        nodes.Add(rootMorton, rootNode);
        
        totalNodes = 1;
        totalEntities = 0;
    }
    
    /// <summary>
    /// Get statistics for performance monitoring - ZERO GC VERSION
    /// </summary>
    public void GetStats(out int nodeCount, out int entityCount, out int maxLevel, out float memoryUsage)
    {
        nodeCount = totalNodes;
        entityCount = totalEntities;
        maxLevel = 0;
        
        // FIXED: Use enumerator instead of GetKeyArray to avoid GC allocation
        var enumerator = nodes.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kvp = enumerator.Current;
            maxLevel = math.max(maxLevel, kvp.Value.level);
        }
        enumerator.Dispose();
        
        // Estimate memory usage (in KB)
        memoryUsage = (totalNodes * 64 + totalEntities * 32) / 1024f; // Rough estimate
    }
    
    /// <summary>
    /// Helper methods
    /// </summary>
    private uint GetNodeMorton(uint entityMorton, int level)
    {
        int shift = (maxDepth - level) * 2;
        return entityMorton >> shift;
    }
    
    private _QuadBounds GetNodeBounds(uint nodeMorton, int level)
    {
        float2 position = _LookupTable.DecodeMorton(nodeMorton << ((maxDepth - level) * 2), worldMin, worldSize);
        float nodeSize = math.pow(2f, maxDepth - level);
        float2 size = worldSize / nodeSize;
        
        return _QuadBounds.FromMinAndSize(position, size);
    }
    
    private int FindEntityIndex(int entityId)
    {
        for (int i = 0; i < entityIds.Length; i++)
        {
            if (entityIds[i] == entityId)
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Dispose native collections - ZERO GC VERSION
    /// </summary>
    public void Dispose()
    {
        if (!isInitialized) return;
        
        // FIXED: Use enumerator instead of GetKeyArray to avoid GC allocation
        var enumerator = nodes.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kvp = enumerator.Current;
            if (kvp.Value.entities.IsCreated)
            {
                kvp.Value.entities.Dispose();
            }
        }
        enumerator.Dispose();
        
        // Dispose main collections
        if (nodes.IsCreated) nodes.Dispose();
        if (mortonCodes.IsCreated) mortonCodes.Dispose();
        if (entityIds.IsCreated) entityIds.Dispose();
        if (entityBounds.IsCreated) entityBounds.Dispose();
        
        isInitialized = false;
    }
}

/// <summary>
/// Burst-compiled QuadTree jobs for maximum performance
/// </summary>
[BurstCompile]
public struct QuadTreeQueryJob : IJob
{
    [ReadOnly] public _NativeQuadTree quadTree;
    [ReadOnly] public _QuadBounds queryBounds;
    public NativeList<int> results;
    
    public void Execute()
    {
        quadTree.Query(queryBounds, results);
    }
}

[BurstCompile]
public struct QuadTreeInsertJob : IJobParallelFor
{
    public _NativeQuadTree quadTree;
    [ReadOnly] public NativeArray<int> entityIds;
    [ReadOnly] public NativeArray<_QuadBounds> bounds;
    
    public void Execute(int index)
    {
        quadTree.Insert(entityIds[index], bounds[index]);
    }
}
