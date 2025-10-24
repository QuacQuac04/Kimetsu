using UnityEngine;

/// <summary>
/// Component cho phép Player có thể được target (dùng trong chế độ PK)
/// Attach script này vào GameObject Player
/// </summary>
public class _PlayerTargetable : MonoBehaviour, ITargetable
{
    [Header("Thông tin hiển thị")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private int level = 1;

    [Header("Máu")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth = 1000f;

    [Header("Target Settings")]
    [SerializeField] private bool canBeTargeted = true; // Có thể bị target không

    #region ITargetable Implementation

    public string GetDisplayName()
    {
        return playerName;
    }

    public int GetLevel()
    {
        return level;
    }

    public Sprite GetAvatar()
    {
        return avatar;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public TargetType GetTargetType()
    {
        return TargetType.Player;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    public bool IsTargetable()
    {
        // Chỉ cho phép target nếu:
        // 1. canBeTargeted = true
        // 2. GameObject đang active
        // 3. Player đang sống
        return canBeTargeted && gameObject.activeInHierarchy && IsAlive();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set tên player
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    /// <summary>
    /// Set level
    /// </summary>
    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }

    /// <summary>
    /// Set avatar
    /// </summary>
    public void SetAvatar(Sprite newAvatar)
    {
        avatar = newAvatar;
    }

    /// <summary>
    /// Nhận sát thương
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// Hồi máu
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    /// <summary>
    /// Set có thể bị target không
    /// </summary>
    public void SetCanBeTargeted(bool value)
    {
        canBeTargeted = value;
    }

    /// <summary>
    /// Xử lý khi chết
    /// </summary>
    private void OnDeath()
    {
        // Thêm logic xử lý chết ở đây
    }

    #endregion

    #region Unity Callbacks

    void Start()
    {
        // Khởi tạo
        currentHealth = maxHealth;
    }

    // Debug: Hiển thị thanh máu trên đầu player (optional)
    void OnGUI()
    {
        if (!IsAlive()) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPos.z > 0)
        {
            // Hiển thị thanh máu đơn giản
            float barWidth = 100f;
            float barHeight = 10f;
            float healthPercent = currentHealth / maxHealth;

            Rect bgRect = new Rect(screenPos.x - barWidth / 2, Screen.height - screenPos.y - barHeight, barWidth, barHeight);
            Rect healthRect = new Rect(screenPos.x - barWidth / 2, Screen.height - screenPos.y - barHeight, barWidth * healthPercent, barHeight);

            GUI.color = Color.black;
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
            GUI.color = Color.green;
            GUI.DrawTexture(healthRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    #endregion
}
