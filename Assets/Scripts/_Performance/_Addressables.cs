using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Threading.Tasks; // OPTIMIZATION: Multithreading support
using Unity.Jobs; // JOBSYSTEM: Required for Jobs
using Unity.Collections; // JOBSYSTEM: Required for NativeArrays
using Unity.Mathematics; // JOBSYSTEM: Required for math types
using Unity.Burst; // JOBSYSTEM: Required for Burst compilation

public class _Addressables : MonoBehaviour
{
}