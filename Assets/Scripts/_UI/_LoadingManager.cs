using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Spine.Unity;
public class _LoadingManager : MonoBehaviour
{
    public static string _nextScene = "_MAP_02";
    public GameObject _loadingBar;
    public Text _textLoading;
    
    [Header("Player Animation")]
    public SkeletonAnimation _playerSpine; // Spine SkeletonAnimation component
    public RectTransform _loadingBarRect; // RectTransform của thanh loading để tính toán vị trí
    
    [Header("Spine Animation Settings")]
    public string _runAnimationName = "Run"; // Tên animation chạy
    public string _idleAnimationName = "Idle"; // Tên animation đứng yên
    
    private float _loadingProgressTime = 20f;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    
    // ZERO GC: Pre-allocated WaitForSeconds
    private static readonly WaitForSeconds cachedWait = new WaitForSeconds(0.5f);
    
    // ZERO GC: Cache components to avoid GetComponent in loops
    private Image loadingBarImage;
    private int lastPercentage = -1;

    private void Start()
    {
        // ZERO GC: Cache Image component
        if (_loadingBar != null)
        {
            loadingBarImage = _loadingBar.GetComponent<Image>();
        }
        
        // FIX: Check PlayerPrefs in case static variable was reset
        if (PlayerPrefs.HasKey("NextScene"))
        {
            string savedScene = PlayerPrefs.GetString("NextScene");
            #if UNITY_EDITOR
            Debug.Log("[LoadingManager] Found saved scene in PlayerPrefs: " + savedScene);
            #endif
            _nextScene = savedScene;
            PlayerPrefs.DeleteKey("NextScene"); // Clear after reading
        }
        
        #if UNITY_EDITOR
        // Debug log để kiểm tra scene sẽ được load
        Debug.Log("[LoadingManager] Next scene to load: " + _nextScene);
        #endif
        
        InitializePlayerAnimation();
        StartCoroutine(LoadSceneWithDelay(_nextScene));
    }
    
    private void InitializePlayerAnimation()
    {
        if (_playerSpine == null || _loadingBarRect == null)
            return;
        
        // Tính toán vị trí dựa trên thanh loading
        _startPosition = _loadingBarRect.position;
        _startPosition.x -= _loadingBarRect.rect.width * 0.5f * _loadingBarRect.lossyScale.x;
        
        _endPosition = _loadingBarRect.position;
        _endPosition.x += _loadingBarRect.rect.width * 0.5f * _loadingBarRect.lossyScale.x;
        
        // Đặt player ở vị trí bắt đầu
        _playerSpine.transform.position = _startPosition;
        
        // Bắt đầu animation chạy
        if (_playerSpine.state != null && !string.IsNullOrEmpty(_runAnimationName))
        {
            _playerSpine.state.SetAnimation(0, _runAnimationName, true);
        }
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // ZERO GC: Use cached component
            if (loadingBarImage != null)
            {
                loadingBarImage.fillAmount = progress;
            }
            
            // ZERO GC: Avoid ToString every frame
            int percentage = (int)(progress * 100f);
            if (percentage != lastPercentage)
            {
                lastPercentage = percentage;
                _textLoading.text = percentage + "%";
            }
            
            yield return null;
        }
    }

    public IEnumerator LoadSceneWithDelay(string sceneName)
    {
        float elapsedTime = 0f;
        while (elapsedTime < _loadingProgressTime)
        {
            float progress = Mathf.Clamp01(elapsedTime / _loadingProgressTime);
            
            // Cập nhật thanh loading - ZERO GC: Use cached component
            if (loadingBarImage != null)
            {
                loadingBarImage.fillAmount = progress;
            }
            
            // ZERO GC: Avoid ToString every frame
            int percentage = (int)(progress * 100f);
            if (percentage != lastPercentage)
            {
                lastPercentage = percentage;
                _textLoading.text = percentage + "%";
            }
            
            // Animate player chạy theo progress
            AnimatePlayer(progress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Chuyển sang animation idle trước khi load scene
        OnLoadingComplete();
        
        // Đợi một chút để player dừng lại - ZERO GC: Use cached WaitForSeconds
        yield return cachedWait;
        
        #if UNITY_EDITOR
        // Debug log trước khi load scene cuối cùng
        Debug.Log("[LoadingManager] Actually loading scene: " + sceneName);
        #endif
        
        SceneManager.LoadScene(sceneName);
    }
    
    private void AnimatePlayer(float progress)
    {
        if (_playerSpine != null)
        {
            // Di chuyển player từ start đến end position theo progress
            Vector3 currentPosition = Vector3.Lerp(_startPosition, _endPosition, progress);
            _playerSpine.transform.position = currentPosition;
            
            // Đảm bảo player visible
            if (!_playerSpine.gameObject.activeInHierarchy)
            {
                _playerSpine.gameObject.SetActive(true);
            }
            
            // Điều chỉnh hướng player (flip nếu cần)
            if (progress > 0)
            {
                // Hướng phải - không flip
                _playerSpine.skeleton.ScaleX = Mathf.Abs(_playerSpine.skeleton.ScaleX);
            }
            
            // Điều chỉnh tốc độ animation dựa trên progress
            if (_playerSpine.state != null)
            {
                // Tăng tốc độ animation khi gần hoàn thành
                float animationSpeed = Mathf.Lerp(0.8f, 1.5f, progress);
                _playerSpine.state.TimeScale = animationSpeed;
            }
        }
    }
    
    // Method để chuyển animation khi loading hoàn thành
    private void OnLoadingComplete()
    {
        if (_playerSpine != null && _playerSpine.state != null && !string.IsNullOrEmpty(_idleAnimationName))
        {
            _playerSpine.state.SetAnimation(0, _idleAnimationName, true);
        }
    }
}

