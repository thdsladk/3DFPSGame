using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.Memory;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 역할 : 최적화 Before / After 를 같은 조건에서 재는 측정 도구. 실행 인자에 -perfprobe 가 있을 때만 동작한다. (평소 플레이에는 영향 없음)
// 1. 렌더링 측정 : 고정 시드로 좀비 60마리를 같은 자리에 세우고(AI 정지), 고정 시점 3곳에서 프레임·CPU·GPU·렌더 통계·화면을 기록한다.
// 2. 게임플레이 측정 : 같은 좀비들의 AI 를 켜고 플레이어를 무적으로 둔 채, 스크립트까지 포함한 CPU 부하를 기록한다.
// 3. 메모리 : 메모리 카운터를 기록하고 Memory Profiler 스냅샷(.snap)을 남긴 뒤 종료한다.
// 결과는 실행 파일 옆 PerfResults/<라벨>/ 에 CSV · PNG · .snap 으로 저장한다. (라벨은 -perflabel <이름>)
public class PerformanceProbe : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab; // 측정용으로 세울 좀비 프리팹
    [SerializeField] private int _zombieCount = 60; // 측정용으로 세울 좀비 수
    [SerializeField] private int _randomSeed = 12345; // 좀비 위치를 매번 같게 하는 시드
    [SerializeField] private float _warmupSeconds = 1f; // 시점을 옮긴 뒤 기록하기 전에 기다리는 시간
    [SerializeField] private float _sampleSeconds = 4f; // 시점마다 기록하는 시간
    [SerializeField] private float _gameplaySeconds = 8f; // 게임플레이 측정 시간

    private const string EnableArgument = "-perfprobe";
    private const string LabelArgument = "-perflabel";

    // 고정 시점 (위치, 회전)
    private static readonly (string Name, Vector3 Position, Vector3 Euler)[] Views =
    {
        ("V1_east", new Vector3(2f, 1.6f, 3f), new Vector3(0f, 90f, 0f)),
        ("V2_north", new Vector3(2f, 1.6f, 3f), new Vector3(0f, 0f, 0f)),
        ("V3_south", new Vector3(-18f, 1.6f, 2f), new Vector3(0f, 180f, 0f)),
    };

    private readonly List<Zombie> _zombies = new List<Zombie>();
    private readonly StringBuilder _csv = new StringBuilder();
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private string _outputDirectory;

    // 프레임마다 값을 읽는 프로파일러 카운터
    private ProfilerRecorder _mainThread;
    private ProfilerRecorder _renderThread;
    private ProfilerRecorder _batches;
    private ProfilerRecorder _setPass;
    private ProfilerRecorder _drawCalls;
    private ProfilerRecorder _triangles;
    private ProfilerRecorder _gcAllocInFrame;
    private ProfilerRecorder _behaviourUpdate;

    private void Start()
    {
        if (HasArgument(EnableArgument) == false)
        {
            enabled = false;
            return;
        }

        string label = GetArgumentValue(LabelArgument) ?? "run";
        _outputDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "PerfResults", label);
        Directory.CreateDirectory(_outputDirectory);

        StartRecorders();
        StartCoroutine(Measure_Coroutine());
    }

    private void OnDestroy()
    {
        _mainThread.Dispose();
        _renderThread.Dispose();
        _batches.Dispose();
        _setPass.Dispose();
        _drawCalls.Dispose();
        _triangles.Dispose();
        _gcAllocInFrame.Dispose();
        _behaviourUpdate.Dispose();
    }

    private IEnumerator Measure_Coroutine()
    {
        // 1. 게임이 시작될 때까지 기다리고, 스포너를 멈춘 뒤 고정 좀비를 세운다.
        yield return new WaitUntil(() => GameManager.Instance.State == GameState.Playing);
        PrepareFixedScene();
        yield return new WaitForSeconds(_warmupSeconds);

        _csv.AppendLine("phase,view,frames,fps,frame_ms,cpu_main_ms,cpu_render_ms,gpu_ms,main_thread_ms,render_thread_ms,behaviour_update_ms,batches,setpass,drawcalls,tris,gc_alloc_kb_per_frame");

        // 2. 렌더링 측정 : 측정용 카메라로 고정 시점 3곳을 돈다. (AI 정지)
        Camera probeCamera = CreateProbeCamera();
        foreach (var view in Views)
        {
            probeCamera.transform.SetPositionAndRotation(view.Position, Quaternion.Euler(view.Euler));
            yield return new WaitForSeconds(_warmupSeconds);
            yield return Sample_Coroutine("render", view.Name, _sampleSeconds);

            ScreenCapture.CaptureScreenshot(Path.Combine(_outputDirectory, view.Name + ".png"));
            yield return null;
        }

        // 3. 게임플레이 측정 : 원래 카메라로 돌아가 좀비 AI 를 켠다. (플레이어는 무적)
        Destroy(probeCamera.gameObject);
        SetSceneCamerasEnabled(true);
        SetZombieAIEnabled(true);
        MakePlayerInvincible();
        yield return new WaitForSeconds(_warmupSeconds);
        yield return Sample_Coroutine("gameplay", "player_view", _gameplaySeconds);
        ScreenCapture.CaptureScreenshot(Path.Combine(_outputDirectory, "gameplay.png"));
        yield return null;

        // 4. 메모리 카운터와 스냅샷을 남기고 끝낸다.
        WriteMemoryCounters();
        File.WriteAllText(Path.Combine(_outputDirectory, "frames.csv"), _csv.ToString());

        bool isSnapshotDone = false;
        MemoryProfiler.TakeSnapshot(Path.Combine(_outputDirectory, "memory.snap"), (path, success) => isSnapshotDone = true);
        yield return new WaitUntil(() => isSnapshotDone);

        GameLogger.Log($"[PerformanceProbe] 측정 끝 : {_outputDirectory}");
        Application.Quit();
    }

    // 정한 시간 동안 프레임마다 값을 모아 평균을 CSV 한 줄로 남긴다.
    private IEnumerator Sample_Coroutine(string phase, string viewName, float seconds)
    {
        int frames = 0;
        double frameMs = 0, cpuMain = 0, cpuRender = 0, gpu = 0, mainThread = 0, renderThread = 0, behaviour = 0;
        double batches = 0, setPass = 0, drawCalls = 0, tris = 0, gcAlloc = 0;
        int gpuFrames = 0;

        float endTime = Time.unscaledTime + seconds;
        while (Time.unscaledTime < endTime)
        {
            yield return null;

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                cpuMain += _frameTimings[0].cpuMainThreadFrameTime;
                cpuRender += _frameTimings[0].cpuRenderThreadFrameTime;
                if (_frameTimings[0].gpuFrameTime > 0)
                {
                    gpu += _frameTimings[0].gpuFrameTime;
                    gpuFrames++;
                }
            }

            frames++;
            frameMs += Time.unscaledDeltaTime * 1000.0;
            mainThread += _mainThread.LastValue * 1e-6;
            renderThread += _renderThread.LastValue * 1e-6;
            behaviour += _behaviourUpdate.LastValue * 1e-6;
            batches += _batches.LastValue;
            setPass += _setPass.LastValue;
            drawCalls += _drawCalls.LastValue;
            tris += _triangles.LastValue;
            gcAlloc += _gcAllocInFrame.LastValue / 1024.0;
        }

        double n = Mathf.Max(1, frames);
        double avgFrameMs = frameMs / n;
        _csv.AppendLine(string.Join(",", phase, viewName, frames, (1000.0 / avgFrameMs).ToString("F1"), avgFrameMs.ToString("F2"),
            (cpuMain / n).ToString("F2"), (cpuRender / n).ToString("F2"), (gpuFrames > 0 ? gpu / gpuFrames : 0).ToString("F2"),
            (mainThread / n).ToString("F2"), (renderThread / n).ToString("F2"), (behaviour / n).ToString("F2"),
            (batches / n).ToString("F0"), (setPass / n).ToString("F0"), (drawCalls / n).ToString("F0"), (tris / n).ToString("F0"),
            (gcAlloc / n).ToString("F2")));
    }

    // 스포너·좀비 AI 를 멈추고, 고정 시드로 좀비를 같은 자리에 세운다.
    private void PrepareFixedScene()
    {
        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.StopAllCoroutines();
            spawner.enabled = false;
        }

        // 씬에 놓인 좀비는 측정에서 빼서 매번 같은 조건이 되게 한다.
        foreach (Zombie sceneZombie in FindObjectsByType<Zombie>(FindObjectsSortMode.None))
        {
            sceneZombie.gameObject.SetActive(false);
        }

        var region = new NavMeshRegion();
        Random.InitState(_randomSeed);
        for (int i = 0; i < _zombieCount * 4 && _zombies.Count < _zombieCount; i++)
        {
            if (region.TryGetRandomPoint(new Vector3(24f, 0f, -12f), 0f, 80f, out Vector3 position) == false) continue;

            Zombie zombie = Instantiate(_zombiePrefab, position + Vector3.up, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            _zombies.Add(zombie);
        }

        SetZombieAIEnabled(false);
    }

    private void SetZombieAIEnabled(bool isEnabled)
    {
        foreach (Zombie zombie in _zombies)
        {
            zombie.enabled = isEnabled;
            zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = isEnabled;
        }
    }

    // 메인·미니맵 카메라를 끄고, 고정 시점용 카메라를 메인 카메라와 같은 설정으로 만든다.
    private Camera CreateProbeCamera()
    {
        Camera mainCamera = Camera.main;
        SetSceneCamerasEnabled(false);

        // 메인 카메라가 꺼져 있는 동안에도 Camera.main 을 쓰는 스크립트(총, 줌)가 null 이 되지 않게 태그를 붙인다.
        var cameraObject = new GameObject("ProbeCamera") { tag = "MainCamera" };
        Camera probeCamera = cameraObject.AddComponent<Camera>();
        probeCamera.CopyFrom(mainCamera);
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        return probeCamera;
    }

    private void SetSceneCamerasEnabled(bool isEnabled)
    {
        foreach (Camera sceneCamera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sceneCamera.name == "ProbeCamera") continue;
            sceneCamera.enabled = isEnabled;
        }
    }

    // 게임플레이 측정 중에 플레이어가 쓰러져 게임이 멈추지 않게 체력을 크게 둔다. (측정 전용)
    private void MakePlayerInvincible()
    {
        Player player = FindFirstObjectByType<Player>();
        typeof(Player).GetField("_health", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(player, float.MaxValue);
    }

    private void WriteMemoryCounters()
    {
        string[] counters =
        {
            "Total Used Memory", "Total Reserved Memory", "GC Used Memory", "GC Reserved Memory", "Gfx Used Memory",
            "Texture Memory", "Texture Count", "Mesh Memory", "Mesh Count", "Material Count", "AnimationClip Memory", "Audio Used Memory",
        };

        var memory = new StringBuilder("counter,value_mb_or_count\n");
        foreach (string counter in counters)
        {
            using ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, counter);
            bool isCount = counter.EndsWith("Count");
            string value = recorder.Valid ? (isCount ? recorder.CurrentValue.ToString() : (recorder.CurrentValue / (1024.0 * 1024.0)).ToString("F1")) : "n/a";
            memory.AppendLine($"{counter},{value}");
        }

        File.WriteAllText(Path.Combine(_outputDirectory, "memory.csv"), memory.ToString());
    }

    private void StartRecorders()
    {
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        _renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 15);
        _behaviourUpdate = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate", 15);
        _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _gcAllocInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    private static bool HasArgument(string name)
    {
        return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;
    }

    private static string GetArgumentValue(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
