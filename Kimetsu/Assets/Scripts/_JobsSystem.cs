using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// _JobsSystem - Advanced multithreaded job management system
/// Combines Unity Jobs System with Burst Compiler for maximum performance
/// </summary>
public class _JobsSystem : MonoBehaviour
{
    private static _JobsSystem instance;
    public static _JobsSystem Instance => instance;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentJobs = 32;
    
    // Native arrays for high-performance operations
    private NativeArray<float3> boidPositions;
    private NativeArray<float3> boidVelocities;
    private NativeArray<float3> boidNewVelocities;
    private NativeArray<float> boidRadii;
    
    // Zero GC job management - fixed arrays instead of List/Queue
    private JobHandle[] activeJobs = new JobHandle[16];
    private int activeJobCount = 0;
    private JobHandle[] completedJobs = new JobHandle[16];
    private int completedJobCount = 0;
    
    // Performance tracking
    private int totalJobsScheduled = 0;
    private int totalJobsCompleted = 0;
    private float averageJobTime = 0f;
    
    // Bounds for collision detection
    private float3 worldBoundsMin = new float3(-50, -50, -50);
    private float3 worldBoundsMax = new float3(50, 50, 50);
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeNativeArrays();
    }
    
    void Start()
    {
        // Start the job processing coroutine
        StartCoroutine(ProcessJobsCoroutine());
    }
    
    private void InitializeNativeArrays()
    {
        int maxBoids = 8; // Zero GC - ultra minimal array size
        
        boidPositions = new NativeArray<float3>(maxBoids, Allocator.Persistent);
        boidVelocities = new NativeArray<float3>(maxBoids, Allocator.Persistent);
        boidNewVelocities = new NativeArray<float3>(maxBoids, Allocator.Persistent);
        boidRadii = new NativeArray<float>(maxBoids, Allocator.Persistent);
        
        // Initialize with default values - zero GC loop
        for (int i = 0; i < maxBoids; i++)
        {
            boidRadii[i] = 0.5f; // Default radius
        }
    }
    
    /// <summary>
    /// Schedule a burst-compiled boid calculation job
    /// </summary>
    public JobHandle ScheduleBoidJob(Vector3[] positions, Vector3[] velocities, int count, 
                                   float separationRadius, float alignmentRadius, float cohesionRadius, float maxSpeed)
    {
        if (count <= 0 || count > boidPositions.Length) return default;
        
        // Copy data to native arrays
        for (int i = 0; i < count; i++)
        {
            boidPositions[i] = new float3(positions[i].x, positions[i].y, positions[i].z);
            boidVelocities[i] = new float3(velocities[i].x, velocities[i].y, velocities[i].z);
        }
        
        // Create and schedule the burst job
        var boidJob = new _BurstCompiler.BoidJob
        {
            positions = boidPositions.GetSubArray(0, count),
            velocities = boidVelocities.GetSubArray(0, count),
            newVelocities = boidNewVelocities.GetSubArray(0, count),
            separationRadius = separationRadius,
            alignmentRadius = alignmentRadius,
            cohesionRadius = cohesionRadius,
            maxSpeed = maxSpeed
        };
        
        JobHandle handle = boidJob.Schedule(count, 32); // Process 32 boids per batch
        // Zero GC - add to fixed array instead of List
        if (activeJobCount < activeJobs.Length) {
            activeJobs[activeJobCount] = handle;
            activeJobCount++;
        }
        totalJobsScheduled++;
        
        return handle;
    }
    
    /// <summary>
    /// Schedule a burst-compiled physics job
    /// </summary>
    public JobHandle SchedulePhysicsJob(Vector3[] velocities, int count, float deltaTime, JobHandle dependency = default)
    {
        if (count <= 0 || count > boidPositions.Length) return default;
        
        // Copy velocities to native array
        for (int i = 0; i < count; i++)
        {
            boidVelocities[i] = new float3(velocities[i].x, velocities[i].y, velocities[i].z);
        }
        
        var physicsJob = new _BurstCompiler.PhysicsJob
        {
            positions = boidPositions.GetSubArray(0, count),
            velocities = boidVelocities.GetSubArray(0, count),
            deltaTime = deltaTime
        };
        
        JobHandle handle = physicsJob.Schedule(count, 64, dependency);
        // Zero GC - add to fixed array instead of List
        if (activeJobCount < activeJobs.Length) {
            activeJobs[activeJobCount] = handle;
            activeJobCount++;
        }
        totalJobsScheduled++;
        
        return handle;
    }
    
    /// <summary>
    /// Schedule a burst-compiled collision job
    /// </summary>
    public JobHandle ScheduleCollisionJob(int count, JobHandle dependency = default)
    {
        if (count <= 0 || count > boidPositions.Length) return default;
        
        var collisionJob = new _BurstCompiler.CollisionJob
        {
            positions = boidPositions.GetSubArray(0, count),
            velocities = boidVelocities.GetSubArray(0, count),
            radii = boidRadii.GetSubArray(0, count),
            boundsMin = worldBoundsMin,
            boundsMax = worldBoundsMax
        };
        
        JobHandle handle = collisionJob.Schedule(count, 64, dependency);
        // Zero GC - add to fixed array instead of List
        if (activeJobCount < activeJobs.Length) {
            activeJobs[activeJobCount] = handle;
            activeJobCount++;
        }
        totalJobsScheduled++;
        
        return handle;
    }
    
    // FIXED: Pre-allocated native arrays to avoid repeated allocations
    private NativeArray<float> sharedInputA;
    private NativeArray<float> sharedInputB;
    private NativeArray<float> sharedResults;
    private bool mathArraysInitialized = false;
    
    /// <summary>
    /// Schedule a burst-compiled math operation job - ZERO GC VERSION
    /// </summary>
    public JobHandle ScheduleMathJob(float[] inputA, float[] inputB, float[] results, int operation, JobHandle dependency = default)
    {
        int count = math.min(inputA.Length, inputB.Length);
        if (count <= 0) return default;
        
        // FIXED: Initialize shared arrays once
        if (!mathArraysInitialized)
        {
            int maxSize = 256; // Fixed maximum size
            sharedInputA = new NativeArray<float>(maxSize, Allocator.Persistent);
            sharedInputB = new NativeArray<float>(maxSize, Allocator.Persistent);
            sharedResults = new NativeArray<float>(maxSize, Allocator.Persistent);
            mathArraysInitialized = true;
        }
        
        // Clamp count to shared array size
        count = math.min(count, sharedInputA.Length);
        
        // Copy data to shared arrays
        for (int i = 0; i < count; i++)
        {
            sharedInputA[i] = inputA[i];
            sharedInputB[i] = inputB[i];
        }
        
        var mathJob = new _BurstCompiler.MathJob
        {
            inputA = sharedInputA.GetSubArray(0, count),
            inputB = sharedInputB.GetSubArray(0, count),
            results = sharedResults.GetSubArray(0, count),
            operation = operation
        };
        
        JobHandle handle = mathJob.Schedule(count, 128, dependency);
        
        // Copy results back immediately after job completion
        handle = JobHandle.CombineDependencies(handle, ScheduleCopyResultsJob(sharedResults, results, count, handle));
        
        // Zero GC - add to fixed array instead of List
        if (activeJobCount < activeJobs.Length) {
            activeJobs[activeJobCount] = handle;
            activeJobCount++;
        }
        totalJobsScheduled++;
        
        return handle;
    }
    
    /// <summary>
    /// Get results from boid calculation - ZERO GC
    /// </summary>
    public void GetBoidResults(Vector3[] outVelocities, int count)
    {
        for (int i = 0; i < count && i < outVelocities.Length; i++)
        {
            float3 vel = boidNewVelocities[i];
            // ZERO GC: Reuse existing Vector3 instead of creating new
            outVelocities[i].Set(vel.x, vel.y, vel.z);
        }
    }
    
    /// <summary>
    /// Get positions from physics calculation - ZERO GC
    /// </summary>
    public void GetPhysicsResults(Vector3[] outPositions, int count)
    {
        for (int i = 0; i < count && i < outPositions.Length; i++)
        {
            float3 pos = boidPositions[i];
            // ZERO GC: Reuse existing Vector3 instead of creating new
            outPositions[i].Set(pos.x, pos.y, pos.z);
        }
    }
    
    /// <summary>
    /// Complete all active jobs and clean up
    /// </summary>
    public void CompleteAllJobs()
    {
        // Zero GC - use for loop instead of foreach
        for (int i = 0; i < activeJobCount; i++)
        {
            if (!activeJobs[i].IsCompleted)
            {
                activeJobs[i].Complete();
            }
        }
        // Zero GC - reset count instead of Clear()
        activeJobCount = 0;
    }
    
    /// <summary>
    /// Check if a specific job is complete
    /// </summary>
    public bool IsJobComplete(JobHandle handle)
    {
        return handle.IsCompleted;
    }
    
    /// <summary>
    /// Get job system statistics
    /// </summary>
    public void GetStats(out int scheduled, out int completed, out int active, out float avgTime)
    {
        scheduled = totalJobsScheduled;
        completed = totalJobsCompleted;
        active = activeJobCount;
        avgTime = averageJobTime;
    }
    
    /// <summary>
    /// Set world bounds for collision detection
    /// </summary>
    public void SetWorldBounds(Vector3 min, Vector3 max)
    {
        worldBoundsMin = new float3(min.x, min.y, min.z);
        worldBoundsMax = new float3(max.x, max.y, max.z);
    }
    
    private JobHandle ScheduleCopyResultsJob(NativeArray<float> source, float[] destination, int count, JobHandle dependency)
    {
        // FIXED: Create a proper copy job to avoid main thread blocking
        var copyJob = new CopyResultsJob
        {
            source = source.GetSubArray(0, count),
            destination = destination,
            count = count
        };
        
        return copyJob.Schedule(dependency);
    }
    
    /// <summary>
    /// Job to copy results from NativeArray to managed array - REMOVED BURST for managed array access
    /// </summary>
    private struct CopyResultsJob : IJob
    {
        [ReadOnly] public NativeArray<float> source;
        [WriteOnly] public float[] destination;
        [ReadOnly] public int count;
        
        public void Execute()
        {
            for (int i = 0; i < count && i < destination.Length; i++)
            {
                destination[i] = source[i];
            }
        }
    }
    
    private System.Collections.IEnumerator ProcessJobsCoroutine()
    {
        while (true)
        {
            // Zero GC - clean up completed jobs using array
            for (int i = activeJobCount - 1; i >= 0; i--)
            {
                if (activeJobs[i].IsCompleted)
                {
                    // Move last job to current position (swap and remove)
                    activeJobs[i] = activeJobs[activeJobCount - 1];
                    activeJobCount--;
                    totalJobsCompleted++;
                }
            }
            
            // Limit the number of concurrent jobs
            while (activeJobCount > maxConcurrentJobs)
            {
                yield return null;
            }
            
            yield return null; // Wait one frame
        }
    }
    
    void OnDestroy()
    {
        // Complete all jobs before destroying
        CompleteAllJobs();
        
        // FIXED: Dispose all native arrays including shared ones
        if (boidPositions.IsCreated) boidPositions.Dispose();
        if (boidVelocities.IsCreated) boidVelocities.Dispose();
        if (boidNewVelocities.IsCreated) boidNewVelocities.Dispose();
        if (boidRadii.IsCreated) boidRadii.Dispose();
        
        // Dispose shared math arrays
        if (mathArraysInitialized)
        {
            if (sharedInputA.IsCreated) sharedInputA.Dispose();
            if (sharedInputB.IsCreated) sharedInputB.Dispose();
            if (sharedResults.IsCreated) sharedResults.Dispose();
            mathArraysInitialized = false;
        }
    }
    
    void OnApplicationQuit()
    {
        CompleteAllJobs();
    }
}
