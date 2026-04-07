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
    public Transform pooPathRoot; // <--- 这里是关键！

    [Header("状态")]
    public bool isFocusModeActive = false;

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
            // 关键：如果关闭模式，直接隐藏并返回
            if (!active)
            {
                guideLine.gameObject.SetActive(false);
                return;
            }

            // 开启模式时
            guideLine.gameObject.SetActive(true);
            GenerateLineFromPathRoot(); // 这里只会被 ToggleFocusMode 调用一次
        }
    }

    // ✅ 新增的核心方法：读取子物体坐标连线
    private void GenerateLineFromPathRoot()
    {
        if (guideLine == null || pooPathRoot == null) return;

        // 1. 获取所有子物体的位置（不包括父物体自己）
        List<Vector3> points = new List<Vector3>();
        foreach (Transform child in pooPathRoot)
        {
            points.Add(child.position);
        }

        // 2. 如果拐点太少，无法连线，直接退出
        if (points.Count < 2) return;

        // 3. 将坐标赋给 LineRenderer
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