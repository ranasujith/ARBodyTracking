using System.Collections.Generic;
using UnityEngine;

public class PoseVisualizer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform markerParent;
    [SerializeField] private RectTransform markerPrefab;

    [Header("Marker Colours")]
    [SerializeField] private Color headColor = Color.yellow;
    [SerializeField] private Color shoulderColor = Color.cyan;
    [SerializeField] private Color elbowColor = Color.green;
    [SerializeField] private Color wristColor = Color.red;

    private readonly Dictionary<int, RectTransform> _markers =
        new Dictionary<int, RectTransform>();

    private static readonly Dictionary<int, string> JointNames = new()
    {
        { 0,  "Head"          },
        { 11, "Left Shoulder" },
        { 12, "Right Shoulder"},
        { 13, "Left Elbow"    },
        { 14, "Right Elbow"   },
        { 15, "Left Wrist"    },
        { 16, "Right Wrist"   },
    };

    private void Awake()
    {
        foreach (var kvp in JointNames)
            CreateMarker(kvp.Key, kvp.Value);
    }

    private void CreateMarker(int id, string label)
    {
        var marker = Instantiate(markerPrefab, markerParent);
        marker.name = label;
        marker.gameObject.SetActive(false); 

        var img = marker.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = GetJointColor(id);

        _markers[id] = marker;
    }

    public void UpdateJoint(int landmarkId, float normalizedX, float normalizedY)
    {
        if (!_markers.TryGetValue(landmarkId, out var marker)) return;

        float w = markerParent.rect.width;
        float h = markerParent.rect.height;

        float posX = (normalizedX - 0.5f) * w;
        float posY = (0.5f - normalizedY) * h;

        marker.anchoredPosition = new Vector2(posX, posY);
        if (!marker.gameObject.activeSelf)
            marker.gameObject.SetActive(true);
    }

    public void SetMarkerVisible(int landmarkId, bool visible)
    {
        if (_markers.TryGetValue(landmarkId, out var m))
            m.gameObject.SetActive(visible);
    }

    private Color GetJointColor(int id) => id switch
    {
        0 => headColor,
        11 or 12 => shoulderColor,
        13 or 14 => elbowColor,
        15 or 16 => wristColor,
        _ => Color.white
    };
}