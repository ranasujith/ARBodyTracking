using System.Collections.Generic;
using UnityEngine;

public class PoseVisualizer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform markerParent;
    [SerializeField] private RectTransform markerPrefab;

    private Dictionary<int, RectTransform> markers =
        new Dictionary<int, RectTransform>();


    private readonly int[] trackedLandmarks =
    {
        0,   // Nose (Head)
        11,  // Left Shoulder
        12,  // Right Shoulder
        13,  // Left Elbow
        14,  // Right Elbow
        15,  // Left Wrist
        16   // Right Wrist
    };

    private void Start()
    {
        CreateMarkers();
    }
    
    private void CreateMarkers()
    {
        foreach (int id in trackedLandmarks)
        {
            RectTransform marker =
                Instantiate(markerPrefab, markerParent);

            marker.name = $"Joint_{id}";

            markers.Add(id, marker);
            marker.anchoredPosition = new Vector2(
                Random.Range(-300, 300),
                Random.Range(-500, 500)
            );
        }
    }

    public void UpdateJoint(int landmarkId, float normalizedX, float normalizedY)
    {
        if (!markers.ContainsKey(landmarkId))
            return;

        RectTransform marker = markers[landmarkId];
        Debug.Log($"Joint {landmarkId} X:{normalizedX} Y:{normalizedY}");
        float width = markerParent.rect.width;
        float height = markerParent.rect.height;

        float posX = (normalizedX - 0.5f) * width;
        float posY = (0.5f - normalizedY) * height;

        marker.anchoredPosition =
            new Vector2(posX, posY);
    }

    public void SetMarkerVisible(int landmarkId, bool visible)
    {
        if (!markers.ContainsKey(landmarkId))
            return;

        markers[landmarkId].gameObject.SetActive(visible);
    }
}