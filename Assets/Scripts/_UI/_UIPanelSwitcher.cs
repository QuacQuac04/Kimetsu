using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class _UIPanelSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ButtonPanelPair
    {
        [Tooltip("Nut de bam")]
        public Button button;
        
        [Tooltip("Panel tuong ung se hien thi khi bam nut")]
        public GameObject panel;
        
        [Tooltip("Co active panel nay khi khoi dong khong?")]
        public bool activeOnStart = false;
    }

    [Header("Danh sach cac nut va panel")]
    [Tooltip("Them cac cap button-panel vao day")]
    public List<ButtonPanelPair> buttonPanelPairs = new List<ButtonPanelPair>();

    [Header("Tuy chon")]
    [Tooltip("Mau cho nut dang active")]
    public Color activeButtonColor = Color.white;
    
    [Tooltip("Mau cho nut khong active")]
    public Color inactiveButtonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Tooltip("Co doi mau nut khong?")]
    public bool changeButtonColor = true;

    private int currentActiveIndex = -1;

    private void Start()
    {
        // Gan su kien cho cac nut
        for (int i = 0; i < buttonPanelPairs.Count; i++)
        {
            int index = i; // Capture index for closure
            ButtonPanelPair pair = buttonPanelPairs[i];
            
            if (pair.button != null)
            {
                pair.button.onClick.AddListener(() => SwitchToPanel(index));
            }

            // Kiem tra panel nao active luc khoi dong
            if (pair.activeOnStart && currentActiveIndex == -1)
            {
                currentActiveIndex = index;
            }
        }

        // Hien thi panel mac dinh
        if (currentActiveIndex == -1 && buttonPanelPairs.Count > 0)
        {
            currentActiveIndex = 0;
        }

        UpdatePanelVisibility();
    }

    public void SwitchToPanel(int index)
    {
        if (index < 0 || index >= buttonPanelPairs.Count)
        {
            return;
        }

        currentActiveIndex = index;
        UpdatePanelVisibility();
    }

    public void SwitchToPanelByName(string panelName)
    {
        for (int i = 0; i < buttonPanelPairs.Count; i++)
        {
            if (buttonPanelPairs[i].panel != null && 
                buttonPanelPairs[i].panel.name == panelName)
            {
                SwitchToPanel(i);
                return;
            }
        }
    }

    private void UpdatePanelVisibility()
    {
        for (int i = 0; i < buttonPanelPairs.Count; i++)
        {
            ButtonPanelPair pair = buttonPanelPairs[i];
            bool isActive = (i == currentActiveIndex);

            // An/hien panel
            if (pair.panel != null)
            {
                pair.panel.SetActive(isActive);
            }

            // Doi mau nut (neu co bat)
            if (changeButtonColor && pair.button != null)
            {
                ColorBlock colors = pair.button.colors;
                colors.normalColor = isActive ? activeButtonColor : inactiveButtonColor;
                pair.button.colors = colors;
            }
        }
    }

    // Ham tien ich de goi tu code khac
    public GameObject GetCurrentPanel()
    {
        if (currentActiveIndex >= 0 && currentActiveIndex < buttonPanelPairs.Count)
        {
            return buttonPanelPairs[currentActiveIndex].panel;
        }
        return null;
    }

    public int GetCurrentIndex()
    {
        return currentActiveIndex;
    }
}
