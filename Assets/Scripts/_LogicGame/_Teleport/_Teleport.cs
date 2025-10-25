using UnityEngine;
using UnityEngine.SceneManagement;

public class _Teleport : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên scene đích (map muốn teleport đến)")]
    [SerializeField] private string targetSceneName;
    
    [Tooltip("Tên scene loading (mặc định: _UI_Loading_Map)")]
    [SerializeField] private string loadingSceneName = "_UI_Loading_Map";
    
    [Tooltip("Có hiển thị loading screen không?")]
    [SerializeField] private bool useLoadingScreen = true;
    
    [Header("Optional Settings")]
    [Tooltip("Tag của player (mặc định là 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Delay trước khi load scene (giây)")]
    [SerializeField] private float loadDelay = 0.5f;

    // Game 2D - sử dụng OnTriggerEnter2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem object va chạm có tag Player không
        if (other.CompareTag(playerTag))
        {
            // Load scene
            LoadScene();
        }
    }

    // Nếu game của bạn là 3D, uncomment phần này và comment OnTriggerEnter2D ở trên
    /*
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }
    */

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target Scene Name is empty!");
            return;
        }

        if (loadDelay > 0)
        {
            Invoke(nameof(LoadSceneDelayed), loadDelay);
        }
        else
        {
            LoadSceneDelayed();
        }
    }

    private void LoadSceneDelayed()
    {
        if (useLoadingScreen)
        {
            // Lưu map đích vào LoadingManager
            _LoadingManager._nextScene = targetSceneName;
            Debug.Log("Teleport: Set next scene = " + targetSceneName + ", loading scene = " + loadingSceneName);
            
            // Load scene loading
            SceneManager.LoadScene(loadingSceneName);
        }
        else
        {
            // Load trực tiếp không qua loading
            Debug.Log("Teleport: Load truc tiep den " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
