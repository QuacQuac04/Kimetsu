using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIComponentConverter : EditorWindow
{
    private GameObject selectedObject;
    private string newType = "Button";

    [MenuItem("Tools/UI Converter")]
    public static void ShowWindow()
    {
        GetWindow<UIComponentConverter>("UI Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chuyển đổi UI Component", EditorStyles.boldLabel);

        selectedObject = (GameObject)EditorGUILayout.ObjectField("Đối tượng UI:", selectedObject, typeof(GameObject), true);
        newType = EditorGUILayout.TextField("Loại mới:", newType);

        if (GUILayout.Button("Chuyển đổi"))
        {
            if (selectedObject != null)
                ConvertComponent(selectedObject, newType);
            else
                Debug.LogWarning("Hãy chọn một đối tượng UI!");
        }
    }

    private void ConvertComponent(GameObject obj, string type)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("Đối tượng không phải là UI element.");
            return;
        }

        Vector3 pos = rect.localPosition;
        Vector2 size = rect.sizeDelta;
        Vector2 anchorMin = rect.anchorMin;
        Vector2 anchorMax = rect.anchorMax;
        Vector2 pivot = rect.pivot;

        // Xóa component cũ
        var oldImg = obj.GetComponent<Image>();
        var oldText = obj.GetComponent<TextMeshProUGUI>();
        var oldBtn = obj.GetComponent<Button>();

        if (oldImg) DestroyImmediate(oldImg);
        if (oldText) DestroyImmediate(oldText);
        if (oldBtn) DestroyImmediate(oldBtn);

        // Thêm component mới
        switch (type.ToLower())
        {
            case "button":
                var btnImg = obj.AddComponent<Image>();
                var btn = obj.AddComponent<Button>();
                obj.name = "_btn_" + obj.name.Replace("_img_", "");
                break;

            case "text":
                var tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.text = "New Text";
                obj.name = "_txt_" + obj.name.Replace("_img_", "");
                break;

            case "panel":
                var panelImg = obj.AddComponent<Image>();
                obj.name = "_panel_" + obj.name.Replace("_img_", "");
                break;

            case "toggle":
                var toggle = obj.AddComponent<Toggle>();
                var bg = obj.AddComponent<Image>();
                obj.name = "_toggle_" + obj.name.Replace("_img_", "");
                break;

            case "image":
                var img = obj.AddComponent<Image>();
                obj.name = "_img_" + obj.name.Replace("_btn_", "").Replace("_txt_", "").Replace("_panel_", "").Replace("_toggle_", "");
                break;

            case "inputfield":
                var inputField = obj.AddComponent<TMP_InputField>();
                var inputImg = obj.AddComponent<Image>();
                obj.name = "_inputfield_" + obj.name.Replace("_img_", "");
                break;

            case "scrollview":
                var scrollRect = obj.AddComponent<ScrollRect>();
                var scrollImg = obj.AddComponent<Image>();
                obj.name = "_scrollview_" + obj.name.Replace("_img_", "");
                break;


            case "slider":
                var slider = obj.AddComponent<Slider>();
                var sliderImg = obj.AddComponent<Image>();
                obj.name = "_slider_" + obj.name.Replace("_img_", "");
                break;

            case "dropdown":
                var dropdown = obj.AddComponent<TMP_Dropdown>();
                var dropdownImg = obj.AddComponent<Image>();
                obj.name = "_dropdown_" + obj.name.Replace("_img_", "");
                break;

            case "scrollbar":
                var scrollbar = obj.AddComponent<Scrollbar>();
                var scrollbarImg = obj.AddComponent<Image>();
                obj.name = "_scrollbar_" + obj.name.Replace("_img_", "");
                break;

            case "mask":
                var mask = obj.AddComponent<Mask>();
                var maskImg = obj.AddComponent<Image>();
                obj.name = "_mask_" + obj.name.Replace("_img_", "");
                break;

            case "canvas":
                var canvas = obj.AddComponent<Canvas>();
                var canvasScaler = obj.AddComponent<CanvasScaler>();
                var graphicRaycaster = obj.AddComponent<GraphicRaycaster>();
                obj.name = "_canvas_" + obj.name.Replace("_img_", "");
                break;
        }

        // Khôi phục lại transform
        rect.localPosition = pos;
        rect.sizeDelta = size;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;

        Debug.Log($"Đã chuyển {obj.name} sang loại {type}");
    }
}
