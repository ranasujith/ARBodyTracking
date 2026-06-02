# AR Body Tracking — Unity Demo

Real-time upper-body pose tracking in a Unity application using MediaPipe's Pose Landmarker. The webcam feed is displayed live while coloured joint markers and a skeleton overlay are rendered on top via a Unity UI Canvas.

---

## Features

- Live webcam capture with `WebCamTexture`
- Upper-body joint detection — head, shoulders, elbows, wrists (7 landmarks)
- Coloured circular markers per joint type
- Skeleton line overlay connecting joints
- Thread-safe result queue between the MediaPipe callback thread and Unity's main thread
- 20+ FPS on desktop (CPU delegate, LIVE_STREAM mode)
- Android build support (StreamingAssets copy on first launch)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 2022+ |
| Pose detection | MediaPipe Tasks Vision — `PoseLandmarker` |
| Model | `pose_landmarker_full.bytes` |
| Camera | Unity `WebCamTexture` |
| UI | Unity UI Canvas (`RectTransform` markers) |
| Platform | Windows (primary), Android (supported) |

---

## Architecture

```
WebCamManager
  │  WebCamTexture.GetPixels32()
  │  Vertical flip (bottom-up → top-down)
  ▼
PoseTrackingManager
  │  Builds Mediapipe.Image from Color32[]
  │  Calls PoseLandmarker.DetectAsync()
  ▼
MediaPipe PoseLandmarker (LIVE_STREAM, CPU)
  │  pose_landmarker_full.bytes
  │  OnPoseLandmarksCallback (background thread)
  ▼
Result Queue  ──(lock)──▶  Unity Update() thread
  ▼
PoseVisualizer
  │  UpdateJoint(id, x, y)  ×7
  │  RefreshSkeleton()
  ▼
Canvas Overlay
  RectTransform markers + line segments
```

See `ARCHITECTURE.png` for the visual diagram.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── WebcamManager.cs          # WebCamTexture capture + display
│   ├── PoseTrackingManager.cs    # MediaPipe orchestration + detection loop
│   ├── PoseVisualizer.cs         # Joint markers + skeleton rendering
│   └── PoseLandmarkerRunner.cs   # MediaPipe plugin runner integration
├── StreamingAssets/
│   └── pose_landmarker_full.bytes   # MediaPipe model file
└── Scenes/
    └── MainScene.unity
```

---

## Setup

### Prerequisites

- Unity 2022.3 LTS or newer
- [MediaPipe Unity Plugin](https://github.com/homuler/MediaPipeUnityPlugin) installed via UPM
- `pose_landmarker_full.bytes` placed in `Assets/StreamingAssets/`

### Scene Setup

1. Create a Canvas (Screen Space — Overlay).
2. Add a `RawImage` child for the webcam feed.
3. Add an empty `RectTransform` child as the marker parent.
4. Attach `WebcamManager` to a GameObject; assign the `RawImage`.
5. Attach `PoseTrackingManager`; assign `WebcamManager` and `PoseVisualizer`.
6. Attach `PoseVisualizer`; assign the marker parent, marker prefab, and line prefab.
7. Set `modelFileName` on `PoseTrackingManager` to `pose_landmarker_full.bytes`.

### Prefabs

**Marker prefab** — UI Image, ~20×20px, pivot centre.  
**Line prefab** — UI Image (white 1px texture), pivot centre-left, height 6px; width is set at runtime.

---

## How It Works

### Coordinate mapping

MediaPipe returns normalised landmark coordinates `(x, y)` in the range `[0, 1]`, origin top-left. The webcam feed is mirrored (selfie view), so X is flipped (`1 − x`) before display. The conversion to `RectTransform.anchoredPosition` is:

```
posX = (x − 0.5) × parentWidth
posY = (0.5 − y) × parentHeight
```

### Webcam pixel orientation

`WebCamTexture.GetPixels32()` returns pixels in bottom-row-first order. The image is flipped vertically before being passed to MediaPipe so landmark Y values align with the on-screen display.

### Threading

`PoseLandmarker.DetectAsync` fires its result callback on a background thread. Results are enqueued into a `Queue<LandmarkSnapshot>` behind a `lock`. Unity's `Update()` dequeues and applies them on the main thread.

### Model loading

The model bytes are read directly with `File.ReadAllBytes` and passed to `BaseOptions` as `modelAssetBuffer`. This bypasses MediaPipe's native file resolver entirely, which avoids `KeyNotFoundException` and `errno` errors on Windows.

---

## Tracked Landmarks

| ID | Joint | Marker colour |
|---|---|---|
| 0 | Head (nose) | Yellow |
| 11 | Left shoulder | Cyan |
| 12 | Right shoulder | Cyan |
| 13 | Left elbow | Green |
| 14 | Right elbow | Green |
| 15 | Left wrist | Red |
| 16 | Right wrist | Red |

---

## Performance

- Detection runs every frame via `WaitForEndOfFrame` in a coroutine.
- `RefreshSkeleton()` is called once per frame after all joints are updated (not once per joint).
- Pixel buffer is reused across frames; the `NativeArray` is allocated once and reused unless resolution changes.

---

## Android Notes

On Android, `StreamingAssets` are inside the APK and cannot be read directly. `PoseTrackingManager` copies the model to `Application.persistentDataPath` on first launch using `UnityWebRequest`.

---

## Deliverables

| Item | Location |
|---|---|
| Source code | `Assets/Scripts/` |
| Build | `/Build/` |
| README | `README.md` |
| Architecture diagram | `ARCHITECTURE.png` |

---

## License

MIT
