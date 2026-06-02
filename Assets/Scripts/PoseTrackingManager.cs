using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using BaseOptions = Mediapipe.Tasks.Core.BaseOptions;
using RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode;
using PoseLandmarker = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarker;
using PoseLandmarkerOptions = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerOptions;
using PoseLandmarkerResult = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerResult;

public class PoseTrackingManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WebcamManager webcamSource;
    [SerializeField] private PoseVisualizer poseVisualizer;

    [Header("Model Settings")]
    [SerializeField] private string modelFileName = "pose_landmarker_full.bytes";
    [SerializeField] private int numPoses = 1;
    [SerializeField] private float detectionConfidence = 0.5f;
    [SerializeField] private float presenceConfidence = 0.5f;
    [SerializeField] private float trackingConfidence = 0.5f;

    private static readonly int[] TrackedLandmarks = { 0, 11, 12, 13, 14, 15, 16 };

    private PoseLandmarker landmarker;
    private bool isRunning;

    private readonly Queue<LandmarkSnapshot> resultQueue = new Queue<LandmarkSnapshot>();
    private readonly object queueLock = new object();

    private NativeArray<byte> imageBuffer;
    private int bufferWidth;
    private int bufferHeight;
    private bool initialized = false;

    private void Start()
    {
        webcamSource.OnCameraReady += OnCameraReady;
        if (webcamSource.IsReady) OnCameraReady();
    }

    private void Update()
    {
        lock (queueLock)
        {
            while (resultQueue.Count > 0)
            {
                var snap = resultQueue.Dequeue();
                foreach (var lm in snap.Landmarks)
                    poseVisualizer.UpdateJoint(lm.Id, lm.X, lm.Y);

                poseVisualizer.RefreshSkeleton(); 
            }
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        landmarker?.Close();

        if (imageBuffer.IsCreated)
            imageBuffer.Dispose();
    }

    private void OnCameraReady()
    {
        if (initialized)
            return;

        initialized = true;
        StartCoroutine(InitAndRun());
    }

    private IEnumerator InitAndRun()
    {
        string modelPath;

#if UNITY_ANDROID && !UNITY_EDITOR
    modelPath = System.IO.Path.Combine(Application.persistentDataPath, modelFileName);
    if (!System.IO.File.Exists(modelPath))
        yield return CopyStreamingAsset(modelFileName, modelPath);
#else
        modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelFileName);
#endif

        if (!System.IO.File.Exists(modelPath))
        {
            Debug.LogError($"[PoseTrackingManager] Model not found at: {modelPath}");
            yield break;
        }

        Debug.Log($"[PoseTrackingManager] Loading model from: {modelPath}");

        byte[] modelBytes = null;
        try
        {
            modelBytes = System.IO.File.ReadAllBytes(modelPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PoseTrackingManager] Failed to read model: {e.Message}");
            yield break;
        }

        Debug.Log($"[PoseTrackingManager] Model loaded, {modelBytes.Length} bytes.");

        var baseOptions = new BaseOptions(
            BaseOptions.Delegate.CPU,
            modelAssetBuffer: modelBytes);   

        var options = new PoseLandmarkerOptions(
            baseOptions,
            runningMode: RunningMode.LIVE_STREAM,
            numPoses: numPoses,
            minPoseDetectionConfidence: detectionConfidence,
            minPosePresenceConfidence: presenceConfidence,
            minTrackingConfidence: trackingConfidence,
            resultCallback: OnPoseLandmarksCallback);

        landmarker = PoseLandmarker.CreateFromOptions(options);
        isRunning = true;

        Debug.Log("[PoseTrackingManager] PoseLandmarker ready — starting loop.");
        yield return DetectionLoop();
    }


    private IEnumerator DetectionLoop()
    {
        var waitForEndOfFrame = new WaitForEndOfFrame();

        while (isRunning)
        {
            yield return waitForEndOfFrame;
            if (!webcamSource.IsReady) continue;

            Color32[] pixels = webcamSource.GetPixels32();
            if (pixels == null) continue;

            int w = webcamSource.Width;
            int h = webcamSource.Height;

            int needed = w * h * 4;
            if (!imageBuffer.IsCreated || bufferWidth != w || bufferHeight != h)
            {
                if (imageBuffer.IsCreated) imageBuffer.Dispose();
                imageBuffer = new NativeArray<byte>(needed, Allocator.Persistent);
                bufferWidth = w;
                bufferHeight = h;
            }

            CopyColor32ToNativeArray(pixels, imageBuffer, w, h);

            Mediapipe.Image image;
            try
            {
                image = new Mediapipe.Image(
                    Mediapipe.ImageFormat.Types.Format.Srgba,
                    w, h,
                    w * 4,
                    imageBuffer);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PoseTrackingManager] Image build failed: {e.Message}");
                continue;
            }

            long timestampMs = (long)(Time.realtimeSinceStartup * 1000);
            landmarker.DetectAsync(image, timestampMs);
        }
    }

    private void OnPoseLandmarksCallback(PoseLandmarkerResult result,Mediapipe.Image image,long timestamp)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            return;

        var landmarks = result.poseLandmarks[0];
        var snap = new LandmarkSnapshot();

        foreach (int id in TrackedLandmarks)
        {
            if (id >= landmarks.landmarks.Count) continue;
            var lm = landmarks.landmarks[id];
            snap.Landmarks.Add(new LandmarkData(id, 1f - lm.x, lm.y)); 
        }

        lock (queueLock)
            resultQueue.Enqueue(snap);
    }

    private static void CopyColor32ToNativeArray(Color32[] src, NativeArray<byte> dst, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            int srcRow = (height - 1 - y); 
            for (int x = 0; x < width; x++)
            {
                int srcIdx = (srcRow * width + x);
                int dstIdx = (y * width + x) * 4;
                dst[dstIdx] = src[srcIdx].r;
                dst[dstIdx + 1] = src[srcIdx].g;
                dst[dstIdx + 2] = src[srcIdx].b;
                dst[dstIdx + 3] = src[srcIdx].a;
            }
        }
    }

    private class LandmarkSnapshot
    {
        public List<LandmarkData> Landmarks = new List<LandmarkData>();
    }

    private readonly struct LandmarkData
    {
        public readonly int Id;
        public readonly float X, Y;
        public LandmarkData(int id, float x, float y) { Id = id; X = x; Y = y; }
    }
}