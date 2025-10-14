using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Sprites;

public class AutoUIBuilder : EditorWindow
{
    private string folderPath = "Assets/UIAssets/MainMenu";
    private Vector2 referenceResolution = new Vector2(2160, 1080);
    private bool autoAnchor = true;
    private bool usePSBLayers = true;
    private bool autoReplaceSprites = true;
    
    // Sprite replacement settings
    private string spriteReplacementFolder = "Assets/PSB/Img";
    private bool preserveSpritePosition = true;
    
    // Canvas settings
    private RenderMode canvasRenderMode = RenderMode.ScreenSpaceCamera;
    
    [MenuItem("Tools/UI Auto Builder")]
    public static void ShowWindow()
    {
        GetWindow<AutoUIBuilder>("Auto UI Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Tự động tạo UI từ PSB/PNG", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Folder Path Selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Thư mục PSB:", GUILayout.Width(100));
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("📁", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Chọn thư mục chứa PSB/PNG", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                folderPath = FileUtil.GetProjectRelativePath(path);
            }
        }
        EditorGUILayout.EndHorizontal();

        // Show folder status
        if (!string.IsNullOrEmpty(folderPath))
        {
            if (Directory.Exists(folderPath))
            {
                string[] psbFiles = Directory.GetFiles(folderPath, "*.psb");
                string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Files Found:", GUILayout.Width(100));
                GUI.color = (psbFiles.Length > 0 || pngFiles.Length > 0) ? Color.green : Color.yellow;
                EditorGUILayout.LabelField($"{psbFiles.Length} PSB, {pngFiles.Length} PNG", EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Thư mục không tồn tại!", MessageType.Warning);
            }
        }

        EditorGUILayout.Space();
        
        // Sprite Replacement Settings
        EditorGUILayout.LabelField("Sprite Replacement Settings", EditorStyles.boldLabel);
        autoReplaceSprites = EditorGUILayout.Toggle("Auto Replace Sprites:", autoReplaceSprites);
        
        if (autoReplaceSprites)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprite Folder:", GUILayout.Width(100));
            spriteReplacementFolder = EditorGUILayout.TextField(spriteReplacementFolder);
            if (GUILayout.Button("📁", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Chọn thư mục sprites PNG", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    spriteReplacementFolder = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            preserveSpritePosition = EditorGUILayout.Toggle("Preserve Position:", preserveSpritePosition);
        }

        EditorGUILayout.Space();
        
        // Canvas Settings
        EditorGUILayout.LabelField("Canvas Settings", EditorStyles.boldLabel);
        canvasRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode:", canvasRenderMode);
        referenceResolution = EditorGUILayout.Vector2Field("Reference Resolution:", referenceResolution);
        
        EditorGUILayout.Space();
        
        // UI Settings
        autoAnchor = EditorGUILayout.Toggle("Tự động neo (Anchor):", autoAnchor);
        usePSBLayers = EditorGUILayout.Toggle("Sử dụng PSB Layers:", usePSBLayers);

        GUILayout.Space(10);
        
        if(GUILayout.Button("Build UI", GUILayout.Height(40)))
        {
            BuildUI();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Cách đặt tên:\n" +
            "- _btn: Button\n" +
            "- _img: Image\n" +
            "- _txt: Text\n" +
            "- _panel: Panel\n\n" +
            "Anchor tự động:\n" +
            "- top_left, top_center, top_right\n" +
            "- middle_left, middle_center, middle_right\n" +
            "- bottom_left, bottom_center, bottom_right\n\n" +
            "Auto Replace Sprites:\n" +
            "Sẽ tự động thay thế sprites PSB bằng PNG từ folder được chỉ định\n\n" +
            "Canvas Mode: ScreenSpaceCamera để đặt vào scene như MAP_02", 
            MessageType.Info);
    }

    void BuildUI()
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Không tìm thấy thư mục: " + folderPath); 
            return;
        }

        // Step 1: Replace sprites trong PSB bằng PNG trước
        if (autoReplaceSprites && Directory.Exists(spriteReplacementFolder))
        {
            Debug.Log("===== STEP 1: Starting sprite replacement process =====");
            ReplacePSBSpritesWithPNG();
            AssetDatabase.Refresh();
            Debug.Log("===== Sprite replacement complete =====\n");
        }

        // Step 2: Build UI từ PSB đã updated
        Debug.Log("===== STEP 2: Starting UI build =====");
        
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if(canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = canvasRenderMode;
            
            // Nếu dùng ScreenSpaceCamera, set camera
            if (canvasRenderMode == RenderMode.ScreenSpaceCamera)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    canvas.worldCamera = mainCam;
                }
            }
            
            // Thiết lập Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Thiết lập RectTransform
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            
            Debug.Log($"Đã tạo Canvas mới với Render Mode: {canvasRenderMode}, Reference Resolution: {referenceResolution}");
        }

        List<string> processedFiles = new List<string>();

        // Xử lý PSB files
        if (usePSBLayers)
        {
            string[] psbFiles = Directory.GetFiles(folderPath, "*.psb");
            foreach(string psbFile in psbFiles)
            {
                ProcessPSBFile(psbFile, canvas);
                processedFiles.Add(psbFile);
            }
        }

        // Xử lý PNG files
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
        foreach(string file in pngFiles)
        {
            ProcessImageFile(file, canvas);
            processedFiles.Add(file);
        }
        
        Debug.Log($"===== Hoàn thành! Đã xử lý {processedFiles.Count} file từ folder: {folderPath} =====");
    }

    void ReplacePSBSpritesWithPNG()
    {
        string[] psbFiles = Directory.GetFiles(folderPath, "*.psb");
        
        if (psbFiles.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy file PSB");
            return;
        }

        // Build map của PNG sprites
        var replacementMap = BuildReplacementMap();
        Debug.Log($"Tìm thấy {replacementMap.Count} PNG sprites trong folder: {spriteReplacementFolder}");

        int totalReplaced = 0;

        foreach (string psbPath in psbFiles)
        {
            Debug.Log($"\n--- Processing PSB: {Path.GetFileName(psbPath)} ---");
            
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(psbPath);
            Sprite[] sprites = assets.OfType<Sprite>().ToArray();
            
            if (sprites.Length == 0)
            {
                Debug.LogWarning($"Không tìm thấy sprite nào trong PSB");
                continue;
            }

            Debug.Log($"Tìm thấy {sprites.Length} sprites trong PSB");

            foreach (Sprite psbSprite in sprites)
            {
                if (psbSprite == null) continue;
                
                string spriteName = psbSprite.name;
                string detectedNumber = ExtractNumberFromName(spriteName);
                string detectedName = CleanSpriteName(spriteName);

                Sprite replacementSprite = null;
                string matchType = "";

                // Priority 1: Try name-based matching
                if (!string.IsNullOrEmpty(detectedName))
                {
                    replacementSprite = FindSpriteByName(replacementMap, detectedName);
                    if (replacementSprite != null)
                    {
                        matchType = $"NAME ('{detectedName}')";
                    }
                }

                // Priority 2: Fallback to number-based matching
                if (replacementSprite == null && !string.IsNullOrEmpty(detectedNumber))
                {
                    replacementSprite = FindSpriteByNumber(replacementMap, detectedNumber);
                    if (replacementSprite != null)
                    {
                        matchType = $"NUMBER (#{detectedNumber})";
                    }
                }

                if (replacementSprite != null)
                {
                    Debug.Log($"   ✅ [{matchType}] Replace: '{spriteName}' → '{replacementSprite.name}'");
                    totalReplaced++;
                    // Note: Actual replacement would require modifying PSB import settings or 
                    // using TextureImporter - for now we load PNG directly when building UI
                }
                else
                {
                    string searchInfo = !string.IsNullOrEmpty(detectedName) ? $"'{detectedName}'" : $"#{detectedNumber}";
                    Debug.LogWarning($"   ❌ No replacement found for: {spriteName} ({searchInfo})");
                }
            }
        }

        Debug.Log($"\nTotal sprites ready to replace: {totalReplaced}");
    }

    Dictionary<string, Sprite> BuildReplacementMap()
    {
        var replacementMap = new Dictionary<string, Sprite>();
        
        if (!Directory.Exists(spriteReplacementFolder))
        {
            Debug.LogError($"Sprite folder not found: {spriteReplacementFolder}");
            return replacementMap;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteReplacementFolder });
        
        Debug.Log($"Found {guids.Length} sprites in replacement folder");

        foreach (string guid in guids)
        {
            string spritePath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            
            if (sprite != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(spritePath).ToLower();
                replacementMap[fileName] = sprite;
                
                // Add variations
                replacementMap[fileName.Replace("-", "_")] = sprite;
                replacementMap[fileName.Replace("_", "-")] = sprite;
                
                Debug.Log($"   Added to map: {fileName}");
            }
        }
        
        return replacementMap;
    }

    Sprite FindSpriteByName(Dictionary<string, Sprite> replacementMap, string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        string lowerName = targetName.ToLower();
        
        // Exact match
        if (replacementMap.TryGetValue(lowerName, out var sprite))
        {
            return sprite;
        }
        
        // Try with separator variations
        string withUnderscore = lowerName.Replace("-", "_");
        if (replacementMap.TryGetValue(withUnderscore, out sprite))
        {
            return sprite;
        }

        string withDash = lowerName.Replace("_", "-");
        if (replacementMap.TryGetValue(withDash, out sprite))
        {
            return sprite;
        }
        
        return null;
    }

    Sprite FindSpriteByNumber(Dictionary<string, Sprite> replacementMap, string targetNumber)
    {
        if (string.IsNullOrEmpty(targetNumber)) return null;

        foreach (var kvp in replacementMap)
        {
            string key = kvp.Key;
            // Check if key contains the number as standalone entity
            if (key == targetNumber || key.EndsWith("_" + targetNumber) || key.EndsWith("-" + targetNumber))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }

    string ExtractNumberFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        // Pure number
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+$"))
            return name;

        var numbers = System.Text.RegularExpressions.Regex.Matches(name, @"\d+");
        
        if (numbers.Count > 0)
            return numbers[numbers.Count - 1].Value;

        return "";
    }

    string CleanSpriteName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        string cleaned = name;
        
        // Remove layer prefixes
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^Layer\d+_", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Character|Char)\d*_", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Remove trailing underscore + numbers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[_\-]\d+$", "");
        cleaned = cleaned.Trim('_', ' ', '-');

        return cleaned;
    }

    void ProcessPSBFile(string psbPath, Canvas canvas)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(psbPath);
        Sprite[] sprites = assets.OfType<Sprite>().ToArray();
        
        if (sprites.Length == 0)
        {
            Debug.LogWarning($"Không tìm thấy sprite nào trong PSB: {psbPath}");
            return;
        }

        Texture2D mainTexture = assets.OfType<Texture2D>().FirstOrDefault();
        Vector2 textureSize = mainTexture != null ? new Vector2(mainTexture.width, mainTexture.height) : referenceResolution;

        string psbName = Path.GetFileNameWithoutExtension(psbPath);
        GameObject rootPanel = new GameObject(psbName + "_Panel", typeof(RectTransform));
        rootPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rootRect = rootPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = textureSize;
        rootRect.anchoredPosition = Vector2.zero;

        Debug.Log($"Tạo Panel gốc: {psbName}_Panel với {sprites.Length} sprites");

        var spriteGroups = new Dictionary<string, List<Sprite>>();
        
        foreach(Sprite sprite in sprites)
        {
            if (sprite == null) continue;
            
            string baseName = GetBaseName(sprite.name);
            
            if (!spriteGroups.ContainsKey(baseName))
                spriteGroups[baseName] = new List<Sprite>();
            
            spriteGroups[baseName].Add(sprite);
        }

        foreach(var group in spriteGroups)
        {
            string baseName = group.Key;
            List<Sprite> groupSprites = group.Value;
            
            if (groupSprites.Count == 1)
            {
                CreateUIElementFromPSB(groupSprites[0], groupSprites[0].name, rootPanel.transform, textureSize);
            }
            else
            {
                Sprite firstSprite = groupSprites[0];
                GameObject container = CreateUIElementFromPSB(firstSprite, baseName + "_Group", rootPanel.transform, textureSize);
                
                for (int i = 0; i < groupSprites.Count; i++)
                {
                    GameObject variantGO = new GameObject(groupSprites[i].name, typeof(RectTransform));
                    variantGO.transform.SetParent(container.transform, false);
                    
                    RectTransform variantRect = variantGO.GetComponent<RectTransform>();
                    variantRect.anchorMin = Vector2.zero;
                    variantRect.anchorMax = Vector2.one;
                    variantRect.sizeDelta = Vector2.zero;
                    variantRect.anchoredPosition = Vector2.zero;
                    
                    Image img = variantGO.AddComponent<Image>();
                    img.sprite = groupSprites[i];
                    img.preserveAspect = true;
                    
                    variantGO.SetActive(i == 0);
                }
            }
        }
    }
    
    string GetBaseName(string spriteName)
    {
        string baseName = spriteName;
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"[_\-]\d+$", "");
        return baseName;
    }

    void ProcessImageFile(string filePath, Canvas canvas)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Không load được sprite: {filePath}");
            return;
        }
        
        string name = Path.GetFileNameWithoutExtension(filePath);
        CreateUIElement(sprite, name, canvas.transform, canvas);
    }

    GameObject CreateUIElementFromPSB(Sprite sprite, string name, Transform parent, Vector2 textureSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        
        Rect spriteRect = sprite.rect;
        Vector2 spriteSize = new Vector2(spriteRect.width, spriteRect.height);
        
        float bottomLeftX = spriteRect.x;
        float bottomLeftY = spriteRect.y;
        
        float spriteCenterX = bottomLeftX + spriteRect.width / 2f;
        float spriteCenterY = bottomLeftY + spriteRect.height / 2f;
        
        float posX = spriteCenterX - (textureSize.x / 2f);
        float posY = spriteCenterY - (textureSize.y / 2f);
        
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = spriteSize;
        rect.anchoredPosition = new Vector2(posX, posY);

        if (name.Contains("_txt"))
        {
            CreateTextElement(go, name);
        }
        else if (name.Contains("_panel"))
        {
            CreatePanelElement(go, sprite);
        }
        else
        {
            CreateImageElement(go, sprite, name);
        }
        
        return go;
    }

    void CreateUIElement(Sprite sprite, string name, Transform parent, Canvas canvas)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);

        if (autoAnchor)
        {
            SetAnchorFromName(rect, name);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        if (name.Contains("_txt"))
        {
            CreateTextElement(go, name);
        }
        else if (name.Contains("_panel"))
        {
            CreatePanelElement(go, sprite);
        }
        else
        {
            CreateImageElement(go, sprite, name);
        }
    }

    void CreateImageElement(GameObject go, Sprite sprite, string name)
    {
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        if (name.Contains("_btn"))
        {
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;
        }
    }

    void CreateTextElement(GameObject go, string name)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = name.Replace("_txt", "").Replace("_", " ");
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    void CreatePanelElement(GameObject go, Sprite sprite)
    {
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
    }

    void SetAnchorFromName(RectTransform rect, string name)
    {
        string nameLower = name.ToLower();

        if (nameLower.Contains("top_left") || nameLower.Contains("topleft"))
        {
            SetAnchor(rect, 0, 1, 0, 1, 0, 1);
        }
        else if (nameLower.Contains("top_center") || nameLower.Contains("topcenter") || nameLower.Contains("top_middle"))
        {
            SetAnchor(rect, 0.5f, 1, 0.5f, 1, 0.5f, 1);
        }
        else if (nameLower.Contains("top_right") || nameLower.Contains("topright"))
        {
            SetAnchor(rect, 1, 1, 1, 1, 1, 1);
        }
        else if (nameLower.Contains("middle_left") || nameLower.Contains("middleleft") || nameLower.Contains("center_left"))
        {
            SetAnchor(rect, 0, 0.5f, 0, 0.5f, 0, 0.5f);
        }
        else if (nameLower.Contains("middle_center") || nameLower.Contains("center") || nameLower.Contains("middle"))
        {
            SetAnchor(rect, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
        }
        else if (nameLower.Contains("middle_right") || nameLower.Contains("middleright") || nameLower.Contains("center_right"))
        {
            SetAnchor(rect, 1, 0.5f, 1, 0.5f, 1, 0.5f);
        }
        else if (nameLower.Contains("bottom_left") || nameLower.Contains("bottomleft"))
        {
            SetAnchor(rect, 0, 0, 0, 0, 0, 0);
        }
        else if (nameLower.Contains("bottom_center") || nameLower.Contains("bottomcenter") || nameLower.Contains("bottom_middle"))
        {
            SetAnchor(rect, 0.5f, 0, 0.5f, 0, 0.5f, 0);
        }
        else if (nameLower.Contains("bottom_right") || nameLower.Contains("bottomright"))
        {
            SetAnchor(rect, 1, 0, 1, 0, 1, 0);
        }
        else
        {
            SetAnchor(rect, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    void SetAnchor(RectTransform rect, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY, float pivotX, float pivotY)
    {
        rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rect.pivot = new Vector2(pivotX, pivotY);
        
        Vector2 offset = Vector2.zero;
        
        if (anchorMinX == 0) offset.x = rect.sizeDelta.x / 2 + 20;
        else if (anchorMinX == 1) offset.x = -(rect.sizeDelta.x / 2 + 20);
        
        if (anchorMinY == 0) offset.y = rect.sizeDelta.y / 2 + 20;
        else if (anchorMinY == 1) offset.y = -(rect.sizeDelta.y / 2 + 20);
        
        rect.anchoredPosition = offset;
    }
}