using UnityEngine;
using UnityEngine.UI;
using TMPro; // 👈 thêm namespace TMP

public class _Enemy : MonoBehaviour, ITargetable
{
    [Header("Thông tin hiển thị (có thể chỉnh trong Inspector)")]
    [SerializeField] private string bossDisplayName = "Enemy"; // 👈 Tên có thể sửa trong Inspector
    public Sprite avater;
    public int level = 1;

    [Header("Target Settings")]
    [SerializeField] private TargetType targetType = TargetType.Enemy; // Loại mục tiêu
    [SerializeField] private bool isTargetable = true; // Có thể target được không

    [Header("UI Canvas References")]
    public TextMeshProUGUI txt_NameBoss; // Tên boss
    public TextMeshProUGUI txt_HP_Bar; // Chỉ số máu
    public TextMeshProUGUI txt_level_boss; // Level boss
    public Image avatar_Boss; // Avatar boss

    [Header("Thanh máu của Enemy")]
    public Image healthBar;

    [Header("Máu Enemy")]
    public float maxHealth = 100f;
    public float currenHealth;

    [Header("Thông tin sát thương và di chuyển của Enemy")]
    public float attackDamage = 0f;
    public float moveSpeed = 0f;

    [Header("Thành phần Enemy")]
    public Rigidbody2D RB2D;

    protected virtual void Awake()
    {
        RB2D = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        currenHealth = maxHealth;
        UpdateUI();
    }

    // Phương thức cập nhật toàn bộ UI
    public virtual void UpdateUI()
    {
        UpdateHealthBar();
        UpdateNameDisplay();
        UpdateLevelDisplay();
        UpdateAvatarDisplay();
    }

    // Cập nhật tên boss
    public virtual void UpdateNameDisplay()
    {
        if (txt_NameBoss != null)
        {
            txt_NameBoss.text = bossDisplayName;
        }
    }

    // Cập nhật level boss
    public virtual void UpdateLevelDisplay()
    {
        if (txt_level_boss != null)
        {
            txt_level_boss.text = "Lv." + level.ToString();
        }
    }

    // Cập nhật avatar boss
    public virtual void UpdateAvatarDisplay()
    {
        if (avatar_Boss != null && avater != null)
        {
            avatar_Boss.sprite = avater;
        }
    }

    // Getter để lấy tên boss
    public string GetBossName()
    {
        return bossDisplayName;
    }

    // Setter để đổi tên boss (nếu cần runtime)
    public void SetBossName(string newName)
    {
        bossDisplayName = newName;
        UpdateNameDisplay();
    }

    public virtual void TakeDame(float damage)
    {
        currenHealth -= damage;
        if (currenHealth < 0) currenHealth = 0;

        UpdateHealthBar();

        if (currenHealth <= 0)
        {
            Die();
        }
    }

    public virtual void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currenHealth / maxHealth;
        }

        // Cập nhật text hiển thị số máu
        if (txt_HP_Bar != null)
        {
            txt_HP_Bar.text = Mathf.RoundToInt(currenHealth).ToString("N0") + " / " + Mathf.RoundToInt(maxHealth).ToString("N0");
        }
    }

    public virtual void Die()
    {
        Destroy(gameObject);
    }

    #region ITargetable Implementation

    public string GetDisplayName()
    {
        return bossDisplayName;
    }

    public int GetLevel()
    {
        return level;
    }

    public Sprite GetAvatar()
    {
        return avater;
    }

    public float GetCurrentHealth()
    {
        return currenHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public TargetType GetTargetType()
    {
        return targetType;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public bool IsAlive()
    {
        return currenHealth > 0;
    }

    public bool IsTargetable()
    {
        return isTargetable && gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Set loại target (Enemy, Boss, NPC, etc.)
    /// </summary>
    public void SetTargetType(TargetType type)
    {
        targetType = type;
    }

    /// <summary>
    /// Set có thể target được hay không
    /// </summary>
    public void SetTargetable(bool value)
    {
        isTargetable = value;
    }

    #endregion
}
