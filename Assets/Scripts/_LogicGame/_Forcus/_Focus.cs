using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Hệ thống Focus & Target - Quản lý việc chọn và theo dõi mục tiêu
/// </summary>
public class _Focus : MonoBehaviour
{
    public static _Focus Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float autoChangeDistance = 100f; // Khoảng cách tự đổi target
    [SerializeField] private bool isPKMode = false; // Chế độ PK
    
    [Header("Screen Bounds Detection")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float screenBoundsBuffer = 2f; // Buffer để check off-screen

    [Header("References")]
    [SerializeField] private Transform playerTransform; // Transform của player
    [SerializeField] private _BossInfoUI bossInfoUI; // UI hiển thị thông tin boss

    // Target hiện tại
    private ITargetable currentTarget;
    private bool isManualTarget = false; // Target được chọn thủ công hay tự động

    // Danh sách các target có thể chọn
    private List<ITargetable> allTargets = new List<ITargetable>();
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.2f; // Cập nhật mỗi 0.2s

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động tìm camera nếu chưa có
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Tự động tìm player nếu chưa có
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void Update()
    {
        // Cập nhật target list định kỳ
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateTargetList();
            lastUpdateTime = Time.time;
        }

        // Kiểm tra target hiện tại
        if (currentTarget != null)
        {
            // Nếu target chết hoặc không còn targetable
            if (!currentTarget.IsAlive() || !currentTarget.IsTargetable())
            {
                OnTargetDied();
                return;
            }

            // Nếu là target tự động
            if (!isManualTarget)
            {
                // Check khoảng cách để tự đổi target
                float distance = Vector3.Distance(playerTransform.position, currentTarget.GetTransform().position);
                if (distance > autoChangeDistance)
                {
                    SelectNearestTarget();
                }
            }
            else
            {
                // Nếu là target thủ công, check xem có chạy khỏi màn hình không
                if (IsTargetOffScreen(currentTarget))
                {
                    // Đổi sang target gần nhất
                    isManualTarget = false;
                    SelectNearestTarget();
                }
            }
        }
        else
        {
            // Không có target, tự động chọn
            SelectNearestTarget();
        }
    }

    #region Target Management

    /// <summary>
    /// Cập nhật danh sách target có thể chọn
    /// </summary>
    private void UpdateTargetList()
    {
        allTargets.Clear();

        // Tìm tất cả các MonoBehaviour implement ITargetable
        MonoBehaviour[] allObjects = FindObjectsOfType<MonoBehaviour>();
        foreach (var obj in allObjects)
        {
            if (obj is ITargetable targetable)
            {
                if (targetable.IsAlive() && targetable.IsTargetable())
                {
                    allTargets.Add(targetable);
                }
            }
        }
    }

    /// <summary>
    /// Chọn target gần nhất
    /// </summary>
    private void SelectNearestTarget()
    {
        if (playerTransform == null) return;

        ITargetable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in allTargets)
        {
            // Nếu là chế độ hòa bình, không chọn người chơi
            if (!isPKMode && target.GetTargetType() == TargetType.Player)
                continue;

            float distance = Vector3.Distance(playerTransform.position, target.GetTransform().position);
            
            // Ưu tiên theo thứ tự: NPC > Boss > Enemy > Item > Player
            int priority = GetTargetPriority(target.GetTargetType());
            float weightedDistance = distance / priority;

            if (weightedDistance < nearestDistance)
            {
                nearestDistance = weightedDistance;
                nearest = target;
            }
        }

        SetTarget(nearest, false);
    }

    /// <summary>
    /// Set target hiện tại
    /// </summary>
    private void SetTarget(ITargetable target, bool manual)
    {
        currentTarget = target;
        isManualTarget = manual;

        // Cập nhật UI
        if (bossInfoUI != null && target != null)
        {
            bossInfoUI.UpdateTargetInfo(target);
            bossInfoUI.Show();
        }
        else if (bossInfoUI != null)
        {
            bossInfoUI.Hide();
        }
    }

    /// <summary>
    /// Check xem target có nằm ngoài màn hình không
    /// </summary>
    private bool IsTargetOffScreen(ITargetable target)
    {
        if (mainCamera == null || target == null) return false;

        Vector3 screenPoint = mainCamera.WorldToViewportPoint(target.GetTransform().position);
        
        return screenPoint.x < -screenBoundsBuffer || screenPoint.x > 1 + screenBoundsBuffer ||
               screenPoint.y < -screenBoundsBuffer || screenPoint.y > 1 + screenBoundsBuffer;
    }

    /// <summary>
    /// Lấy độ ưu tiên của target type (số càng cao càng ưu tiên)
    /// </summary>
    private int GetTargetPriority(TargetType type)
    {
        switch (type)
        {
            case TargetType.NPC: return 10;
            case TargetType.Boss: return 8;
            case TargetType.Enemy: return 6;
            case TargetType.Item: return 4;
            case TargetType.Player: return 2;
            default: return 1;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Click vào một vị trí để chọn target
    /// </summary>
    public void SelectTargetAtPosition(Vector3 worldPosition, float radius = 1f)
    {
        ITargetable selected = null;
        float nearestDistance = float.MaxValue;
        bool hasNPC = false;

        // Tìm tất cả target trong bán kính
        List<ITargetable> targetsInRange = new List<ITargetable>();
        foreach (var target in allTargets)
        {
            float distance = Vector3.Distance(worldPosition, target.GetTransform().position);
            if (distance <= radius)
            {
                targetsInRange.Add(target);
                if (target.GetTargetType() == TargetType.NPC)
                    hasNPC = true;
            }
        }

        // Ưu tiên NPC nếu có
        if (hasNPC)
        {
            foreach (var target in targetsInRange)
            {
                if (target.GetTargetType() == TargetType.NPC)
                {
                    float distance = Vector3.Distance(worldPosition, target.GetTransform().position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        selected = target;
                    }
                }
            }
        }
        else
        {
            // Không có NPC, chọn gần vị trí click nhất
            foreach (var target in targetsInRange)
            {
                float distance = Vector3.Distance(worldPosition, target.GetTransform().position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    selected = target;
                }
            }
        }

        if (selected != null)
        {
            SetTarget(selected, true); // Manual target
        }
    }

    /// <summary>
    /// Button Focus - Đổi qua target tiếp theo
    /// </summary>
    public void FocusNextTarget()
    {
        if (playerTransform == null || allTargets.Count == 0) return;

        // Lấy danh sách targets sắp xếp theo khoảng cách
        var sortedTargets = allTargets
            .Where(t => IsTargetInScreen(t)) // Chỉ lấy target trong màn hình
            .OrderBy(t => Vector3.Distance(playerTransform.position, t.GetTransform().position))
            .ToList();

        if (sortedTargets.Count == 0)
        {
            // Không có target trong màn hình, lặp lại từ đầu
            SetTarget(null, false);
            return;
        }

        // Tìm index của target hiện tại
        int currentIndex = -1;
        if (currentTarget != null)
        {
            currentIndex = sortedTargets.IndexOf(currentTarget);
        }

        // Chọn target tiếp theo
        int nextIndex = (currentIndex + 1) % sortedTargets.Count;
        SetTarget(sortedTargets[nextIndex], true);
    }

    /// <summary>
    /// Check xem target có trong màn hình không
    /// </summary>
    private bool IsTargetInScreen(ITargetable target)
    {
        if (mainCamera == null || target == null) return false;

        Vector3 screenPoint = mainCamera.WorldToViewportPoint(target.GetTransform().position);
        return screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1 && screenPoint.z > 0;
    }

    /// <summary>
    /// Xử lý khi target chết
    /// </summary>
    private void OnTargetDied()
    {
        if (currentTarget == null) return;

        TargetType deadTargetType = currentTarget.GetTargetType();

        // Nếu là người chơi chết, giữ target
        if (deadTargetType == TargetType.Player)
        {
            // Vẫn giữ target vào người chơi đó (có thể để xem thông tin)
            return;
        }

        // Ưu tiên chọn target mới theo thứ tự: Boss/Enemy > Item > NPC
        ITargetable nextTarget = null;
        float nearestDistance = float.MaxValue;

        // Tìm Boss/Enemy gần nhất
        foreach (var target in allTargets)
        {
            if (target.GetTargetType() == TargetType.Boss || target.GetTargetType() == TargetType.Enemy)
            {
                float distance = Vector3.Distance(playerTransform.position, target.GetTransform().position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nextTarget = target;
                }
            }
        }

        // Nếu không có Boss/Enemy, tìm Item
        if (nextTarget == null)
        {
            nearestDistance = float.MaxValue;
            foreach (var target in allTargets)
            {
                if (target.GetTargetType() == TargetType.Item)
                {
                    float distance = Vector3.Distance(playerTransform.position, target.GetTransform().position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nextTarget = target;
                    }
                }
            }
        }

        // Nếu không có Item, tìm NPC
        if (nextTarget == null)
        {
            nearestDistance = float.MaxValue;
            foreach (var target in allTargets)
            {
                if (target.GetTargetType() == TargetType.NPC)
                {
                    float distance = Vector3.Distance(playerTransform.position, target.GetTransform().position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nextTarget = target;
                    }
                }
            }
        }

        SetTarget(nextTarget, false);
    }

    /// <summary>
    /// Set chế độ PK
    /// </summary>
    public void SetPKMode(bool enabled)
    {
        isPKMode = enabled;
        
        // Nếu đang target người chơi mà tắt PK mode, đổi target
        if (!enabled && currentTarget != null && currentTarget.GetTargetType() == TargetType.Player)
        {
            SelectNearestTarget();
        }
    }

    /// <summary>
    /// Lấy target hiện tại
    /// </summary>
    public ITargetable GetCurrentTarget()
    {
        return currentTarget;
    }

    #endregion
}
