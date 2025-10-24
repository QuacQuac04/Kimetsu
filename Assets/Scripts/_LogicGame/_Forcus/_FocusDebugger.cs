using UnityEngine;

/// <summary>
/// Debug tool để test hệ thống Focus
/// Attach vào GameObject nào đó để debug
/// </summary>
public class _FocusDebugger : MonoBehaviour
{
    [Header("Debug UI")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private int fontSize = 20;

    [Header("Test Keys")]
    [SerializeField] private KeyCode focusNextKey = KeyCode.Tab;
    [SerializeField] private KeyCode togglePKModeKey = KeyCode.P;

    private GUIStyle style;

    void Start()
    {
        // Tạo style cho debug text
        style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
    }

    void Update()
    {
        // Test Focus Next bằng phím Tab
        if (Input.GetKeyDown(focusNextKey))
        {
            if (_Focus.Instance != null)
            {
                _Focus.Instance.FocusNextTarget();
            }
        }

        // Toggle PK Mode bằng phím P
        if (Input.GetKeyDown(togglePKModeKey))
        {
            if (_Focus.Instance != null)
            {
                bool currentPK = false; // Cần thêm getter trong _Focus nếu muốn lấy giá trị hiện tại
                _Focus.Instance.SetPKMode(!currentPK);
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo || _Focus.Instance == null) return;

        string debugText = "=== FOCUS DEBUG INFO ===\n\n";

        ITargetable currentTarget = _Focus.Instance.GetCurrentTarget();

        if (currentTarget != null)
        {
            debugText += $"Target Name: {currentTarget.GetDisplayName()}\n";
            debugText += $"Target Type: {currentTarget.GetTargetType()}\n";
            debugText += $"Target Level: {currentTarget.GetLevel()}\n";
            debugText += $"Target HP: {currentTarget.GetCurrentHealth():F0} / {currentTarget.GetMaxHealth():F0}\n";
            debugText += $"Target Alive: {currentTarget.IsAlive()}\n";
            debugText += $"Target Position: {currentTarget.GetTransform().position}\n";

            // Tính khoảng cách đến player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(player.transform.position, currentTarget.GetTransform().position);
                debugText += $"Distance to Player: {distance:F2}\n";
            }
        }
        else
        {
            debugText += "No Target Selected\n";
        }

        debugText += "\n=== CONTROLS ===\n";
        debugText += $"[{focusNextKey}] - Focus Next Target\n";
        debugText += $"[{togglePKModeKey}] - Toggle PK Mode\n";
        debugText += "[Click] - Select Target at Position\n";

        // Vẽ text lên màn hình
        GUI.Label(new Rect(10, 10, 500, 500), debugText, style);

        // Vẽ khung quanh target hiện tại (nếu có)
        if (currentTarget != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(currentTarget.GetTransform().position);
            if (screenPos.z > 0)
            {
                float size = 30f;
                Rect rect = new Rect(screenPos.x - size / 2, Screen.height - screenPos.y - size / 2, size, size);
                
                GUI.color = Color.red;
                GUI.Box(rect, "");
                GUI.color = Color.white;
            }
        }
    }
}
