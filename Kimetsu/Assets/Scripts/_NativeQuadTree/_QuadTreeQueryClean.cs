using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Clean QuadTree query system - No static fields, Burst compatible
/// </summary>
public static class _QuadTreeQueryClean
{
    /// <summary>
    /// Query entities within circular range - BURST SAFE
    /// </summary>
    public static void QueryCircle(_NativeQuadTree quadTree, float2 center, float radius, NativeList<int> results)
    {
        results.Clear();
        
        // Calculate bounding box
        float2 radiusVec = new float2(radius, radius);
        _QuadBounds bounds = _QuadBounds.FromCenterAndSize(center, radiusVec * 2f);
        
        // Use temporary list
        using var temp = new NativeList<int>(64, Allocator.Temp);
        quadTree.Query(bounds, temp);
        
        // Copy results
        for (int i = 0; i < temp.Length; i++)
        {
            results.Add(temp[i]);
        }
    }
    
    /// <summary>
    /// Burst-safe circle query job
    /// </summary>
    [BurstCompile]
    public struct SafeCircleQueryJob : IJob
    {
        [ReadOnly] public _NativeQuadTree quadTree;
        [ReadOnly] public float2 center;
        [ReadOnly] public float radius;
        public NativeList<int> results;
        
        public void Execute()
        {
            results.Clear();
            
            // Inline implementation to avoid any static method calls
            float2 radiusVec = new float2(radius, radius);
            _QuadBounds bounds = _QuadBounds.FromCenterAndSize(center, radiusVec * 2f);
            
            using var temp = new NativeList<int>(64, Allocator.Temp);
            quadTree.Query(bounds, temp);
            
            for (int i = 0; i < temp.Length; i++)
            {
                results.Add(temp[i]);
            }
        }
    }
}
