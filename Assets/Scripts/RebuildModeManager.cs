using UnityEngine;

public class RebuildModeManager : MonoBehaviour
{
    public static RebuildModeManager Instance { get; private set; }

    [Header("角色与相机引用")]
    [Tooltip("场景中的主主角（例如：狗狗）")]
    public GameObject mainPlayer;
    [Tooltip("远端房间里的第一人称控制器物体")]
    public GameObject rebuildFPSPlayer;

    [Header("UI 引用")]
    public GameObject normalHUDPanel;      // 常态 UI 面板
    public GameObject rebuildModePanel;    // 重建模式专用的 Panel
    public GameObject normalUIAbilityIcon; // 第一次触发后，在常态 UI 中显示的图标

    [Header("状态")]
    public bool isRebuildModeActive = false;
    public bool hasUnlockedRebuild = false; // 是否已经解锁了该模式

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 初始状态
        if (rebuildModePanel != null) rebuildModePanel.SetActive(false);
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(false);

        // 确保游戏开始时，远端角色是关闭的
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(false);
    }

    // 外部调用：开启/关闭模式
    public void ToggleRebuildMode()
    {
        // 如果从未触发过，由 Poo 逻辑调用 Unlock
        if (!hasUnlockedRebuild) return;

        isRebuildModeActive = !isRebuildModeActive;

        if (isRebuildModeActive)
        {
            EnterRebuildMode();
        }
        else
        {
            ExitRebuildMode();
        }
    }

    // 第一次触发时由 Poo 调用
    public void UnlockAndEnter()
    {
        hasUnlockedRebuild = true;

        // 第一次触发显示图标（或者在退出时显示）
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(true);

        isRebuildModeActive = true;
        EnterRebuildMode();
    }

    private void EnterRebuildMode()
    {
        if (FocusModeManager.Instance != null && FocusModeManager.Instance.isFocusModeActive)
        {
            FocusModeManager.Instance.ToggleFocusMode();
        }

        if (mainPlayer != null) mainPlayer.SetActive(false);
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(true);

        if (normalHUDPanel != null) normalHUDPanel.SetActive(false);
        if (rebuildModePanel != null) rebuildModePanel.SetActive(true);

        // --- 核心修复：进入重建模式时，隐藏常态 UI 里的那个图标 ---
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(false);
    }

    private void ExitRebuildMode()
    {
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(false);
        if (mainPlayer != null) mainPlayer.SetActive(true);

        if (normalHUDPanel != null) normalHUDPanel.SetActive(true);
        if (rebuildModePanel != null) rebuildModePanel.SetActive(false);

        // --- 核心修复：退出重建模式时，只有解锁了才显示图标 ---
        RefreshAbilityIcon();
    }

    // 新增：一个统一的刷新图标方法
    public void RefreshAbilityIcon()
    {
        if (normalUIAbilityIcon == null) return;

        // 只有解锁了，且当前不在重建模式，且常态UI是开启状态时才显示
        bool shouldShow = hasUnlockedRebuild && !isRebuildModeActive;
        normalUIAbilityIcon.SetActive(shouldShow);
    }
}