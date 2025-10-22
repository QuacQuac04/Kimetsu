using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float Horizontal { get { return (snapX) ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x; } }
    public float Vertical { get { return (snapY) ? SnapFloat(input.y, AxisOptions.Vertical) : input.y; } }
    public Vector2 Direction { get { return new Vector2(Horizontal, Vertical); } }

    public float HandleRange
    {
        get { return handleRange; }
        set { handleRange = Mathf.Abs(value); }
    }

    public float DeadZone
    {
        get { return deadZone; }
        set { deadZone = Mathf.Abs(value); }
    }

    public AxisOptions AxisOptions { get { return AxisOptions; } set { axisOptions = value; } }
    public bool SnapX { get { return snapX; } set { snapX = value; } }
    public bool SnapY { get { return snapY; } set { snapY = value; } }

    [SerializeField] private float handleRange = 1;
    [SerializeField] private float deadZone = 0;
    [SerializeField] private AxisOptions axisOptions = AxisOptions.Both;
    [SerializeField] private bool snapX = false;
    [SerializeField] private bool snapY = false;

    [Header("Dynamic Joystick Settings")]
    [SerializeField] private bool isDynamic = true; // Joystick di chuyen theo ngon tay
    [SerializeField] private RectTransform movementArea = null; // Vung gioi han di chuyen
    [SerializeField] private float moveThreshold = 1f; // Khoang cach toi thieu de joystick di chuyen

    [SerializeField] protected RectTransform background = null;
    [SerializeField] private RectTransform handle = null;
    private RectTransform baseRect = null;

    private Canvas canvas;
    private Camera cam;

    private Vector2 input = Vector2.zero;
    private Vector2 rawInput = Vector2.zero; // For handle visual position
    private Vector2 joystickStartPosition; // Vi tri ban dau cua joystick

    protected virtual void Start()
    {
        HandleRange = handleRange;
        DeadZone = deadZone;
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("The Joystick is not placed inside a canvas");

        Vector2 center = new Vector2(0.5f, 0.5f);
        background.pivot = center;
        handle.anchorMin = center;
        handle.anchorMax = center;
        handle.pivot = center;
        handle.anchoredPosition = Vector2.zero;
        
        // Neu khong gan movementArea, dung chinh baseRect
        if (movementArea == null)
        {
            movementArea = baseRect;
        }
        else
        {
            // Dam bao movementArea co pivot o tam (0.5, 0.5)
            movementArea.pivot = center;
        }
        
        // Neu la dynamic joystick, bat dau o tam movementArea
        if (isDynamic)
        {
            background.anchoredPosition = Vector2.zero;
            joystickStartPosition = Vector2.zero;
        }
        else
        {
            // Luu vi tri ban dau cua joystick
            joystickStartPosition = background.anchoredPosition;
        }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
        
        // Neu la dynamic joystick, di chuyen background den vi tri cham
        if (isDynamic)
        {
            Vector2 touchPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                movementArea, 
                eventData.position, 
                cam, 
                out touchPos
            );
            
            // Gioi han joystick trong vung movementArea
            background.anchoredPosition = ClampToArea(touchPos);
        }
        
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;

        Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        Vector2 radius = background.sizeDelta / 2;
        rawInput = (eventData.position - position) / (radius * canvas.scaleFactor);
        
        // Clamp rawInput for handle display
        float magnitude = rawInput.magnitude;
        
        // Neu vuot qua moveThreshold va isDynamic, di chuyen joystick
        if (isDynamic && magnitude > moveThreshold)
        {
            Vector2 difference = rawInput.normalized * (magnitude - moveThreshold) * radius;
            Vector2 newPos = background.anchoredPosition + difference;
            background.anchoredPosition = ClampToArea(newPos);
            
            // Tinh lai position sau khi di chuyen
            position = RectTransformUtility.WorldToScreenPoint(cam, background.position);
            rawInput = (eventData.position - position) / (radius * canvas.scaleFactor);
            magnitude = rawInput.magnitude;
        }
        
        if (magnitude > 1f)
            rawInput = rawInput.normalized;
        
        // Calculate movement input (always normalized when moving)
        input = rawInput;
        FormatInput();
        HandleInput(magnitude, rawInput.normalized, radius, cam);
        
        // Handle follows finger smoothly
        handle.anchoredPosition = rawInput * radius * handleRange;
    }
    
    private Vector2 ClampToArea(Vector2 position)
    {
        // Tinh ban kinh cua background (joystick)
        float joystickRadius = background.sizeDelta.x / 2f;
        
        // Tinh ban kinh cua movementArea
        float areaRadius = movementArea.rect.width / 2f;
        
        // Tinh khoang cach gioi han (areaRadius - joystickRadius)
        float maxDistance = areaRadius - joystickRadius;
        
        // Tinh khoang cach tu tam movementArea den vi tri mong muon
        float distance = position.magnitude;
        
        // Neu vuot qua gioi han, clamp lai
        if (distance > maxDistance)
        {
            position = position.normalized * maxDistance;
        }
        
        return position;
    }

    protected virtual void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (magnitude > deadZone)
        {
            // Always normalize for full speed movement
            input = normalised;
        }
        else
            input = Vector2.zero;
    }

    private void FormatInput()
    {
        if (axisOptions == AxisOptions.Horizontal)
            input = new Vector2(input.x, 0f);
        else if (axisOptions == AxisOptions.Vertical)
            input = new Vector2(0f, input.y);
    }

    private float SnapFloat(float value, AxisOptions snapAxis)
    {
        if (value == 0)
            return value;

        if (axisOptions == AxisOptions.Both)
        {
            float angle = Vector2.Angle(input, Vector2.up);
            if (snapAxis == AxisOptions.Horizontal)
            {
                if (angle < 22.5f || angle > 157.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            else if (snapAxis == AxisOptions.Vertical)
            {
                if (angle > 67.5f && angle < 112.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            return value;
        }
        else
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
        }
        return 0;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        rawInput = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        
        // Neu la dynamic joystick, reset ve vi tri ban dau
        if (isDynamic)
        {
            background.anchoredPosition = joystickStartPosition;
        }
    }

    protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        Vector2 localPoint = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out localPoint))
        {
            Vector2 pivotOffset = baseRect.pivot * baseRect.sizeDelta;
            return localPoint - (background.anchorMax * baseRect.sizeDelta) + pivotOffset;
        }
        return Vector2.zero;
    }
}

public enum AxisOptions { Both, Horizontal, Vertical }