using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapBuilder.Editor
{
    public class AutoSpriteReplacer : EditorWindow
    {
        private List<SpriteInScene> sceneSprites = new List<SpriteInScene>();
        private Vector2 scrollPosition;
        private bool autoFindOnOpen = true;
        private bool preserveSpritePosition = true; // Auto-adjust position when replacing sprites

        // Path settings
        private string psbFolderPath = "Assets/PSB";
        private string spineImageFolderPath = "Assets/PSB/Img";
        private string selectedPsbFile = "";
        private bool showPathSettings = false;

        [System.Serializable]
        public class SpriteInScene
        {
            public GameObject gameObject;
            public SpriteRenderer spriteRenderer;
            public Sprite originalSprite;
            public Sprite replacementSprite;
            public string detectedNumber;
            public string folderSource;
            public bool shouldReplace = true;

            // Multi-sprite support
            public List<string> detectedNumbers = new List<string>();
            public bool isMultiSprite = false;
            public string multiSpritePattern = "";

            // Name-based matching support
            public string detectedName = "";
            public List<string> nameKeywords = new List<string>();
            public bool isNameMatch = false;
            public string nameMatchType = ""; // "exact", "partial", "keyword"
            public float nameSimilarity = 0f;
        }

        [MenuItem("Tools/Map Builder/Auto Sprite Replacer")]
        public static void ShowWindow()
        {
            GetWindow<AutoSpriteReplacer>("Auto Sprite Replacer");
        }

        private void OnEnable()
        {
            LoadPathSettings();

            if (autoFindOnOpen)
            {
                FindSpritesInScene();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Auto Sprite Replacer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawPathSettings();
            EditorGUILayout.Space();

            DrawActionButtons();
            EditorGUILayout.Space();

            DrawSpriteList();
            EditorGUILayout.Space();

            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPathSettings()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📁 Path Settings", EditorStyles.boldLabel);
            showPathSettings = EditorGUILayout.Toggle("Show Settings", showPathSettings);
            EditorGUILayout.EndHorizontal();

            if (showPathSettings)
            {
                EditorGUILayout.Space();

                // PSB Folder Path
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("PSB Folder:", GUILayout.Width(100));
                psbFolderPath = EditorGUILayout.TextField(psbFolderPath);
                if (GUILayout.Button("📁", GUILayout.Width(30)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select PSB Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        psbFolderPath = FileUtil.GetProjectRelativePath(path);
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Show available PSB files
                if (Directory.Exists(psbFolderPath))
                {
                    string[] psbFiles = Directory.GetFiles(psbFolderPath, "*.psb");
                    if (psbFiles.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("PSB Files:", GUILayout.Width(100));

                        string[] psbNames = psbFiles.Select(f => Path.GetFileName(f)).ToArray();
                        int selectedIndex = System.Array.IndexOf(psbNames, selectedPsbFile);
                        if (selectedIndex < 0) selectedIndex = 0;

                        selectedIndex = EditorGUILayout.Popup(selectedIndex, psbNames);
                        if (selectedIndex >= 0 && selectedIndex < psbNames.Length)
                        {
                            selectedPsbFile = psbNames[selectedIndex];
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No .psb files found in selected folder", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("PSB folder does not exist", MessageType.Error);
                }

                EditorGUILayout.Space();

                // Spine Image Folder Path
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Spine Img Folder:", GUILayout.Width(100));
                spineImageFolderPath = EditorGUILayout.TextField(spineImageFolderPath);
                if (GUILayout.Button("📁", GUILayout.Width(30)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Spine Image Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        spineImageFolderPath = FileUtil.GetProjectRelativePath(path);
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Show spine image count
                if (Directory.Exists(spineImageFolderPath))
                {
                    string[] imageFiles = Directory.GetFiles(spineImageFolderPath, "*.png");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("PNG Files Found:", GUILayout.Width(100));
                    GUI.color = imageFiles.Length > 0 ? Color.green : Color.red;
                    EditorGUILayout.LabelField($"{imageFiles.Length} files", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Spine image folder does not exist", MessageType.Error);
                }

                EditorGUILayout.Space();

                // Quick setup buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🚀 Auto-detect Paths"))
                {
                    AutoDetectPaths();
                }
                if (GUILayout.Button("💾 Save Settings"))
                {
                    SavePathSettings();
                }
                if (GUILayout.Button("📂 Load Settings"))
                {
                    LoadPathSettings();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📥 Import PSB to Scene"))
            {
                ImportPSBToScene();
            }

            if (GUILayout.Button("🔍 Find Sprites in Scene"))
            {
                FindSpritesInScene();
            }

            if (GUILayout.Button("🔄 Find Replacements"))
            {
                FindReplacementSprites();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = sceneSprites.Any(s => s.shouldReplace && s.replacementSprite != null);
            if (GUILayout.Button("✅ Replace All Selected"))
            {
                ReplaceAllSelectedSprites();
            }

            if (GUILayout.Button("🎯 Replace by Numbers"))
            {
                ReplaceByNumbers();
            }

            if (GUILayout.Button("🏷️ Replace by Names"))
            {
                ReplaceByNames();
            }

            if (GUILayout.Button("🔄 Replace Multi-Sprites"))
            {
                ReplaceMultiSprites();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↩️ Restore Originals"))
            {
                RestoreOriginalSprites();
            }

            if (GUILayout.Button("🔍 Debug Folders"))
            {
                DebugFolderContents();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            autoFindOnOpen = EditorGUILayout.Toggle("Auto-find on open", autoFindOnOpen);
            preserveSpritePosition = EditorGUILayout.Toggle("Preserve Position", preserveSpritePosition);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpriteList()
        {
            if (sceneSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites found in scene. Make sure you have dragged Map02.psb into the scene first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Found {sceneSprites.Count} Sprites in Scene", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            foreach (var spriteInfo in sceneSprites)
            {
                EditorGUILayout.BeginHorizontal();

                // Checkbox
                spriteInfo.shouldReplace = EditorGUILayout.Toggle(spriteInfo.shouldReplace, GUILayout.Width(20));

                // GameObject name (clickable)
                if (GUILayout.Button(spriteInfo.gameObject.name, EditorStyles.linkLabel, GUILayout.Width(120)))
                {
                    Selection.activeGameObject = spriteInfo.gameObject;
                    EditorGUIUtility.PingObject(spriteInfo.gameObject);
                }

                // Detected number/name (with indicators)
                if (spriteInfo.isMultiSprite)
                {
                    GUI.color = Color.cyan;
                    EditorGUILayout.LabelField($"#{spriteInfo.multiSpritePattern}", GUILayout.Width(80));
                    GUI.color = Color.white;
                }
                else if (!string.IsNullOrEmpty(spriteInfo.detectedNumber))
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"#{spriteInfo.detectedNumber}", GUILayout.Width(30));
                    GUI.color = Color.white;
                }
                else if (!string.IsNullOrEmpty(spriteInfo.detectedName))
                {
                    GUI.color = Color.magenta;
                    EditorGUILayout.LabelField($"N:{spriteInfo.detectedName}", GUILayout.Width(80));
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("?", GUILayout.Width(30));
                    GUI.color = Color.white;
                }

                // Arrow
                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // Replacement info
                if (spriteInfo.replacementSprite != null)
                {
                    // Color based on match type
                    if (spriteInfo.isNameMatch)
                    {
                        GUI.color = Color.magenta; // Name matches in magenta
                    }
                    else
                    {
                        GUI.color = Color.green; // Number matches in green
                    }

                    EditorGUILayout.LabelField(spriteInfo.replacementSprite.name, GUILayout.Width(100));

                    // Show match type and similarity for name matches
                    if (spriteInfo.isNameMatch)
                    {
                        string matchInfo = $"({spriteInfo.nameMatchType}:{spriteInfo.nameSimilarity:F1})";
                        EditorGUILayout.LabelField(matchInfo, GUILayout.Width(80));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"({spriteInfo.folderSource})", GUILayout.Width(80));
                    }
                    GUI.color = Color.white;

                    // Preview
                    if (spriteInfo.replacementSprite.texture != null)
                    {
                        Rect rect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                        EditorGUI.DrawPreviewTexture(rect, spriteInfo.replacementSprite.texture);
                    }
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("NOT FOUND", GUILayout.Width(100));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "🎯 Auto Sprite Replacer - Enhanced PSB & Spine Support + Name & Multi-Sprite Logic:\n\n" +
                "📋 WORKFLOW NÂNG CẤP 2024 - HỖ TRỢ TÊN GIỐNG NHAU:\n" +
                "1. 📁 Cấu hình đường dẫn trong 'Path Settings'\n" +
                "2. 🚀 Click 'Auto-detect Paths' để tự động tìm đường dẫn\n" +
                "3. 📥 Click 'Import PSB to Scene' để import PSB vào scene\n" +
                "4. 🔍 Tool sẽ tự động tìm sprites trong scene\n" +
                "5. 🔄 Click 'Find Replacements' để tìm sprites thay thế\n" +
                "6. ✅ Click 'Replace All Selected' để thay thế\n\n" +
                "🆕 TÍNH NĂNG MỚI - MATCHING SIÊU CHÍNH XÁC:\n" +
                "• 🏷️ CHỈ EXACT MATCH: 'Hoa_1' → 'Hoa.png', 'Hoa2_3' → 'Hoa2.png'\n" +
                "• ✅ KHÔNG BAO GIỜ cross-match: 'Hoa' ≠ 'Hoa2', 'Nha2' ≠ 'Hoa2'\n" +
                "• 🎯 Logic: Chỉ match khi tên GIỐNG HỆT hoặc có separator (_-.)\n" +
                "• 🎨 UI hiển thị: Xanh (số), Tím (tên), Cyan (multi-sprite)\n" +
                "• 🔄 Replace by Names: Chuyên xử lý name-based matching\n\n" +
                "🔧 PRESERVE POSITION (MỚI):\n" +
                "• ✅ BẬT: Tự động điều chỉnh vị trí để giữ nguyên visual (khuyến nghị)\n" +
                "• ❌ TẮT: Thay thế sprite trực tiếp không điều chỉnh vị trí\n" +
                "• Giải quyết vấn đề pivot point khác nhau giữa PSB và PNG\n\n" +
                "✅ LOGIC NÂNG CẤP:\n" +
                "• Phát hiện số: Layer1_16 → số 16\n" +
                "• Phát hiện tên: Layer4_Nha1 → tên 'Nha'\n" +
                "• Multi-sprite: 2-3-4-6-7-8-9 → [2,3,4,6,7,8,9]\n" +
                "• Tối ưu hóa dung lượng: 1 ảnh cho nhiều vị trí giống nhau\n" +
                "• KHÔNG nén texture - giữ 100% chất lượng gốc",
                MessageType.Info);
        }

        private void FindSpritesInScene()
        {
            sceneSprites.Clear();

            // Find all SpriteRenderer components in the scene
            SpriteRenderer[] allSpriteRenderers = FindObjectsOfType<SpriteRenderer>();

            Debug.Log($"=== FINDING SPRITES IN SCENE ===");
            Debug.Log($"Found {allSpriteRenderers.Length} SpriteRenderer components");

            foreach (var spriteRenderer in allSpriteRenderers)
            {
                if (spriteRenderer.sprite != null)
                {
                    var spriteInfo = new SpriteInScene
                    {
                        gameObject = spriteRenderer.gameObject,
                        spriteRenderer = spriteRenderer,
                        originalSprite = spriteRenderer.sprite,
                        detectedNumber = ExtractNumberFromName(spriteRenderer.sprite.name)
                    };

                    // Extract multi-sprite information
                    ExtractMultiSpriteInfo(spriteInfo);

                    // Extract name-based information
                    ExtractNameInfo(spriteInfo);

                    sceneSprites.Add(spriteInfo);

                    string spriteType = spriteInfo.isMultiSprite ? $"MULTI-SPRITE({spriteInfo.multiSpritePattern})" : $"SINGLE(#{spriteInfo.detectedNumber})";
                    if (!string.IsNullOrEmpty(spriteInfo.detectedName))
                    {
                        spriteType += $" NAME({spriteInfo.detectedName})";
                    }
                    Debug.Log($"Found sprite: {spriteRenderer.sprite.name} [{spriteType}] in GameObject: {spriteRenderer.gameObject.name}");
                }
            }

            Debug.Log($"=== TOTAL: {sceneSprites.Count} sprites found in scene ===");

            // Auto-find replacements
            if (sceneSprites.Count > 0)
            {
                FindReplacementSprites();
            }
        }

        private void AutoDetectPaths()
        {
            Debug.Log("=== AUTO-DETECTING PATHS ===");

            // Look for PSB folders
            string[] psbFolders = Directory.GetDirectories("Assets", "*PSB*", SearchOption.AllDirectories);
            if (psbFolders.Length > 0)
            {
                psbFolderPath = psbFolders[0];
                Debug.Log($"Found PSB folder: {psbFolderPath}");

                // Look for Img subfolder
                string imgPath = Path.Combine(psbFolderPath, "Img");
                if (Directory.Exists(imgPath))
                {
                    spineImageFolderPath = imgPath;
                    Debug.Log($"Found Img folder: {spineImageFolderPath}");
                }
            }

            // Auto-select first PSB file
            if (Directory.Exists(psbFolderPath))
            {
                string[] psbFiles = Directory.GetFiles(psbFolderPath, "*.psb");
                if (psbFiles.Length > 0)
                {
                    selectedPsbFile = Path.GetFileName(psbFiles[0]);
                    Debug.Log($"Auto-selected PSB: {selectedPsbFile}");
                }
            }

            EditorUtility.DisplayDialog("Auto-detect Complete",
                $"PSB Folder: {psbFolderPath}\nSpine Img Folder: {spineImageFolderPath}\nSelected PSB: {selectedPsbFile}", "OK");
        }

        private void SavePathSettings()
        {
            EditorPrefs.SetString("AutoSpriteReplacer_PSBPath", psbFolderPath);
            EditorPrefs.SetString("AutoSpriteReplacer_SpineImgPath", spineImageFolderPath);
            EditorPrefs.SetString("AutoSpriteReplacer_SelectedPSB", selectedPsbFile);

            EditorUtility.DisplayDialog("Settings Saved", "All settings have been saved to EditorPrefs.", "OK");
        }

        private void LoadPathSettings()
        {
            psbFolderPath = EditorPrefs.GetString("AutoSpriteReplacer_PSBPath", "Assets/PSB");
            spineImageFolderPath = EditorPrefs.GetString("AutoSpriteReplacer_SpineImgPath", "Assets/PSB/Img");
            selectedPsbFile = EditorPrefs.GetString("AutoSpriteReplacer_SelectedPSB", "");
        }

        private void ImportPSBToScene()
        {
            if (string.IsNullOrEmpty(selectedPsbFile))
            {
                EditorUtility.DisplayDialog("No PSB Selected", "Please select a PSB file first in Path Settings.", "OK");
                return;
            }

            string psbPath = Path.Combine(psbFolderPath, selectedPsbFile);

            if (!File.Exists(psbPath))
            {
                EditorUtility.DisplayDialog("PSB Not Found", $"PSB file not found at: {psbPath}", "OK");
                return;
            }

            // Load PSB asset
            var psbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(psbPath);
            if (psbAsset == null)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Could not load PSB as GameObject: {psbPath}", "OK");
                return;
            }

            // Instantiate in scene
            GameObject psbInstance = PrefabUtility.InstantiatePrefab(psbAsset) as GameObject;
            if (psbInstance != null)
            {
                // Position at origin
                psbInstance.transform.position = Vector3.zero;

                // Select the imported object
                Selection.activeGameObject = psbInstance;

                // Auto-find sprites after import
                EditorApplication.delayCall += () => {
                    FindSpritesInScene();
                };

                Debug.Log($"✅ Successfully imported {selectedPsbFile} to scene");
                EditorUtility.DisplayDialog("Import Success",
                    $"Successfully imported {selectedPsbFile} to scene!\nAuto-finding sprites...", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Import Failed", "Failed to instantiate PSB in scene.", "OK");
            }
        }

        private string ExtractNumberFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            Debug.Log($"🔍 Extracting number from: '{name}'");

            // If the name is already just a number, return it
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+$"))
            {
                Debug.Log($"   → Name is pure number: '{name}'");
                return name;
            }

            // Try to find numbers in the name
            var numbers = System.Text.RegularExpressions.Regex.Matches(name, @"\d+");

            Debug.Log($"   Found {numbers.Count} numbers: {string.Join(", ", numbers.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value))}");

            if (numbers.Count > 0)
            {
                // For names like "Layer4_1", we want the number after underscore (the last one)
                // For names like "Character1_0", we also want the last number
                string result = numbers[numbers.Count - 1].Value;
                Debug.Log($"   → Selected number: '{result}'");
                return result;
            }

            Debug.Log($"   → No numbers found");
            return "";
        }

        private void ExtractNameInfo(SpriteInScene spriteInfo)
        {
            if (spriteInfo.originalSprite == null) return;

            string name = spriteInfo.originalSprite.name;

            // Clean the name by removing common prefixes/suffixes and numbers
            string cleanName = CleanSpriteName(name);
            spriteInfo.detectedName = cleanName;

            // Extract keywords from the name
            ExtractNameKeywords(spriteInfo, cleanName);

            // Only log if we have a valid name (avoid log spam for number-only sprites)
            if (!string.IsNullOrEmpty(cleanName))
            {
                Debug.Log($"🔍 Name Detection: '{name}' → Clean: '{cleanName}'");
            }
        }

        private string CleanSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            // Remove common prefixes and suffixes
            string cleaned = name;

            // Remove layer prefixes like "Layer1_", "Layer4_", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^Layer\d+_", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove character prefixes like "Character1_", "Char_", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Character|Char)\d*_", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // CRITICAL FIX: Only remove numbers after underscore/dash, NOT numbers that are part of the name
            // Examples:
            // "Hoa2_1" → "Hoa2" (keep the 2, remove _1)
            // "Hoa_1" → "Hoa" (remove _1)
            // "Nha3_5" → "Nha3" (keep the 3, remove _5)

            // Remove underscore/dash followed by numbers at the end: _1, _2, -3, etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[_\-]\d+$", "");

            // Remove leading/trailing underscores and spaces
            cleaned = cleaned.Trim('_', ' ', '-');

            return cleaned;
        }

        private void ExtractNameKeywords(SpriteInScene spriteInfo, string cleanName)
        {
            spriteInfo.nameKeywords.Clear();

            if (string.IsNullOrEmpty(cleanName)) return;

            // Add the full clean name as primary keyword
            spriteInfo.nameKeywords.Add(cleanName.ToLower());

            // Split by common separators and add individual words
            string[] separators = { "_", "-", " ", ".", "+" };
            string[] words = cleanName.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                if (word.Length > 1 && !spriteInfo.nameKeywords.Contains(word.ToLower()))
                {
                    spriteInfo.nameKeywords.Add(word.ToLower());
                }
            }

            // Add common variations for Vietnamese names
            AddVietnameseVariations(spriteInfo, cleanName);
        }

        private void AddVietnameseVariations(SpriteInScene spriteInfo, string name)
        {
            string lowerName = name.ToLower();

            // Common Vietnamese building/object name variations
            var variations = new Dictionary<string, string[]>
            {
                { "nha", new[] { "house", "building", "home" } },
                { "cay", new[] { "tree", "plant" } },
                { "hoa", new[] { "flower", "bloom" } },
                { "den", new[] { "lamp", "light" } },
                { "cua", new[] { "door", "gate" } },
                { "tuong", new[] { "wall" } },
                { "dat", new[] { "ground", "earth" } },
                { "nuoc", new[] { "water" } },
                { "decor", new[] { "decoration", "ornament" } },
                { "hangrао", new[] { "fence", "barrier" } },
                { "buico", new[] { "grass", "bush" } }
            };

            foreach (var variation in variations)
            {
                if (lowerName.Contains(variation.Key))
                {
                    foreach (string synonym in variation.Value)
                    {
                        if (!spriteInfo.nameKeywords.Contains(synonym))
                        {
                            spriteInfo.nameKeywords.Add(synonym);
                        }
                    }
                }
            }
        }

        private void ExtractMultiSpriteInfo(SpriteInScene spriteInfo)
        {
            if (spriteInfo.originalSprite == null) return;

            string name = spriteInfo.originalSprite.name;
            Debug.Log($"🔍 Extracting multi-sprite info from: '{name}'");

            // Check for multi-number patterns like "2-3-4-6-7-8-9"
            var multiNumberPattern = System.Text.RegularExpressions.Regex.Match(name, @"(\d+(?:-\d+)+)");

            if (multiNumberPattern.Success)
            {
                string pattern = multiNumberPattern.Groups[1].Value;
                Debug.Log($"   → Found multi-sprite pattern: '{pattern}'");

                spriteInfo.isMultiSprite = true;
                spriteInfo.multiSpritePattern = pattern;

                // Split the pattern and extract individual numbers
                string[] numbers = pattern.Split('-');
                spriteInfo.detectedNumbers.Clear();

                foreach (string number in numbers)
                {
                    if (!string.IsNullOrEmpty(number.Trim()))
                    {
                        spriteInfo.detectedNumbers.Add(number.Trim());
                    }
                }

                Debug.Log($"   → Extracted {spriteInfo.detectedNumbers.Count} numbers: {string.Join(", ", spriteInfo.detectedNumbers)}");

                // Set the primary detected number to the first one for compatibility
                if (spriteInfo.detectedNumbers.Count > 0)
                {
                    spriteInfo.detectedNumber = spriteInfo.detectedNumbers[0];
                }
            }
            else
            {
                // Single number sprite - add to list for consistency
                if (!string.IsNullOrEmpty(spriteInfo.detectedNumber))
                {
                    spriteInfo.detectedNumbers.Clear();
                    spriteInfo.detectedNumbers.Add(spriteInfo.detectedNumber);
                    spriteInfo.isMultiSprite = false;
                }
            }
        }

        private void FindReplacementSprites()
        {
            Debug.Log("=== FINDING REPLACEMENT SPRITES ===");

            // Use configured spine image folder
            string[] searchFolders = {
                spineImageFolderPath
            };

            // First, let's see what files we have in each folder
            foreach (string folder in searchFolders)
            {
                if (Directory.Exists(folder))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
                    Debug.Log($"📁 Folder {folder}: {guids.Length} sprites found");

                    foreach (string guid in guids)
                    {
                        string spritePath = AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = Path.GetFileNameWithoutExtension(spritePath);
                        Debug.Log($"   - {fileName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"📁 Folder {folder}: NOT FOUND");
                }
            }

            foreach (var spriteInfo in sceneSprites)
            {
                bool hasNumber = !string.IsNullOrEmpty(spriteInfo.detectedNumber);
                bool hasName = !string.IsNullOrEmpty(spriteInfo.detectedName);

                if (!hasNumber && !hasName)
                {
                    Debug.LogWarning($"⚠️ No number or name detected for sprite: {spriteInfo.originalSprite.name}");
                    continue;
                }

                string targetNumber = spriteInfo.detectedNumber;
                string targetName = spriteInfo.detectedName;

                Debug.Log($"🔍 Looking for replacement for sprite: {spriteInfo.originalSprite.name}");
                if (hasNumber) Debug.Log($"   → Number: #{targetNumber}");
                if (hasName) Debug.Log($"   → Name: '{targetName}' Keywords: [{string.Join(", ", spriteInfo.nameKeywords)}]");

                foreach (string folder in searchFolders)
                {
                    if (!Directory.Exists(folder)) continue;

                    Debug.Log($"   Searching in {folder}...");
                    string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });

                    foreach (string guid in guids)
                    {
                        string spritePath = AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = Path.GetFileNameWithoutExtension(spritePath);

                        // Check if filename matches (name-based FIRST, then number-based)
                        bool matches = false;
                        string matchType = "";
                        float similarity = 0f;

                        // PRIORITY 1: Try NAME-based matching FIRST (if available)
                        // This prevents "Nha2" from matching "Hoa2" just because both have number 2
                        if (hasName)
                        {
                            var nameMatch = CheckNameMatch(fileName, spriteInfo);
                            matches = nameMatch.isMatch;
                            matchType = nameMatch.matchType;
                            similarity = nameMatch.similarity;

                            if (matches)
                            {
                                Debug.Log($"      ✅ NAME-MATCH (PRIORITY): '{targetName}' → '{fileName}' ({matchType}, similarity:{similarity:F2})");
                            }
                        }

                        // PRIORITY 2: If no name match, try number-based matching (fallback)
                        if (!matches && hasNumber)
                        {
                            if (spriteInfo.isMultiSprite && spriteInfo.detectedNumbers.Count > 0)
                            {
                                // Multi-sprite matching
                                matches = ContainsAnyNumber(fileName, spriteInfo.detectedNumbers);
                                matchType = "multi-number";
                                Debug.Log($"      Checking {fileName} for multi-sprite #{spriteInfo.multiSpritePattern}: {(matches ? "✅ MULTI-MATCH" : "❌ no match")}");
                            }
                            else
                            {
                                // Single sprite matching by number only
                                matches = ContainsNumber(fileName, targetNumber);
                                matchType = "number";
                                Debug.Log($"      Checking {fileName} for #{targetNumber}: {(matches ? "✅ NUMBER-MATCH (FALLBACK)" : "❌ no match")}");
                            }
                        }

                        if (matches)
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            if (sprite != null)
                            {
                                spriteInfo.replacementSprite = sprite;
                                spriteInfo.folderSource = Path.GetFileName(folder);
                                spriteInfo.isNameMatch = matchType.Contains("name");
                                spriteInfo.nameMatchType = matchType;
                                spriteInfo.nameSimilarity = similarity;

                                string matchInfo = hasNumber ? $"#{targetNumber}" : $"'{targetName}' ({matchType})";
                                Debug.Log($"✅ Found replacement for {matchInfo}: {fileName} in {spriteInfo.folderSource}");
                                break;
                            }
                        }
                    }

                    if (spriteInfo.replacementSprite != null) break;
                }

                if (spriteInfo.replacementSprite == null)
                {
                    string searchInfo = hasNumber ? $"#{targetNumber}" : $"'{targetName}'";
                    Debug.LogWarning($"❌ No replacement found for {searchInfo} ({spriteInfo.originalSprite.name})");
                }
            }

            int foundCount = sceneSprites.Count(s => s.replacementSprite != null);
            int multiSpriteCount = sceneSprites.Count(s => s.isMultiSprite);
            int nameMatchCount = sceneSprites.Count(s => s.isNameMatch);
            int numberMatchCount = foundCount - nameMatchCount;

            Debug.Log($"=== REPLACEMENT SEARCH COMPLETE: {foundCount}/{sceneSprites.Count} replacements found ===");
            Debug.Log($"Number matches: {numberMatchCount}, Name matches: {nameMatchCount}, Multi-sprites: {multiSpriteCount}");

            string message = $"Found {foundCount} replacement sprites out of {sceneSprites.Count} scene sprites.\n";
            message += $"Number-based matches: {numberMatchCount}\n";
            message += $"Name-based matches: {nameMatchCount}\n";
            if (multiSpriteCount > 0)
            {
                message += $"Multi-sprite patterns: {multiSpriteCount}\n";
            }
            message += "Check Console for detailed logs.";

            EditorUtility.DisplayDialog("Search Complete", message, "OK");
        }

        private bool ContainsNumber(string fileName, string targetNumber)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(targetNumber)) return false;

            Debug.Log($"      🔍 Checking if '{fileName}' contains number '{targetNumber}'");

            // More flexible number matching
            // Check for exact number match with various patterns
            var patterns = new string[]
            {
                targetNumber,                    // exact number
                "_" + targetNumber,              // _16
                targetNumber + "_",              // 16_
                "_" + targetNumber + "_",        // _16_
                targetNumber + ".",              // 16.png
                "(" + targetNumber + ")",        // (16)
                "[" + targetNumber + "]",        // [16]
            };

            foreach (var pattern in patterns)
            {
                if (fileName.Contains(pattern))
                {
                    Debug.Log($"         ✅ MATCH with pattern: '{pattern}'");
                    return true;
                }
                else
                {
                    Debug.Log($"         ❌ No match with pattern: '{pattern}'");
                }
            }

            // Also check if the number appears at the end of the filename
            if (fileName.EndsWith(targetNumber))
            {
                Debug.Log($"         ✅ MATCH at end of filename");
                return true;
            }

            Debug.Log($"         ❌ No match found");
            return false;
        }

        private (bool isMatch, string matchType, float similarity) CheckNameMatch(string fileName, SpriteInScene spriteInfo)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteInfo.detectedName))
            {
                return (false, "", 0f);
            }

            string lowerFileName = fileName.ToLower();
            string lowerDetectedName = spriteInfo.detectedName.ToLower();

            Debug.Log($"         🔍 Checking: sprite name '{lowerDetectedName}' vs file '{lowerFileName}'");

            // ULTRA STRICT MATCHING - ONLY EXACT MATCHES ALLOWED
            // This prevents ANY cross-matching between similar names

            // 1. EXACT name match (ONLY this is allowed)
            // "nha2" == "nha2" → ✅ MATCH
            // "nha2" == "hoa2" → ❌ NO MATCH (completely different)
            // "hoa" == "hoa2" → ❌ NO MATCH (different)
            if (lowerFileName == lowerDetectedName)
            {
                Debug.Log($"         ✅✅✅ EXACT MATCH!");
                return (true, "exact-name", 1.0f);
            }

            // 2. ONLY allow match if filename starts with detected name followed by separator
            // "nha2-1" or "nha2_1" or "nha2.png" → matches "nha2"
            // But "nha20" or "nha2a" → does NOT match "nha2"
            if (lowerFileName.StartsWith(lowerDetectedName))
            {
                // Check what comes after the detected name
                if (lowerFileName.Length > lowerDetectedName.Length)
                {
                    char nextChar = lowerFileName[lowerDetectedName.Length];
                    // Only match if followed by separator or extension
                    if (nextChar == '_' || nextChar == '-' || nextChar == '.')
                    {
                        Debug.Log($"         ✅ MATCH with separator '{nextChar}'");
                        return (true, "exact-with-separator", 0.98f);
                    }
                    else
                    {
                        Debug.Log($"         ❌ REJECTED: followed by '{nextChar}' (not a separator)");
                        return (false, "", 0f);
                    }
                }
            }

            // NO OTHER MATCHING ALLOWED
            // No fuzzy, no partial, no keyword - ONLY exact match

            Debug.Log($"         ❌ NO MATCH");
            return (false, "", 0f);
        }

        private float CalculateStringSimilarity(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0f;

            if (str1 == str2)
                return 1f;

            // Simple Levenshtein distance-based similarity
            int maxLen = Mathf.Max(str1.Length, str2.Length);
            int distance = LevenshteinDistance(str1, str2);

            return 1f - ((float)distance / maxLen);
        }

        private int LevenshteinDistance(string str1, string str2)
        {
            int[,] matrix = new int[str1.Length + 1, str2.Length + 1];

            for (int i = 0; i <= str1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= str2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1;

                    matrix[i, j] = Mathf.Min(
                        matrix[i - 1, j] + 1,      // deletion
                        Mathf.Min(
                            matrix[i, j - 1] + 1,  // insertion
                            matrix[i - 1, j - 1] + cost // substitution
                        )
                    );
                }
            }

            return matrix[str1.Length, str2.Length];
        }

        private bool ContainsAnyNumber(string fileName, List<string> targetNumbers)
        {
            if (string.IsNullOrEmpty(fileName) || targetNumbers == null || targetNumbers.Count == 0) return false;

            Debug.Log($"      🔍 Checking if '{fileName}' contains any of these numbers: [{string.Join(", ", targetNumbers)}]");

            foreach (string targetNumber in targetNumbers)
            {
                if (ContainsNumber(fileName, targetNumber))
                {
                    Debug.Log($"         ✅ MULTI-SPRITE MATCH with number: '{targetNumber}'");
                    return true;
                }
            }

            Debug.Log($"         ❌ No multi-sprite match found");
            return false;
        }

        private void ReplaceSpritePreservingPosition(SpriteInScene spriteInfo)
        {
            if (spriteInfo.replacementSprite == null || spriteInfo.spriteRenderer == null)
                return;

            var renderer = spriteInfo.spriteRenderer;
            var originalSprite = spriteInfo.originalSprite;
            var newSprite = spriteInfo.replacementSprite;

            if (preserveSpritePosition)
            {
                // Calculate position offset to maintain visual position
                // This compensates for differences in pivot and bounds between original and replacement sprites

                Vector3 originalWorldPosition = renderer.transform.position;

                // Get the pivot offset in world space for both sprites
                Vector3 originalPivotOffset = CalculatePivotOffsetInWorldSpace(originalSprite, renderer.transform);
                Vector3 newPivotOffset = CalculatePivotOffsetInWorldSpace(newSprite, renderer.transform);

                // Calculate the adjustment needed
                Vector3 positionAdjustment = originalPivotOffset - newPivotOffset;

                // Replace the sprite
                renderer.sprite = newSprite;

                // Adjust position to maintain visual placement (only if adjustment is significant)
                if (positionAdjustment.magnitude > 0.001f) // Only adjust if difference is noticeable
                {
                    renderer.transform.position = originalWorldPosition + positionAdjustment;
                    Debug.Log($"🔧 Position adjusted by {positionAdjustment} for {originalSprite.name} → {newSprite.name}");
                }
                else
                {
                    Debug.Log($"✅ No position adjustment needed for {originalSprite.name} → {newSprite.name}");
                }
            }
            else
            {
                // Simply replace sprite without position adjustment
                renderer.sprite = newSprite;
                Debug.Log($"✅ Replaced {originalSprite.name} → {newSprite.name} (no position adjustment)");
            }
        }

        private Vector3 CalculatePivotOffsetInWorldSpace(Sprite sprite, Transform transform)
        {
            if (sprite == null) return Vector3.zero;

            // Get sprite pivot in normalized coordinates (0-1)
            Vector2 normalizedPivot = sprite.pivot / sprite.rect.size;

            // Get sprite size in world units (considering pixels per unit)
            Vector2 spriteWorldSize = sprite.rect.size / sprite.pixelsPerUnit;

            // Calculate pivot offset from center in local space
            Vector2 pivotOffsetFromCenter = new Vector2(
                (normalizedPivot.x - 0.5f) * spriteWorldSize.x,
                (normalizedPivot.y - 0.5f) * spriteWorldSize.y
            );

            // Convert to world space using transform scale
            Vector3 worldOffset = new Vector3(
                pivotOffsetFromCenter.x * transform.lossyScale.x,
                pivotOffsetFromCenter.y * transform.lossyScale.y,
                0
            );

            return worldOffset;
        }

        private void ReplaceAllSelectedSprites()
        {
            var spritesToReplace = sceneSprites.Where(s => s.shouldReplace && s.replacementSprite != null).ToList();

            if (spritesToReplace.Count == 0)
            {
                EditorUtility.DisplayDialog("No Replacements", "No sprites selected for replacement or no replacement sprites found.", "OK");
                return;
            }

            Undo.RecordObjects(spritesToReplace.Select(s => s.spriteRenderer).ToArray(), "Replace Sprites");
            Undo.RecordObjects(spritesToReplace.Select(s => s.gameObject.transform).ToArray(), "Replace Sprites Transform");

            int replacedCount = 0;

            foreach (var spriteInfo in spritesToReplace)
            {
                ReplaceSpritePreservingPosition(spriteInfo);
                Debug.Log($"✅ Replaced {spriteInfo.originalSprite.name} with {spriteInfo.replacementSprite.name}");
                replacedCount++;
            }

            EditorUtility.DisplayDialog("Replacement Complete",
                $"Successfully replaced {replacedCount} sprites in the scene!", "OK");
        }

        private void ReplaceByNames()
        {
            // Sort by name similarity and replace name-based matches
            var nameBasedSprites = sceneSprites
                .Where(s => s.shouldReplace && s.replacementSprite != null && s.isNameMatch)
                .OrderByDescending(s => s.nameSimilarity)
                .ToList();

            if (nameBasedSprites.Count == 0)
            {
                EditorUtility.DisplayDialog("No Name Matches", "No name-based sprite matches found for replacement.", "OK");
                return;
            }

            Undo.RecordObjects(nameBasedSprites.Select(s => s.spriteRenderer).ToArray(), "Replace Sprites by Names");
            Undo.RecordObjects(nameBasedSprites.Select(s => s.gameObject.transform).ToArray(), "Replace Sprites by Names Transform");

            foreach (var spriteInfo in nameBasedSprites)
            {
                ReplaceSpritePreservingPosition(spriteInfo);
                Debug.Log($"✅ Replaced by name '{spriteInfo.detectedName}' ({spriteInfo.nameMatchType}): {spriteInfo.originalSprite.name} → {spriteInfo.replacementSprite.name}");
            }

            EditorUtility.DisplayDialog("Name Replacement Complete",
                $"Replaced {nameBasedSprites.Count} sprites using name-based matching!", "OK");
        }

        private void ReplaceByNumbers()
        {
            // Sort by detected number and replace in order
            var sortedSprites = sceneSprites
                .Where(s => s.shouldReplace && s.replacementSprite != null && !string.IsNullOrEmpty(s.detectedNumber))
                .OrderBy(s => int.TryParse(s.detectedNumber, out int num) ? num : 999)
                .ToList();

            if (sortedSprites.Count == 0)
            {
                EditorUtility.DisplayDialog("No Sprites", "No numbered sprites found for replacement.", "OK");
                return;
            }

            Undo.RecordObjects(sortedSprites.Select(s => s.spriteRenderer).ToArray(), "Replace Sprites by Numbers");
            Undo.RecordObjects(sortedSprites.Select(s => s.gameObject.transform).ToArray(), "Replace Sprites by Numbers Transform");

            foreach (var spriteInfo in sortedSprites)
            {
                ReplaceSpritePreservingPosition(spriteInfo);
                Debug.Log($"✅ Replaced #{spriteInfo.detectedNumber}: {spriteInfo.originalSprite.name} → {spriteInfo.replacementSprite.name}");
            }

            EditorUtility.DisplayDialog("Replacement Complete",
                $"Replaced {sortedSprites.Count} sprites in numerical order!", "OK");
        }

        private void ReplaceMultiSprites()
        {
            Debug.Log("=== REPLACING MULTI-SPRITES ===");

            // Group sprites by their replacement sprite (one source image for multiple targets)
            var multiSpriteGroups = new Dictionary<Sprite, List<SpriteInScene>>();

            foreach (var spriteInfo in sceneSprites)
            {
                if (spriteInfo.shouldReplace && spriteInfo.replacementSprite != null)
                {
                    if (!multiSpriteGroups.ContainsKey(spriteInfo.replacementSprite))
                    {
                        multiSpriteGroups[spriteInfo.replacementSprite] = new List<SpriteInScene>();
                    }
                    multiSpriteGroups[spriteInfo.replacementSprite].Add(spriteInfo);
                }
            }

            if (multiSpriteGroups.Count == 0)
            {
                EditorUtility.DisplayDialog("No Multi-Sprites", "No multi-sprite replacements found.", "OK");
                return;
            }

            int totalReplaced = 0;
            int multiSpriteCount = 0;

            foreach (var group in multiSpriteGroups)
            {
                var replacementSprite = group.Key;
                var targetSprites = group.Value;

                if (targetSprites.Count > 1)
                {
                    multiSpriteCount++;
                    Debug.Log($"🔄 Multi-sprite replacement: {replacementSprite.name} → {targetSprites.Count} targets");

                    foreach (var spriteInfo in targetSprites)
                    {
                        Debug.Log($"   → Target: {spriteInfo.originalSprite.name} (#{spriteInfo.detectedNumber})");
                    }
                }

                // Record undo for this group
                Undo.RecordObjects(targetSprites.Select(s => s.spriteRenderer).ToArray(),
                    $"Replace Multi-Sprite {replacementSprite.name}");
                Undo.RecordObjects(targetSprites.Select(s => s.gameObject.transform).ToArray(),
                    $"Replace Multi-Sprite {replacementSprite.name} Transform");

                // Replace all sprites in this group
                foreach (var spriteInfo in targetSprites)
                {
                    ReplaceSpritePreservingPosition(spriteInfo);
                    totalReplaced++;

                    string spriteType = spriteInfo.isMultiSprite ? "MULTI" : "SINGLE";
                    Debug.Log($"✅ [{spriteType}] Replaced {spriteInfo.originalSprite.name} → {replacementSprite.name}");
                }
            }

            string message = $"Successfully replaced {totalReplaced} sprites!\n";
            if (multiSpriteCount > 0)
            {
                message += $"Found {multiSpriteCount} multi-sprite sources that replaced multiple targets.";
            }

            EditorUtility.DisplayDialog("Multi-Sprite Replacement Complete", message, "OK");
        }

        private void RestoreOriginalSprites()
        {
            var spritesToRestore = sceneSprites.Where(s => s.originalSprite != null).ToList();

            if (spritesToRestore.Count == 0) return;

            Undo.RecordObjects(spritesToRestore.Select(s => s.spriteRenderer).ToArray(), "Restore Original Sprites");

            foreach (var spriteInfo in spritesToRestore)
            {
                spriteInfo.spriteRenderer.sprite = spriteInfo.originalSprite;
            }

            EditorUtility.DisplayDialog("Restore Complete",
                $"Restored {spritesToRestore.Count} sprites to their original state.", "OK");
        }

        private void DebugFolderContents()
        {
            Debug.Log("=== DEBUGGING FOLDER CONTENTS ===");

            string[] searchFolders = {
                "Assets/PSB/Img"
            };

            foreach (string folder in searchFolders)
            {
                Debug.Log($"\n📁 FOLDER: {folder}");

                if (!Directory.Exists(folder))
                {
                    Debug.LogError($"   ❌ FOLDER DOES NOT EXIST!");
                    continue;
                }

                // Get all files in folder (not just sprites)
                string[] allFiles = Directory.GetFiles(folder);
                Debug.Log($"   📄 Total files: {allFiles.Length}");

                foreach (string file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string extension = Path.GetExtension(file);
                    Debug.Log($"      - {fileName} ({extension})");
                }

                // Get sprites specifically
                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
                Debug.Log($"   🖼️ Sprites found by Unity: {guids.Length}");

                foreach (string guid in guids)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(spritePath);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                    if (sprite != null)
                    {
                        Debug.Log($"      ✅ {fileName} → {sprite.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"      ❌ {fileName} → Failed to load");
                    }
                }
            }

            Debug.Log("\n=== SCENE SPRITES ANALYSIS ===");
            foreach (var spriteInfo in sceneSprites.Take(10)) // Show first 10
            {
                Debug.Log($"Scene sprite: {spriteInfo.originalSprite.name} → detected number: '{spriteInfo.detectedNumber}'");
            }

            EditorUtility.DisplayDialog("Debug Complete",
                "Folder contents logged to Console. Check Console window for details.", "OK");
        }
    }
}


