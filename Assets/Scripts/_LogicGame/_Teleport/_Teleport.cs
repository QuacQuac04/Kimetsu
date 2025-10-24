using UnityEngine;
using UnityEngine.SceneManagement;

public class _Teleport : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên scene cần load (phải có trong Build Settings)")]
    [SerializeField] private string sceneName;
    
    [Header("Optional Settings")]
    [Tooltip("Tag của player (mặc định là 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Delay trước khi load scene (giây)")]
    [SerializeField] private float loadDelay = 0f;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem object va chạm có tag Player không
        if (other.CompareTag(playerTag))
        {
            // Load scene
            LoadScene();
        }
    }

    // Nếu game của bạn là 2D, uncomment phần này và comment OnTriggerEnter ở trên
    /*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }
    */

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
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
        SceneManager.LoadScene(sceneName);
    }
}
