using UnityEngine;
using TMPro;

public class _Info_Game : MonoBehaviour
{
    [Header("TextMeshPro References")]
    [SerializeField] private TextMeshProUGUI txt_PlayerPosition; // Hiển thị toạ độ X, Y, Z
    [SerializeField] private TextMeshProUGUI txt_FPS; // Hiển thị FPS

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // Transform của player
    [SerializeField] private bool autoFindPlayer = true; // Tự động tìm player nếu chưa gán

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Cập nhật mỗi 0.1 giây
    private float nextUpdateTime = 0f;

    [Header("FPS Settings")]
    [SerializeField] private bool showAverageFPS = true; // Hiển thị FPS trung bình
    private float deltaTime = 0f;
    private int frameCount = 0;
    private float fpsSum = 0f;

    [Header("Display Format")]
    [SerializeField] private string positionFormat = "Vị trí: X:{0:F1} Y:{1:F1} Z:{2:F1}";
    [SerializeField] private string fpsFormat = "FPS: {0:F0}";

    void Start()
    {
        // Tự động tìm player nếu chưa gán
        if (playerTransform == null && autoFindPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Thử tìm _Player component
                _Player playerComponent = FindObjectOfType<_Player>();
                if (playerComponent != null)
                {
                    playerTransform = playerComponent.transform;
                }
            }
        }
    }

    void Update()
    {
        // Tính FPS
        CalculateFPS();

        // Cập nhật UI theo interval để tối ưu performance
        if (Time.time >= nextUpdateTime)
        {
            UpdateUI();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    /// <summary>
    /// Tính toán FPS
    /// </summary>
    private void CalculateFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        if (showAverageFPS)
        {
            frameCount++;
            fpsSum += 1f / Time.unscaledDeltaTime;
        }
    }

    /// <summary>
    /// Cập nhật UI
    /// </summary>
    private void UpdateUI()
    {
        // Cập nhật toạ độ player
        UpdatePlayerPosition();

        // Cập nhật FPS
        UpdateFPS();
    }

    /// <summary>
    /// Cập nhật hiển thị toạ độ player
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (txt_PlayerPosition != null && playerTransform != null)
        {
            Vector3 pos = playerTransform.position;
            txt_PlayerPosition.text = string.Format(positionFormat, pos.x, pos.y, pos.z);
        }
        else if (txt_PlayerPosition != null && playerTransform == null)
        {
            txt_PlayerPosition.text = "Player not found!";
        }
    }

    /// <summary>
    /// Cập nhật hiển thị FPS
    /// </summary>
    private void UpdateFPS()
    {
        if (txt_FPS != null)
        {
            float fps;
            
            if (showAverageFPS && frameCount > 0)
            {
                fps = fpsSum / frameCount;
            }
            else
            {
                fps = 1f / deltaTime;
            }

            txt_FPS.text = string.Format(fpsFormat, fps);

            // Đổi màu text theo FPS (optional)
            UpdateFPSColor(fps);
        }
    }

    /// <summary>
    /// Đổi màu text FPS theo performance
    /// </summary>
    private void UpdateFPSColor(float fps)
    {
        if (txt_FPS == null) return;

        if (fps >= 60f)
        {
            txt_FPS.color = Color.green; // FPS tốt
        }
        else if (fps >= 30f)
        {
            txt_FPS.color = Color.yellow; // FPS trung bình
        }
        else
        {
            txt_FPS.color = Color.red; // FPS thấp
        }
    }

    /// <summary>
    /// Reset FPS counter
    /// </summary>
    public void ResetFPSCounter()
    {
        frameCount = 0;
        fpsSum = 0f;
    }

    /// <summary>
    /// Set player transform thủ công
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    /// <summary>
    /// Bật/tắt hiển thị toạ độ
    /// </summary>
    public void SetPositionVisible(bool visible)
    {
        if (txt_PlayerPosition != null)
        {
            txt_PlayerPosition.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Bật/tắt hiển thị FPS
    /// </summary>
    public void SetFPSVisible(bool visible)
    {
        if (txt_FPS != null)
        {
            txt_FPS.gameObject.SetActive(visible);
        }
    }
}
