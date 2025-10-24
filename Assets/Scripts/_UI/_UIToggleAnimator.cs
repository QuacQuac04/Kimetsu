using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class _UIToggleAnimator : MonoBehaviour
{
    public enum SlideDirection { Up, Down, Left, Right }
    [Header("Setup doi tuong UI can thu nho, mo rong")]
    public RectTransform targetPanel;

    [Header("Nut an Toogle")]
    public Button toggleButton;

    [Header("Xuay nut Button truc Z {Khi thu}")]
    public float rotationButton = 180f;

    [Header("Huong truot")]
    public SlideDirection direction = SlideDirection.Down;


    [Header("Khoang cach truot")]
    public float slideDistance = 200f;

    [Header("THoi gian truot a ")]
    public float slideDuration = 0.3f;

    [Header("An them cac phan tu con tuy chon")]
    public List<GameObject> extraObjectsToHide;
    private bool isExpanded = true;
    private Vector2 originalPos;
    private Quaternion originalRotation;

    private void Start()
    {
        if (targetPanel == null)
        {
            return;
        }

        originalPos = targetPanel.anchoredPosition;

        if (toggleButton != null)
        {
            originalRotation = toggleButton.transform.rotation;
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    public void TogglePanel()
    {
        StopAllCoroutines();

        Vector2 targetPos = isExpanded ? GetCollapsedPosition() : originalPos;
        
        // Neu dang mo ra (isExpanded = false), hien extraObjects TRUOC
        if (!isExpanded)
        {
            foreach (var obj in extraObjectsToHide)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
        
        StartCoroutine(SlideTo(targetPos));
        StartCoroutine(RotationButton(isExpanded));
        
        // Neu dang thu vao (isExpanded = true), an extraObjects SAU khi animation xong
        if (isExpanded)
        {
            StartCoroutine(HideExtraObjectsAfterDelay());
        }
        
        isExpanded = !isExpanded;
    }
    
    private IEnumerator HideExtraObjectsAfterDelay()
    {
        // Doi animation chay xong
        yield return new WaitForSeconds(slideDuration);
        
        // Roi moi an cac extra objects
        foreach (var obj in extraObjectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private Vector2 GetCollapsedPosition()
    {
        switch (direction)
        {
            case SlideDirection.Up: return originalPos + new Vector2(0, slideDistance);
            case SlideDirection.Down: return originalPos - new Vector2(0, slideDistance);
            case SlideDirection.Left: return originalPos - new Vector2(slideDistance, 0);
            case SlideDirection.Right: return originalPos + new Vector2(slideDistance, 0);
            default: return originalPos;
        }
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = targetPanel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideDuration);
            targetPanel.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        targetPanel.anchoredPosition = target;
    }

    private IEnumerator RotationButton(bool collapse)
    {
        if (toggleButton == null) yield break;
        float elapsed = 0f;
        Quaternion startRot = toggleButton.transform.rotation;
        Quaternion targetRot = collapse
            ? Quaternion.Euler(0, 0, rotationButton)
            : originalRotation;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideDuration);
            toggleButton.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        toggleButton.transform.rotation = targetRot;
    }
}

