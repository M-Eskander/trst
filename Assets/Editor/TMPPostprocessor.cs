using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System.IO;

/// <summary>
/// Auto-imports TMP Essentials when the project is first loaded.
/// </summary>
[InitializeOnLoad]
public static class TMPPostprocessor
{
    static TMPPostprocessor()
    {
        // Check if TMP essentials have been imported
        if (!TMPEssentialsImported())
        {
            ImportTMPEssentials();
        }
    }

    static bool TMPEssentialsImported()
    {
        // Check for common TMP resources that get imported with essentials
        string tmpSettingsPath = "Assets/Resources/TMP Settings.asset";
        string liberationFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

        return File.Exists(tmpSettingsPath) || File.Exists(liberationFontPath);
    }

    static void ImportTMPEssentials()
    {
        Debug.Log("Auto-importing TMP Essential Resources...");

        try
        {
            // TMP essentials package path within the UGUI package
            string packagePath = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

            if (File.Exists(packagePath))
            {
                AssetDatabase.ImportPackage(packagePath, false);
                Debug.Log("TMP Essential Resources imported successfully!");
            }
            else
            {
                Debug.LogWarning("TMP Essential Resources package not found at: " + packagePath);
                Debug.LogWarning("Please manually import: Window > TextMeshPro > Import TMP Essential Resources");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to import TMP Essentials: " + e.Message);
        }
    }
}
