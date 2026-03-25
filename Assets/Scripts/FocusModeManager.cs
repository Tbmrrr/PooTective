using UnityEngine;

public class FocusModeManager : MonoBehaviour
{
    public static FocusModeManager Instance { get; private set; }

    [Header("画面效果引用")]
    public DogVisionEffect cameraEffect; // 拖入主相机上的渲染脚本

    [Header("UI 引用")]
    [Tooltip("平时显示的常规UI面板（例如：任务列表、地图）")]
    public GameObject normalHUDPanel;

    [Tooltip("进入专注模式后显示的特殊图片")]
    public GameObject closefoucsmodeimage;

    [Header("场景线索")]
    [Tooltip("专注模式下才显示的线索物体（例如：Poo）")]
    public GameObject pooObject; // <--- 新增：拖入场景中的Poo物体

    [Header("状态")]
    public bool isFocusModeActive = false;

    private void Awake()
    {
        // 单例模式
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 初始化状态：确保游戏开始时常态UI开启，专注图片和线索物体关闭
        if (normalHUDPanel != null) normalHUDPanel.SetActive(true);
        if (closefoucsmodeimage != null) closefoucsmodeimage.SetActive(false);
        if (pooObject != null) pooObject.SetActive(false); // <--- 新增：初始隐藏Poo
    }

    void Update()
    {
        // 监听 F 键切换模式
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFocusMode();
        }
    }

    public void ToggleFocusMode()
    {
        isFocusModeActive = !isFocusModeActive;

        // 1. 处理 Shader 渲染效果
        if (cameraEffect != null)
        {
            cameraEffect.isRendering = isFocusModeActive;
        }

        // 2. 处理 UI 和 场景物体的切换逻辑
        HandleUISwitch();

        // 3. 处理其他逻辑（可选）
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
        if (isFocusModeActive)
        {
            // 进入专注模式：关闭常规HUD，打开专注模式图片和Poo
            if (normalHUDPanel != null) normalHUDPanel.SetActive(false);
            if (closefoucsmodeimage != null) closefoucsmodeimage.SetActive(true);
            if (pooObject != null) pooObject.SetActive(true); // <--- 新增：进入模式显示Poo
        }
        else
        {
            // 退出专注模式：恢复常规HUD，关闭专注模式图片和Poo
            if (normalHUDPanel != null) normalHUDPanel.SetActive(true);
            if (closefoucsmodeimage != null) closefoucsmodeimage.SetActive(false);
            if (pooObject != null) pooObject.SetActive(false); // <--- 新增：退出模式隐藏Poo
        }
    }

    void ApplyFocusLogic()
    {
        Debug.Log("进入专注模式：天蓝色滤镜已开启，线索物体已显现。");
    }

    void ResetFocusLogic()
    {
        Debug.Log("退出专注模式：恢复正常视觉。");
    }
}