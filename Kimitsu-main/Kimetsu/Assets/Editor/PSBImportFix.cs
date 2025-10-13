using UnityEngine;
using UnityEditor;
using Unity.Burst;

namespace KimetsuEditor
{
    /// <summary>
    /// Temporary fix for PSB import issues with Burst Compiler
    /// </summary>
    public class PSBImportFix : AssetPostprocessor
    {
        private static bool burstWasEnabled = false;
        
        /// <summary>
        /// Disable Burst before PSB import
        /// </summary>
        void OnPreprocessAsset()
        {
            if (assetPath.EndsWith(".psb") || assetPath.EndsWith(".psd"))
            {
                // Store current Burst state
                burstWasEnabled = BurstCompiler.Options.EnableBurstCompilation;
                
                // Temporarily disable Burst
                if (burstWasEnabled)
                {
                    BurstCompiler.Options.EnableBurstCompilation = false;
                    Debug.Log("🔧 Temporarily disabled Burst for PSB import: " + assetPath);
                }
            }
        }
        
        /// <summary>
        /// Re-enable Burst after PSB import
        /// </summary>
        void OnPostprocessAsset()
        {
            if (assetPath.EndsWith(".psb") || assetPath.EndsWith(".psd"))
            {
                // Restore Burst state
                if (burstWasEnabled)
                {
                    BurstCompiler.Options.EnableBurstCompilation = true;
                    Debug.Log("✅ Re-enabled Burst after PSB import: " + assetPath);
                }
            }
        }
        
        /// <summary>
        /// Toggle Burst Compiler on/off
        /// </summary>
        [MenuItem("Tools/Kimetsu/🔄 Toggle Burst Compiler")]
        public static void ToggleBurstCompiler()
        {
            bool currentState = BurstCompiler.Options.EnableBurstCompilation;
            BurstCompiler.Options.EnableBurstCompilation = !currentState;
            
            string status = !currentState ? "✅ ENABLED" : "❌ DISABLED";
            string icon = !currentState ? "🚀" : "🔧";
            
            Debug.Log($"{icon} Burst Compiler {status}");
            
            if (!currentState)
            {
                Debug.Log("💡 Burst is now ON - Better performance!");
            }
            else
            {
                Debug.Log("💡 Burst is now OFF - Good for PSB import!");
            }
        }
        
        /// <summary>
        /// Force DISABLE Burst Compiler
        /// </summary>
        [MenuItem("Tools/Kimetsu/❌ Disable Burst (For PSB Import)")]
        public static void DisableBurst()
        {
            BurstCompiler.Options.EnableBurstCompilation = false;
            Debug.Log("❌ Burst Compiler DISABLED");
            Debug.Log("🔧 Safe to import PSB files now!");
            Debug.Log("💡 Use 'Enable Burst' when done importing.");
        }
        
        /// <summary>
        /// Force ENABLE Burst Compiler
        /// </summary>
        [MenuItem("Tools/Kimetsu/✅ Enable Burst (For Performance)")]
        public static void EnableBurst()
        {
            BurstCompiler.Options.EnableBurstCompilation = true;
            Debug.Log("✅ Burst Compiler ENABLED");
            Debug.Log("🚀 Maximum performance restored!");
        }
        
        /// <summary>
        /// Check current Burst status with detailed info
        /// </summary>
        [MenuItem("Tools/Kimetsu/📊 Check Burst Status")]
        public static void CheckBurstStatus()
        {
            bool isEnabled = BurstCompiler.Options.EnableBurstCompilation;
            string status = isEnabled ? "✅ ENABLED" : "❌ DISABLED";
            string recommendation = isEnabled ? "Good for performance" : "Good for PSB import";
            
            Debug.Log($"📊 BURST COMPILER STATUS: {status}");
            Debug.Log($"💡 {recommendation}");
            
            if (isEnabled)
            {
                Debug.Log("🚀 Your code is running at maximum speed!");
            }
            else
            {
                Debug.Log("🔧 Safe to import PSB/PSD files without errors.");
            }
        }
        
        /// <summary>
        /// Validation for menu items - show current state
        /// </summary>
        [MenuItem("Tools/Kimetsu/❌ Disable Burst (For PSB Import)", true)]
        public static bool ValidateDisableBurst()
        {
            Menu.SetChecked("Tools/Kimetsu/❌ Disable Burst (For PSB Import)", !BurstCompiler.Options.EnableBurstCompilation);
            return true;
        }
        
        [MenuItem("Tools/Kimetsu/✅ Enable Burst (For Performance)", true)]
        public static bool ValidateEnableBurst()
        {
            Menu.SetChecked("Tools/Kimetsu/✅ Enable Burst (For Performance)", BurstCompiler.Options.EnableBurstCompilation);
            return true;
        }
    }
}
