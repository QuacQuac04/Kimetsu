using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script don gian de mo UI nhanh, chi can keo tha vao Button
/// Khong can setup gi phuc tap, chi can gan targetUI
/// </summary>
public class _QuickOpenUI : MonoBehaviour
{
    [Header("UI can mo")]
    public GameObject targetUI;

    [Header("Che do")]
    [Tooltip("Mo UI nay va dong tat ca UI khac")]
    public bool openExclusive = false;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OpenTargetUI);
        }
    }

    public void OpenTargetUI()
    {
        if (targetUI == null)
        {
            return;
        }

        if (openExclusive)
        {
            // Dong tat ca cac UI khac dang mo
            _QuickOpenUI[] allQuickUI = FindObjectsOfType<_QuickOpenUI>();
            foreach (var quickUI in allQuickUI)
            {
                if (quickUI.targetUI != null && quickUI.targetUI != targetUI && quickUI.targetUI.activeSelf)
                {
                    quickUI.targetUI.SetActive(false);
                }
            }
        }

        // Mo UI
        if (!targetUI.activeSelf)
        {
            targetUI.SetActive(true);
        }
    }
}
