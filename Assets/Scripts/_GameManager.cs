using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class _GameManager : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    // OPTIMIZED: Single method for all scene loading
    public void LoadScene(string sceneName, bool useLoading = false)
    {
        #if UNITY_EDITOR
        Debug.Log("[GameManager] Loading scene: " + sceneName);
        #endif
        
        if (useLoading && sceneName != "_UI_Loading")
        {
            _LoadingManager._nextScene = sceneName;
            PlayerPrefs.SetString("NextScene", sceneName);
            PlayerPrefs.Save();
            SceneManager.LoadScene("_UI_Loading");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    // Quick access methods for UI buttons
    public void LoadMap02() => LoadScene("_MAP_02");
    public void LoadMap08() => LoadScene("_MAP_08");
    
    // Debug method để kiểm tra scenes available
    [ContextMenu("Debug Available Scenes")]
    public void DebugAvailableScenes()
    {
        #if UNITY_EDITOR
        Debug.Log("=== AVAILABLE SCENES ===");
        Debug.Log("Current Scene: " + SceneManager.GetActiveScene().name);
        Debug.Log("Scene Count in Build: " + SceneManager.sceneCountInBuildSettings);
        #endif
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
#if UNITY_EDITOR
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log("Scene " + i + ": " + sceneName);
#endif
        }
        
        #if UNITY_EDITOR
        // Test specific scenes
        Debug.Log("Can load _MAP_02: " + Application.CanStreamedLevelBeLoaded("_MAP_02"));
        Debug.Log("Can load _MAP_08: " + Application.CanStreamedLevelBeLoaded("_MAP_08"));
        #endif
    }
    
    // OPTIMIZED: Single method for force loading with cleanup
    public void ForceLoadScene(string sceneName)
    {
        #if UNITY_EDITOR
        Debug.Log("[GameManager] Force loading: " + sceneName);
        #endif
        _LoadingManager._nextScene = "";
        System.GC.Collect();
        StartCoroutine(DelayedLoad(sceneName));
    }
    
    private System.Collections.IEnumerator DelayedLoad(string sceneName)
    {
        yield return null;
        LoadScene(sceneName);
    }
    
    // OPTIMIZED: Single Addressables loader with fallback
    public void LoadSceneAddressable(string sceneName)
    {
        #if UNITY_EDITOR
        Debug.Log("[GameManager] Loading " + sceneName + " via Addressables");
        #endif
        string[] addresses = { sceneName, "_" + sceneName, "Assets/Scenes/_MAP/" + sceneName + ".unity" };
        StartCoroutine(TryLoadAddressableScene(addresses, sceneName));
    }
    
    // OPTIMIZED: Streamlined Addressables loader with fallback
    private System.Collections.IEnumerator TryLoadAddressableScene(string[] addresses, string fallbackScene)
    {
        // ZERO GC: Use for loop instead of foreach to avoid iterator allocation
        for (int i = 0; i < addresses.Length; i++)
        {
            string address = addresses[i];
            var checkHandle = Addressables.LoadResourceLocationsAsync(address);
            yield return checkHandle;
            
            if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
            {
                var loadHandle = Addressables.LoadSceneAsync(address, LoadSceneMode.Single);
                yield return loadHandle;
                
                if (loadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    #if UNITY_EDITOR
                    Debug.Log("[GameManager] Loaded: " + address);
                    #endif
                    Addressables.Release(checkHandle);
                    yield break;
                }
            }
            Addressables.Release(checkHandle);
        } // end for
        
        #if UNITY_EDITOR
        Debug.LogWarning("[GameManager] Addressables failed, using SceneManager");
        #endif
        LoadScene(fallbackScene);
    }
    
    // REMOVED: Duplicate LoadAddressableScene method - using optimized version above
    
    // Method để debug tất cả addressables có sẵn
    public void DebugAddressables()
    {
        #if UNITY_EDITOR
        Debug.Log("[GameManager] Debugging available Addressables...");
        #endif
        StartCoroutine(ListAllAddressables());
    }
    
    private System.Collections.IEnumerator ListAllAddressables()
    {
        // Lấy tất cả locations
        var handle = Addressables.LoadResourceLocationsAsync("default");
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            #if UNITY_EDITOR
            Debug.Log("[GameManager] Found " + handle.Result.Count + " addressable resources:");
            // ZERO GC: Use for loop instead of foreach
            for (int i = 0; i < handle.Result.Count; i++)
            {
                Debug.Log("[GameManager] - " + handle.Result[i].PrimaryKey);
            }
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogError("[GameManager] Failed to get addressable locations");
            #endif
        }
        
        Addressables.Release(handle);
    }
}
