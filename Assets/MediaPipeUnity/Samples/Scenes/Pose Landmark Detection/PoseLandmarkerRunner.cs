using System.Collections;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Unity.Collections;
using UnityEngine;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
    {
        [SerializeField] private PoseVisualizer poseVisualizer;
        [SerializeField] private WebcamManager webcamManager;

        private readonly Dictionary<int, Vector2> pendingLandmarks = new Dictionary<int, Vector2>();
        private readonly object landmarkLock = new object();

        private static readonly int[] TrackedLandmarks = { 0, 11, 12, 13, 14, 15, 16 };

        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
        }

        private void Update()
        {
            if (poseVisualizer == null) return;

            lock (landmarkLock)
            {
                foreach (var pair in pendingLandmarks)
                    poseVisualizer.UpdateJoint(pair.Key, pair.Value.x, pair.Value.y);

                if (pendingLandmarks.Count > 0)
                    poseVisualizer.RefreshSkeleton();
            }
        }

        protected override IEnumerator Run()
        {
            yield return new WaitUntil(() => webcamManager != null && webcamManager.IsReady);
            Debug.Log("[PoseLandmarkerRunner] WebcamManager is ready.");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetPoseLandmarkerOptions(OnPoseLandmarkDetectionOutput);
            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            Debug.Log("[PoseLandmarkerRunner] PoseLandmarker created, starting detection loop.");

            var waitForEndOfFrame = new WaitForEndOfFrame();

            while (true)
            {
                if (isPaused)
                    yield return new WaitWhile(() => isPaused);

                yield return waitForEndOfFrame;

                if (!webcamManager.IsReady) continue;

                var webCamTexture = webcamManager.WebCamTexture;
                int w = webCamTexture.width;
                int h = webCamTexture.height;

                Color32[] pixels = webcamManager.GetPixels32();
                if (pixels == null) continue;

                var flipped = FlipVertical(pixels, w, h);

                using var nativeArray = new NativeArray<byte>(
                    w * h * 4, Allocator.Temp);

                CopyToNativeArray(flipped, nativeArray);

                Mediapipe.Image image;
                try
                {
                    image = new Mediapipe.Image(
                        Mediapipe.ImageFormat.Types.Format.Srgba,
                        w, h, w * 4,
                        nativeArray);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PoseLandmarkerRunner] Image build failed: {e.Message}");
                    continue;
                }

                taskApi.DetectAsync(image, GetCurrentTimestampMillisec());
            }
        }

        private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
                return;

            var landmarks = result.poseLandmarks[0];

            lock (landmarkLock)
            {
                foreach (var id in TrackedLandmarks)
                {
                    if (id >= landmarks.landmarks.Count) continue;
                    var lm = landmarks.landmarks[id];
                    pendingLandmarks[id] = new Vector2(1f - lm.x, lm.y); 
                }
            }
        }

        private static Color32[] FlipVertical(Color32[] src, int width, int height)
        {
            var dst = new Color32[src.Length];
            for (int y = 0; y < height; y++)
            {
                int srcRow = (height - 1 - y) * width;
                int dstRow = y * width;
                for (int x = 0; x < width; x++)
                    dst[dstRow + x] = src[srcRow + x];
            }
            return dst;
        }

        private static void CopyToNativeArray(Color32[] src,NativeArray<byte> dst)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int b = i * 4;
                dst[b] = src[i].r;
                dst[b + 1] = src[i].g;
                dst[b + 2] = src[i].b;
                dst[b + 3] = src[i].a;
            }
        }
    }
}