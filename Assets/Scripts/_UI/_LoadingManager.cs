using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class _LoadingManager : MonoBehaviour
{
    public static string _nextScene = "_UI_Create_igure";

    [Header("Loading Bar")]
    public Image _loadingBar;
    public Text _textLoading;

    [Header("Player Image")]
    public RectTransform _playerImage; // Image player (_img_Load1)
    public RectTransform _loadingBarRect; // RectTransform của thanh loading để tính toán vị trí

    [Header("Settings")]
    public float _loadingProgressTime = 3f; // Thoi gian loading (giay)

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private int lastPercentage = -1;

    private void Start()
    {
        // Reset loading bar ve 0%
        if (_loadingBar != null)
        {
            _loadingBar.fillAmount = 0f;
        }

        if (_textLoading != null)
        {
            _textLoading.text = "0%";
        }

        // Tu dong bat dau loading khi scene duoc load (vi Teleport da set _nextScene)
        Debug.Log("LoadingManager Start - Next scene: " + _nextScene);
        StartLoading();
    }

    /// <summary>
    /// Goi method nay tu Button Dang Nhap de bat dau loading
    /// </summary>
    public void StartLoading()
    {
        Debug.Log("=== StartLoading() duoc goi! ===");
        
        if (_loadingBar == null)
        {
            Debug.LogError("_loadingBar is NULL!");
            return;
        }

        if (_textLoading == null)
        {
            Debug.LogError("_textLoading is NULL!");
            return;
        }
        
        Debug.Log("Bat dau loading den scene: " + _nextScene);
        Debug.Log("Thoi gian loading: " + _loadingProgressTime + " giay");
        
        InitializePlayerAnimation();
        StartCoroutine(LoadSceneWithDelay(_nextScene));
    }

    /// <summary>
    /// Goi method nay tu Button Dang Nhap de bat dau loading voi scene tu chon
    /// </summary>
    public void StartLoading(string sceneName)
    {
        _nextScene = sceneName;
        InitializePlayerAnimation();
        StartCoroutine(LoadSceneWithDelay(_nextScene));
    }

    private void InitializePlayerAnimation()
    {
        if (_playerImage == null || _loadingBarRect == null)
            return;

        // Tính toán vị trí dựa trên thanh loading
        _startPosition = _loadingBarRect.position;
        _startPosition.x -= _loadingBarRect.rect.width * 0.5f * _loadingBarRect.lossyScale.x;

        _endPosition = _loadingBarRect.position;
        _endPosition.x += _loadingBarRect.rect.width * 0.5f * _loadingBarRect.lossyScale.x;

        // Đặt player ở vị trí bắt đầu
        _playerImage.position = _startPosition;

        // Đảm bảo player visible
        _playerImage.gameObject.SetActive(true);
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Cap nhat loading bar
            if (_loadingBar != null)
            {
                _loadingBar.fillAmount = progress;
            }

            // Cap nhat text %
            int percentage = (int)(progress * 100f);
            if (percentage != lastPercentage && _textLoading != null)
            {
                lastPercentage = percentage;
                _textLoading.text = percentage + "%";
            }

            // Di chuyen player image
            AnimatePlayer(progress);

            yield return null;
        }
    }

    public IEnumerator LoadSceneWithDelay(string sceneName)
    {
        Debug.Log("LoadSceneWithDelay coroutine bat dau!");
        float elapsedTime = 0f;

        while (elapsedTime < _loadingProgressTime)
        {
            float progress = Mathf.Clamp01(elapsedTime / _loadingProgressTime);

            // Cap nhat loading bar
            if (_loadingBar != null)
            {
                _loadingBar.fillAmount = progress;
            }

            // Cap nhat text %
            int percentage = (int)(progress * 100f);
            if (percentage != lastPercentage && _textLoading != null)
            {
                lastPercentage = percentage;
                _textLoading.text = percentage + "%";
            }

            // Di chuyen player image
            AnimatePlayer(progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hoan thanh loading - 100%
        if (_loadingBar != null)
        {
            _loadingBar.fillAmount = 1f;
        }
        if (_textLoading != null)
        {
            _textLoading.text = "100%";
        }
        AnimatePlayer(1f);

        // Doi 0.5 giay roi load scene
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(sceneName);
    }

    private void AnimatePlayer(float progress)
    {
        if (_playerImage != null)
        {
            // Di chuyển player từ start đến end position theo progress
            Vector3 currentPosition = Vector3.Lerp(_startPosition, _endPosition, progress);
            _playerImage.position = currentPosition;
        }
    }
}

