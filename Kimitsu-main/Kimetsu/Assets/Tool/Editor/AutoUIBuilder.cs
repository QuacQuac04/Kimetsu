using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;


public class AutoUIBuilder : EditorWindow
{
    private string folderPath = "Assets/UIAssets/MainMenu";
    [MenuItem("Tools/UI Auto Builder")]
    public static void ShowWindow()
    {
        GetWindow<AutoUIBuilder>("Auto UI Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Tự động tạo UI từ folder ảnh", EditorStyles.boldLabel);
        folderPath = EditorGUILayout.TextField("Thư mục:", folderPath);

        if(GUILayout.Button("Build UI"))
        {
            BuildUI();
        }
    }

    void BuildUI()
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Không tìm thấy thư mục:" + folderPath); return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if(canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        foreach(string file in Directory.GetFiles(folderPath, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(file);

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            Image img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(tex.width, tex.height);

            if (name.EndsWith("_btn"))
            {
                Button btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
            }
            else if (name.EndsWith("_img"))
            {
                Button btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
            }
            else if (name.EndsWith("_txt"))
            {
                DestroyImmediate(img);
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = name.Replace("_txt", "");
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 35;
                tmp.color = Color.white;
            }

            go.transform.localPosition = Vector3.zero;
        }
        Debug.Log("Hoàn thành tạo UI từ folder:" + folderPath);
    }
}
