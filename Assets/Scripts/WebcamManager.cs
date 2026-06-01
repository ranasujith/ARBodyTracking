using System;
using UnityEngine;
using UnityEngine.UI;

public class WebcamManager : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private RawImage displayImage;

    [Header("Settings")]
    [SerializeField] private int requestedWidth = 640;
    [SerializeField] private int requestedHeight = 480;
    [SerializeField] private int requestedFPS = 30;
    [SerializeField] private string deviceName = ""; 

    public WebCamTexture WebCamTexture { get; private set; }

    private Color32[] _pixelBuffer;
    public bool IsReady => WebCamTexture != null && WebCamTexture.isPlaying
                           && WebCamTexture.width > 16;

    public int Width => WebCamTexture != null ? WebCamTexture.width : requestedWidth;
    public int Height => WebCamTexture != null ? WebCamTexture.height : requestedHeight;

    public event Action OnCameraReady;

    private bool readyFired;

    private void Start()
    {
        StartCamera();
    }

    private void Update()
    {
        if (!readyFired && IsReady)
        {
            readyFired = true;
            OnCameraReady?.Invoke();
        }
    }

    private void StartCamera()
    {
        string device = string.IsNullOrEmpty(deviceName)
            ? (WebCamTexture.devices.Length > 0
                ? WebCamTexture.devices[0].name
                : "")
            : deviceName;

        if (string.IsNullOrEmpty(device))
        {
            Debug.LogError("[WebcamSource] No webcam device found!");
            return;
        }

        WebCamTexture = new WebCamTexture(device, requestedWidth, requestedHeight, requestedFPS);

        if (displayImage != null)
        {
            displayImage.texture = WebCamTexture;
            displayImage.material.mainTexture = WebCamTexture;
        }

        WebCamTexture.Play();
        Debug.Log($"[WebcamSource] Starting camera: {device}");
    }

    public Color32[] GetPixels32()
    {
        if (!IsReady) return null;
        _pixelBuffer ??= new Color32[Width * Height];
        WebCamTexture.GetPixels32(_pixelBuffer);
        return _pixelBuffer;
    }

    private void OnDestroy()
    {
        if (WebCamTexture != null && WebCamTexture.isPlaying)
            WebCamTexture.Stop();
    }
}