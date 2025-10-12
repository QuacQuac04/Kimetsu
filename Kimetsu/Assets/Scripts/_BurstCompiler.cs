using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// _BurstCompiler - Ultra-fast compiled jobs for maximum performance
/// Uses Burst compiler for native C++ performance in Unity
/// </summary>
public static class _BurstCompiler
{
    /// <summary>
    /// Burst-compiled boid calculation job - runs at native C++ speed
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BoidJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<float3> velocities;
        [ReadOnly] public float separationRadius;
        [ReadOnly] public float alignmentRadius;
        [ReadOnly] public float cohesionRadius;
        [ReadOnly] public float maxSpeed;
        
        public NativeArray<float3> newVelocities;
        
        public void Execute(int index)
        {
            float3 position = positions[index];
            float3 velocity = velocities[index];
            
            float3 separation = CalculateSeparation(index, position);
            float3 alignment = CalculateAlignment(index, position);
            float3 cohesion = CalculateCohesion(index, position);
            
            // Combine forces with weights
            float3 newVelocity = velocity + 
                               (separation * 1.5f) + 
                               (alignment * 1.0f) + 
                               (cohesion * 1.0f);
            
            // Limit speed
            float speed = math.length(newVelocity);
            if (speed > maxSpeed)
            {
                newVelocity = math.normalize(newVelocity) * maxSpeed;
            }
            
            newVelocities[index] = newVelocity;
        }
        
        private float3 CalculateSeparation(int index, float3 position)
        {
            float3 steer = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index) continue;
                
                float distance = math.distance(position, positions[i]);
                if (distance < separationRadius && distance > 0)
                {
                    float3 diff = position - positions[i];
                    diff = math.normalize(diff) / distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer = math.normalize(steer) * 3f; // Desired speed
                steer -= velocities[index];
            }
            
            return steer;
        }
        
        private float3 CalculateAlignment(int index, float3 position)
        {
            float3 sum = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index) continue;
                
                float distance = math.distance(position, positions[i]);
                if (distance < alignmentRadius)
                {
                    sum += velocities[i];
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                sum = math.normalize(sum) * 3f; // Desired speed
                return sum - velocities[index];
            }
            
            return float3.zero;
        }
        
        private float3 CalculateCohesion(int index, float3 position)
        {
            float3 sum = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index) continue;
                
                float distance = math.distance(position, positions[i]);
                if (distance < cohesionRadius)
                {
                    sum += positions[i];
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                float3 steer = sum - position;
                steer = math.normalize(steer) * 3f; // Desired speed
                return steer - velocities[index];
            }
            
            return float3.zero;
        }
    }
    
    /// <summary>
    /// Burst-compiled physics calculation job
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct PhysicsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> velocities;
        [ReadOnly] public float deltaTime;
        
        public NativeArray<float3> positions;
        
        public void Execute(int index)
        {
            positions[index] += velocities[index] * deltaTime;
        }
    }
    
    /// <summary>
    /// Burst-compiled collision detection job
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct CollisionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<float> radii;
        [ReadOnly] public float3 boundsMin;
        [ReadOnly] public float3 boundsMax;
        
        public NativeArray<float3> velocities;
        
        public void Execute(int index)
        {
            float3 pos = positions[index];
            float3 vel = velocities[index];
            float radius = radii[index];
            
            // Boundary collision
            if (pos.x - radius < boundsMin.x || pos.x + radius > boundsMax.x)
            {
                vel.x = -vel.x;
            }
            if (pos.y - radius < boundsMin.y || pos.y + radius > boundsMax.y)
            {
                vel.y = -vel.y;
            }
            if (pos.z - radius < boundsMin.z || pos.z + radius > boundsMax.z)
            {
                vel.z = -vel.z;
            }
            
            velocities[index] = vel;
        }
    }
    
    /// <summary>
    /// Burst-compiled math operations job
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct MathJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> inputA;
        [ReadOnly] public NativeArray<float> inputB;
        [ReadOnly] public int operation; // 0=add, 1=multiply, 2=sin, 3=cos
        
        public NativeArray<float> results;
        
        public void Execute(int index)
        {
            float a = inputA[index];
            float b = inputB[index];
            
            switch (operation)
            {
                case 0: // Add
                    results[index] = a + b;
                    break;
                case 1: // Multiply
                    results[index] = a * b;
                    break;
                case 2: // Sin
                    results[index] = math.sin(a);
                    break;
                case 3: // Cos
                    results[index] = math.cos(a);
                    break;
                default:
                    results[index] = a;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Burst-compiled sorting job for performance-critical operations
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct SortJob : IJob
    {
        public NativeArray<float> values;
        
        public void Execute()
        {
            // Burst-optimized quicksort
            QuickSort(0, values.Length - 1);
        }
        
        private void QuickSort(int low, int high)
        {
            if (low < high)
            {
                int pi = Partition(low, high);
                QuickSort(low, pi - 1);
                QuickSort(pi + 1, high);
            }
        }
        
        private int Partition(int low, int high)
        {
            float pivot = values[high];
            int i = low - 1;
            
            for (int j = low; j < high; j++)
            {
                if (values[j] < pivot)
                {
                    i++;
                    Swap(i, j);
                }
            }
            
            Swap(i + 1, high);
            return i + 1;
        }
        
        private void Swap(int i, int j)
        {
            float temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
    }
}
