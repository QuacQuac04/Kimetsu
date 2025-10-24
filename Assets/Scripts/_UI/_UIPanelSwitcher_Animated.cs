using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class _UIPanelSwitcher_Animated : MonoBehaviour
{
    [System.Serializable]
    public class ButtonPanelPair
    {
        [Tooltip("Nut de bam")]
        public Button button;
        
        [Tooltip("Panel tuong ung se hien thi khi bam nut")]
        public RectTransform panel;
        
        [Tooltip("Co active panel nay khi khoi dong khong?")]
        public bool activeOnStart = false;
    }

    [Header("Danh sach cac nut va panel")]
    public List<ButtonPanelPair> buttonPanelPairs = new List<ButtonPanelPair>();

    [Header("Hieu ung chuyen panel")]
    public bool useAnimation = true;
    public float fadeDuration = 0.3f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Tuy chon mau nut")]
    public bool changeButtonColor = true;
    public Color activeButtonColor = Color.white;
    public Color inactiveButtonColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Tuy chon am thanh (Optional)")]
    public AudioClip switchSound;
    private AudioSource audioSource;

    private int currentActiveIndex = -1;
    private bool isTransitioning = false;

    private void Start()
    {
        // Setup AudioSource neu co sound
        if (switchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Gan su kien cho cac nut
        for (int i = 0; i < buttonPanelPairs.Count; i++)
        {
            int index = i;
            ButtonPanelPair pair = buttonPanelPairs[i];
            
            if (pair.button != null)
            {
                pair.button.onClick.AddListener(() => SwitchToPanel(index));
            }

            // Setup CanvasGroup neu dung animation
            if (useAnimation && pair.panel != null)
            {
                if (pair.panel.GetComponent<CanvasGroup>() == null)
                {
                    pair.panel.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (pair.activeOnStart && currentActiveIndex == -1)
            {
                currentActiveIndex = index;
            }
        }

        if (currentActiveIndex == -1 && buttonPanelPairs.Count > 0)
        {
            currentActiveIndex = 0;
        }

        InitializePanels();
    }

    private void InitializePanels()
    {
        for (int i = 0; i < buttonPanelPairs.Count; i++)
        {
            ButtonPanelPair pair = buttonPanelPairs[i];
            bool isActive = (i == currentActiveIndex);

            if (pair.panel != null)
            {
                pair.panel.gameObject.SetActive(isActive);
                
                if (useAnimation)
                {
                    CanvasGroup cg = pair.panel.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = isActive ? 1f : 0f;
                    }
                }
            }

            UpdateButtonColor(i, isActive);
        }
    }

    public void SwitchToPanel(int index)
    {
        if (index < 0 || index >= buttonPanelPairs.Count)
        {
            return;
        }

        if (index == currentActiveIndex || isTransitioning)
        {
            return;
        }

        // Phat am thanh
        if (switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound);
        }

        if (useAnimation)
        {
            StartCoroutine(SwitchPanelWithAnimation(index));
        }
        else
        {
            SwitchPanelInstant(index);
        }
    }

    private void SwitchPanelInstant(int newIndex)
    {
        int oldIndex = currentActiveIndex;
        currentActiveIndex = newIndex;

        // Tat panel cu
        if (oldIndex >= 0 && oldIndex < buttonPanelPairs.Count)
        {
            if (buttonPanelPairs[oldIndex].panel != null)
            {
                buttonPanelPairs[oldIndex].panel.gameObject.SetActive(false);
            }
            UpdateButtonColor(oldIndex, false);
        }

        // Bat panel moi
        if (buttonPanelPairs[newIndex].panel != null)
        {
            buttonPanelPairs[newIndex].panel.gameObject.SetActive(true);
        }
        UpdateButtonColor(newIndex, true);
    }

    private IEnumerator SwitchPanelWithAnimation(int newIndex)
    {
        isTransitioning = true;
        int oldIndex = currentActiveIndex;
        currentActiveIndex = newIndex;

        RectTransform oldPanel = oldIndex >= 0 ? buttonPanelPairs[oldIndex].panel : null;
        RectTransform newPanel = buttonPanelPairs[newIndex].panel;

        CanvasGroup oldCG = oldPanel != null ? oldPanel.GetComponent<CanvasGroup>() : null;
        CanvasGroup newCG = newPanel != null ? newPanel.GetComponent<CanvasGroup>() : null;

        // Bat panel moi ngay (nhung trong suot)
        if (newPanel != null)
        {
            newPanel.gameObject.SetActive(true);
            if (newCG != null) newCG.alpha = 0f;
        }

        // Fade out panel cu + Fade in panel moi dong thoi
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (oldCG != null)
            {
                oldCG.alpha = fadeOutCurve.Evaluate(t);
            }

            if (newCG != null)
            {
                newCG.alpha = fadeInCurve.Evaluate(t);
            }

            yield return null;
        }

        // Hoan tat
        if (oldPanel != null)
        {
            oldPanel.gameObject.SetActive(false);
            if (oldCG != null) oldCG.alpha = 0f;
        }

        if (newCG != null)
        {
            newCG.alpha = 1f;
        }

        // Update mau nut
        if (oldIndex >= 0) UpdateButtonColor(oldIndex, false);
        UpdateButtonColor(newIndex, true);

        isTransitioning = false;
    }

    private void UpdateButtonColor(int index, bool isActive)
    {
        if (!changeButtonColor || index < 0 || index >= buttonPanelPairs.Count)
            return;

        Button btn = buttonPanelPairs[index].button;
        if (btn != null)
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = isActive ? activeButtonColor : inactiveButtonColor;
            btn.colors = colors;
        }
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

    public GameObject GetCurrentPanel()
    {
        if (currentActiveIndex >= 0 && currentActiveIndex < buttonPanelPairs.Count)
        {
            return buttonPanelPairs[currentActiveIndex].panel.gameObject;
        }
        return null;
    }

    public int GetCurrentIndex()
    {
        return currentActiveIndex;
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}
