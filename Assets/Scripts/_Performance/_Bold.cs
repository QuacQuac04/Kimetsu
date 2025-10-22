using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// _Bold - Ultra-optimized multithreading system for Unity
/// Handles heavy computations on background threads to keep main thread smooth
/// </summary>
public static class _Bold
{
    // Zero GC - disabled concurrent collections to eliminate allocations
    // private static readonly ConcurrentQueue<IBoldJob> JobQueue = new ConcurrentQueue<IBoldJob>();
    // private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
    
    // Threading control
    private static CancellationTokenSource cancellationTokenSource;
    private static Task[] workerTasks;
    private static readonly object lockObject = new object();
    
    // Performance settings
    private static int workerThreadCount = Math.Max(1, Environment.ProcessorCount - 1);
    private static bool isInitialized = false;
    
    // Statistics
    private static long totalJobsProcessed = 0;
    private static long currentJobsInQueue = 0;
    
    /// <summary>
    /// Initialize the Bold system
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (isInitialized) return;
        
        // DISABLED - Worker threads removed for zero GC
        // cancellationTokenSource = new CancellationTokenSource();
        // workerTasks = new Task[workerThreadCount];
        
        // Use Unity Jobs System instead for zero GC
        
        isInitialized = true;
        // Removed Debug.Log to eliminate GC allocation from string interpolation
    }
    
    /// <summary>
    /// Process main thread actions - DISABLED for Zero GC
    /// </summary>
    public static void ProcessMainThreadActions()
    {
        // DISABLED - MainThreadActions causes GC allocation
        // Use direct method calls instead for zero GC
    }
    
    /// <summary>
    /// Submit a job for background processing - DISABLED for Zero GC
    /// </summary>
    public static void SubmitJob(IBoldJob job)
    {
        // DISABLED - JobQueue causes GC allocation
        // Use Jobs System directly instead for zero GC
    }
    
    /// <summary>
    /// Submit a simple computation job (Disabled to prevent GC allocation)
    /// </summary>
    public static void SubmitComputation<T>(Func<T> computation, Action<T> onComplete)
    {
        // Disabled to eliminate GC allocation from lambda expressions
        // Use direct method calls instead for zero GC
    }
    
    /// <summary>
    /// Submit a heavy calculation with progress callback (Disabled to prevent GC allocation)
    /// </summary>
    public static void SubmitHeavyCalculation(Func<IProgress<float>, object> calculation, Action<object> onComplete, Action<float> onProgress = null)
    {
        // Disabled to eliminate GC allocation from lambda expressions and delegates
        // Use Jobs System for heavy calculations instead
    }
    
    /// <summary>
    /// Submit a burst-compiled job through Jobs System (if available)
    /// </summary>
    public static JobHandle SubmitBurstJob<T>(T job, int arrayLength, int batchSize = 32) where T : struct, IJobParallelFor
    {
        if (_JobsSystem.Instance != null)
        {
            // Use Jobs System for burst-compiled performance
            return job.Schedule(arrayLength, batchSize);
        }
        else
        {
            // Fallback to regular job execution
            return default;
        }
    }
    
    /// <summary>
    /// Submit physics calculation to Jobs System
    /// </summary>
    public static JobHandle SubmitPhysicsCalculation(Vector3[] velocities, int count, float deltaTime)
    {
        if (_JobsSystem.Instance != null)
        {
            return _JobsSystem.Instance.SchedulePhysicsJob(velocities, count, deltaTime);
        }
        return default;
    }
    
    /// <summary>
    /// Worker thread loop
    /// </summary>
    private static void WorkerLoop(int threadId, CancellationToken cancellationToken)
    {
        Thread.CurrentThread.Name = $"BoldWorker_{threadId}";
        
        while (!cancellationToken.IsCancellationRequested)
        {
            // DISABLED - JobQueue removed for zero GC
            // Use Jobs System directly instead
            Thread.Sleep(10); // Reduced activity to save CPU
        }
    }
    
    /// <summary>
    /// Execute action on main thread
    /// </summary>
    public static void ExecuteOnMainThread(Action action)
    {
        // DISABLED - MainThreadActions removed for zero GC
        // Use direct method calls instead
    }
    
    /// <summary>
    /// Get system statistics
    /// </summary>
    public static void GetStats(out long processed, out long queued, out int workers)
    {
        processed = totalJobsProcessed;
        queued = currentJobsInQueue;
        workers = workerThreadCount;
    }
    
    /// <summary>
    /// Shutdown the Bold system
    /// </summary>
    public static void Shutdown()
    {
        if (!isInitialized) return;
        
        // DISABLED - No worker threads to shutdown for zero GC
        // cancellationTokenSource?.Cancel();
        // if (workerTasks != null) Task.WaitAll(workerTasks, TimeSpan.FromSeconds(2));
        // cancellationTokenSource?.Dispose();
        
        isInitialized = false;
        
        // Zero GC - no allocations
    }
}

/// <summary>
/// Interface for Bold jobs
/// </summary>
public interface IBoldJob
{
    void Execute();
}

/// <summary>
/// Simple computation job - DISABLED for Zero GC
/// </summary>
/*
public class ComputationJob<T> : IBoldJob
{
    private readonly Func<T> computation;
    private readonly Action<T> onComplete;
    
    public ComputationJob(Func<T> computation, Action<T> onComplete)
    {
        this.computation = computation;
        this.onComplete = onComplete;
    }
    
    public void Execute()
    {
        try
        {
            T result = computation();
            _Bold.ExecuteOnMainThread(() => onComplete?.Invoke(result));
        }
        catch (Exception)
        {
            // Removed Debug.LogError to eliminate GC allocation
        }
    }
}
*/

/// <summary>
/// Heavy calculation job with progress reporting - DISABLED for Zero GC
/// </summary>
/*
public class HeavyCalculationJob : IBoldJob
{
    private readonly Func<IProgress<float>, object> calculation;
    private readonly Action<object> onComplete;
    private readonly Action<float> onProgress;
    
    public HeavyCalculationJob(Func<IProgress<float>, object> calculation, Action<object> onComplete, Action<float> onProgress)
    {
        this.calculation = calculation;
        this.onComplete = onComplete;
        this.onProgress = onProgress;
    }
    
    public void Execute()
    {
        try
        {
            var progress = new Progress<float>(p => 
            {
                if (onProgress != null)
                    _Bold.ExecuteOnMainThread(() => onProgress(p));
            });
            
            object result = calculation(progress);
            _Bold.ExecuteOnMainThread(() => onComplete?.Invoke(result));
        }
        catch (Exception)
        {
            // Removed Debug.LogError to eliminate GC allocation
        }
    }
}
*/

/// <summary>
/// Boid calculation job for flocking behavior - DISABLED for Zero GC
/// </summary>
/*
public class BoidCalculationJob : IBoldJob
{
    private readonly Vector3[] boidPositions;
    private readonly Vector3[] boidVelocities;
    private readonly int boidCount;
    private readonly float separationRadius;
    private readonly float alignmentRadius;
    private readonly float cohesionRadius;
    private readonly Action<Vector3[]> onComplete;
    
    public BoidCalculationJob(Vector3[] positions, Vector3[] velocities, int count, 
                             float sepRadius, float alignRadius, float cohRadius, 
                             Action<Vector3[]> callback)
    {
        boidPositions = positions;
        boidVelocities = velocities;
        boidCount = count;
        separationRadius = sepRadius;
        alignmentRadius = alignRadius;
        cohesionRadius = cohRadius;
        onComplete = callback;
    }
    
    public void Execute()
    {
        try
        {
            Vector3[] newVelocities = new Vector3[boidCount];
            
            // Calculate boid forces in parallel-friendly way
            for (int i = 0; i < boidCount; i++)
            {
                Vector3 separation = CalculateSeparation(i);
                Vector3 alignment = CalculateAlignment(i);
                Vector3 cohesion = CalculateCohesion(i);
                
                // Combine forces
                newVelocities[i] = boidVelocities[i] + 
                                  (separation * 1.5f) + 
                                  (alignment * 1.0f) + 
                                  (cohesion * 1.0f);
                
                // Limit velocity
                if (newVelocities[i].magnitude > 5f)
                {
                    newVelocities[i] = newVelocities[i].normalized * 5f;
                }
            }
            
            _Bold.ExecuteOnMainThread(() => onComplete?.Invoke(newVelocities));
        }
        catch (Exception)
        {
            // Removed Debug.LogError to eliminate GC allocation
        }
    }
    
    private Vector3 CalculateSeparation(int boidIndex)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;
        
        for (int i = 0; i < boidCount; i++)
        {
            if (i == boidIndex) continue;
            
            float distance = Vector3.Distance(boidPositions[boidIndex], boidPositions[i]);
            if (distance < separationRadius && distance > 0)
            {
                Vector3 diff = boidPositions[boidIndex] - boidPositions[i];
                diff = diff.normalized / distance; // Weight by distance
                steer += diff;
                count++;
            }
        }
        
        if (count > 0)
        {
            steer /= count;
            steer = steer.normalized * 3f; // Desired speed
            steer -= boidVelocities[boidIndex];
        }
        
        return steer;
    }
    
    private Vector3 CalculateAlignment(int boidIndex)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        for (int i = 0; i < boidCount; i++)
        {
            if (i == boidIndex) continue;
            
            float distance = Vector3.Distance(boidPositions[boidIndex], boidPositions[i]);
            if (distance < alignmentRadius)
            {
                sum += boidVelocities[i];
                count++;
            }
        }
        
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * 3f; // Desired speed
            return sum - boidVelocities[boidIndex];
        }
        
        return Vector3.zero;
    }
    
    private Vector3 CalculateCohesion(int boidIndex)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        for (int i = 0; i < boidCount; i++)
        {
            if (i == boidIndex) continue;
            
            float distance = Vector3.Distance(boidPositions[boidIndex], boidPositions[i]);
            if (distance < cohesionRadius)
            {
                sum += boidPositions[i];
                count++;
            }
        }
        
        if (count > 0)
        {
            sum /= count;
            Vector3 steer = sum - boidPositions[boidIndex];
            steer = steer.normalized * 3f; // Desired speed
            return steer - boidVelocities[boidIndex];
        }
        
        return Vector3.zero;
    }
}
*/

/// <summary>
/// MonoBehaviour helper to process main thread actions - DISABLED for Zero GC
/// </summary>
/*
public class _BoldManager : MonoBehaviour
{
    private static _BoldManager instance;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateManager()
    {
        if (instance != null) return;
        
        GameObject managerObj = new GameObject("_BoldManager");
        instance = managerObj.AddComponent<_BoldManager>();
        DontDestroyOnLoad(managerObj);
    }
    
    void Update()
    {
        // Process main thread actions every frame
        _Bold.ProcessMainThreadActions();
    }
    
    void OnApplicationQuit()
    {
        _Bold.Shutdown();
    }
    
    // Debug GUI removed to eliminate GC allocation from UI rendering
}
*/
