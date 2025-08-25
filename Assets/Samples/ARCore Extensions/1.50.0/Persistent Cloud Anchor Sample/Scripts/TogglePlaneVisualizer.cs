using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class TogglePlaneVisualizer : MonoBehaviour
{
    public ARPlaneManager arPlaneManager; // Assign ARPlaneManager from Inspector
    public Button toggleButton;           // Assign UI Button from Inspector

    private bool planesVisible = true;

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePlanes);
        }
    }

    void TogglePlanes()
    {
        planesVisible = !planesVisible;

        // Enable/Disable plane prefab visualization
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(planesVisible);
        }
    }
}
