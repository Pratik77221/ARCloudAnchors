using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEditor;

#if UNITY_EDITOR
public class ARCoreExtensionsConfigHelper : MonoBehaviour
{
    [MenuItem("ARCore/Create ARCoreExtensionsConfig")]
    public static void CreateARCoreExtensionsConfig()
    {
        ARCoreExtensionsConfig config = ScriptableObject.CreateInstance<ARCoreExtensionsConfig>();
        config.CloudAnchorMode = CloudAnchorMode.Enabled;
        
        // Create a Resources directory if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // Save the config in the Resources folder
        AssetDatabase.CreateAsset(config, "Assets/Resources/ARCoreExtensionsConfig.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("ARCoreExtensionsConfig created at Assets/Resources/ARCoreExtensionsConfig.asset");
        Debug.Log("To set up API key authentication, go to Window > Google > ARCore > Project Settings");
        
        // Select the asset in the project view
        Selection.activeObject = config;
    }
}
#endif