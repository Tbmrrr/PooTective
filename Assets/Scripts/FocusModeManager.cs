using UnityEngine;
using System.Collections.Generic; // 用于存储拐点

public class FocusModeManager : MonoBehaviour
{
    public static FocusModeManager Instance { get; private set; }

    [Header("画面效果引用")]
    public DogVisionEffect cameraEffect;

    [Header("UI 引用")]
    public GameObject normalHUDPanel;
    public GameObject closefoucsmodeimage;

    [Header("场景线索")]
    public GameObject pooObject;

    [Header("导向线可视化设置 (新增)")]
    [Tooltip("拖入带有 LineRenderer 的物体")]
    public LineRenderer guideLine;

    [Tooltip("拖入存放所有路径拐点(子物体)的父物体(e.g., FocusPath_Poo)")]
    public Transform pooPathRoot;

    [Header("状态")]
    public bool isFocusModeActive = false;

    // --- 新增：用于缓存 main camera 上的 pjntest 脚本 ---
    private MonoBehaviour pjnTestComponent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (normalHUDPanel != null) normalHUDPanel.SetActive(true);
        if (closefoucsmodeimage != null) closefoucsmodeimage.SetActive(false);
        if (pooObject != null) pooObject.SetActive(false);

        // 初始隐藏导向线物体
        if (guideLine != null) guideLine.gameObject.SetActive(false);

        // --- 新增：在 Start 时自动获取 MainCamera 上的 pjntest 脚本 ---
        if (Camera.main != null)
        {
            // 使用 GetComponent(string) 可以避免因为类名拼写大小写问题导致编译报错
            pjnTestComponent = Camera.main.GetComponent("pjntest") as MonoBehaviour;

            if (pjnTestComponent == null)
            {
                Debug.LogWarning("Main Camera 上未找到 pjntest 脚本，请检查命名或挂载情况。");
            }
        }
        else
        {
            Debug.LogError("场景中未找到 Tag 为 'MainCamera' 的摄像机！");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFocusMode();
        }
    }

    public void ToggleFocusMode()
    {
        isFocusModeActive = !isFocusModeActive;

        if (cameraEffect != null)
        {
            cameraEffect.isRendering = isFocusModeActive;
        }

        HandleUISwitch();

        // --- 新增：根据专注模式状态切换 pjntest 脚本的开关 ---
        if (pjnTestComponent != null)
        {
            // 开启专注模式时禁用组件（!true = false），关闭时启用（!false = true）
            pjnTestComponent.enabled = !isFocusModeActive;
            Debug.Log($"pjntest 脚本已组件状态更新为: {pjnTestComponent.enabled}");
        }

        if (isFocusModeActive)
        {
            ApplyFocusLogic();
        }
        else
        {
            ResetFocusLogic();
        }
    }

    private void HandleUISwitch()
    {
        bool active = isFocusModeActive;

        if (normalHUDPanel != null) normalHUDPanel.SetActive(!active);
        if (closefoucsmodeimage != null) closefoucsmodeimage.SetActive(active);
        if (pooObject != null) pooObject.SetActive(active);

        if (guideLine != null)
        {
            if (!active)
            {
                guideLine.gameObject.SetActive(false);
                return;
            }

            guideLine.gameObject.SetActive(true);
            GenerateLineFromPathRoot();
        }
    }

    private void GenerateLineFromPathRoot()
    {
        if (guideLine == null || pooPathRoot == null) return;

        List<Vector3> points = new List<Vector3>();
        foreach (Transform child in pooPathRoot)
        {
            points.Add(child.position);
        }

        if (points.Count < 2) return;

        guideLine.positionCount = points.Count;
        guideLine.SetPositions(points.ToArray());

        Debug.Log($"导向线已显现，共有 {points.Count} 个拐点。起点：{points[0]}");
    }

    void ApplyFocusLogic()
    {
        Debug.Log("进入专注模式：天蓝色滤镜已开启，导向线已显现。");
    }

    void ResetFocusLogic()
    {
        Debug.Log("退出专注模式：恢复正常视觉。");
    }
}