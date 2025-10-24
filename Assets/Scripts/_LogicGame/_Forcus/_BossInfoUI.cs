using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Manager cho thông tin Boss/Target
/// </summary>
public class _BossInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootPanel; // Panel chính để show/hide
    
    [Header("Text Components")]
    [SerializeField] private TextMeshProUGUI txt_NameBoss; // Tên boss
    [SerializeField] private TextMeshProUGUI txt_HP_Bar; // Chỉ số máu (format: 999,999 / 999,999)
    [SerializeField] private TextMeshProUGUI txt_level_boss; // Level (format: Lv.999)
    
    [Header("Image Components")]
    [SerializeField] private Image img_HP_Bar; // Thanh máu (fill amount)
    [SerializeField] private Image img_avatar_Boss; // Avatar boss

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float fadeSpeed = 5f;
    
    private CanvasGroup canvasGroup;
    private ITargetable currentTarget;
    private bool isVisible = false;

    void Awake()
    {
        // Tự động tìm CanvasGroup hoặc thêm mới
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && useAnimation)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Ẩn panel lúc start
        Hide();
    }

    void Update()
    {
        // Cập nhật UI liên tục nếu có target
        if (currentTarget != null && isVisible)
        {
            UpdateHealthBar();
        }

        // Animation fade
        if (useAnimation && canvasGroup != null)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    /// <summary>
    /// Cập nhật thông tin target mới
    /// </summary>
    public void UpdateTargetInfo(ITargetable target)
    {
        if (target == null)
        {
            Hide();
            return;
        }

        currentTarget = target;

        // Cập nhật tên
        if (txt_NameBoss != null)
        {
            txt_NameBoss.text = target.GetDisplayName();
        }

        // Cập nhật level
        if (txt_level_boss != null)
        {
            txt_level_boss.text = "Lv." + target.GetLevel().ToString();
        }

        // Cập nhật avatar
        if (img_avatar_Boss != null && target.GetAvatar() != null)
        {
            img_avatar_Boss.sprite = target.GetAvatar();
        }

        // Cập nhật máu
        UpdateHealthBar();

        Show();
    }

    /// <summary>
    /// Cập nhật thanh máu
    /// </summary>
    private void UpdateHealthBar()
    {
        if (currentTarget == null) return;

        float currentHP = currentTarget.GetCurrentHealth();
        float maxHP = currentTarget.GetMaxHealth();

        // Cập nhật fill amount
        if (img_HP_Bar != null)
        {
            img_HP_Bar.fillAmount = maxHP > 0 ? currentHP / maxHP : 0f;
        }

        // Cập nhật text số máu
        if (txt_HP_Bar != null)
        {
            txt_HP_Bar.text = FormatNumber(currentHP) + " / " + FormatNumber(maxHP);
        }
    }

    /// <summary>
    /// Format số với dấu phẩy ngăn cách hàng nghìn
    /// </summary>
    private string FormatNumber(float number)
    {
        return Mathf.RoundToInt(number).ToString("N0");
    }

    /// <summary>
    /// Hiển thị UI
    /// </summary>
    public void Show()
    {
        isVisible = true;
        
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Ẩn UI
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        currentTarget = null;

        if (!useAnimation)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Nếu dùng animation, sẽ fade out
            // Có thể thêm logic disable sau khi fade out hoàn toàn
        }
    }

    /// <summary>
    /// Kiểm tra xem UI có đang hiển thị không
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
}
