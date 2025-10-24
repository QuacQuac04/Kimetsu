using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script cho button Focus - Đổi mục tiêu
/// </summary>
[RequireComponent(typeof(Button))]
public class _FocusButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnFocusButtonClick);
    }

    private void OnFocusButtonClick()
    {
        if (_Focus.Instance != null)
        {
            _Focus.Instance.FocusNextTarget();
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnFocusButtonClick);
        }
    }
}
