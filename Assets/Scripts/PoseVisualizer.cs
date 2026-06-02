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
    [SerializeField] private RectTransform linePrefab;

    private Dictionary<string, RectTransform> lines = new Dictionary<string, RectTransform>();   

    private readonly Dictionary<int, RectTransform> _markers = new Dictionary<int, RectTransform>();

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

        CreateLine(0, 11);
        CreateLine(0, 12);
        CreateLine(11, 12);

        CreateLine(11, 13);
        CreateLine(13, 15);

        CreateLine(12, 14);
        CreateLine(14, 16);
    }
    private void CreateLine(int a, int b)
    {
        var line = Instantiate(linePrefab, markerParent);

        line.name = $"{a}_{b}";
        line.gameObject.SetActive(false);

        lines.Add($"{a}_{b}", line);
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

        marker.anchoredPosition = new Vector2(
            (normalizedX - 0.5f) * w,
            (0.5f - normalizedY) * h
        );

        if (!marker.gameObject.activeSelf)
            marker.gameObject.SetActive(true);

    }

    public void RefreshSkeleton() => UpdateSkeleton();

    private void UpdateSkeleton()
    {
        UpdateLine(0, 11);
        UpdateLine(0, 12);
        UpdateLine(11, 12);

        UpdateLine(11, 13);
        UpdateLine(13, 15);

        UpdateLine(12, 14);
        UpdateLine(14, 16);
    }
    private void UpdateLine(int a, int b)
    {
        if (!_markers.ContainsKey(a) || !_markers.ContainsKey(b))
            return;

        var p1 = _markers[a].anchoredPosition;
        var p2 = _markers[b].anchoredPosition;

        if (!lines.TryGetValue($"{a}_{b}", out var line))
            return;

        Vector2 direction = p2 - p1;
        float distance = direction.magnitude;

        line.sizeDelta = new Vector2(distance, 6);

        line.anchoredPosition = (p1 + p2) * 0.5f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);

        if (!line.gameObject.activeSelf)
            line.gameObject.SetActive(true);
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