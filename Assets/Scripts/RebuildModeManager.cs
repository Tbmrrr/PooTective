using UnityEngine;
using UnityEngine.UI;

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

    [Header("时间节点按钮 (重建模式)")]
    [Tooltip("重建模式面板中的第二个按钮")]
    public Button nodeButton2;

    [Header("状态")]
    public bool isRebuildModeActive = false;
    public bool hasUnlockedRebuild = false;
    public bool hasUnlockedNode2 = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (rebuildModePanel != null) rebuildModePanel.SetActive(false);
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(false);
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(false);

        // 初始状态：根据解锁情况更新按钮2
        if (nodeButton2 != null)
        {
            UpdateNode2Visuals(hasUnlockedNode2);
        }
    }

    /// <summary>
    /// 由 Evidence (例如大便2) 第一次交互时调用
    /// </summary>
    public void UnlockTimeNode2()
    {
        hasUnlockedNode2 = true;
        if (nodeButton2 != null)
        {
            UpdateNode2Visuals(true);
        }
        Debug.Log("<color=#5A5757>[重建模式]</color> 时间节点 2 已解锁！");
    }

    /// <summary>
    /// 核心逻辑：使用 0-255 整数控制颜色，且保留 UI 原有的 Alpha 透明度
    /// </summary>
    private void UpdateNode2Visuals(bool isUnlocked)
    {
        if (nodeButton2 == null) return;

        // 设置按钮是否可以点击
        nodeButton2.interactable = isUnlocked;

        // 1. 处理按钮图片颜色 (Image)
        Image btnImage = nodeButton2.GetComponent<Image>();
        if (btnImage != null)
        {
            // 如果解锁了就变白 (255,255,255)，没解锁就变 5A5757 (90,87,87)
            // byte 强制转换确保数值合法，最后一位保留组件原本的 a (透明度)
            byte currentAlpha = (byte)(btnImage.color.a * 255);
            btnImage.color = isUnlocked ?
                new Color32(255, 255, 255, currentAlpha) :
                new Color32(90, 87, 87, currentAlpha);
        }

        // 2. 处理按钮文字颜色 (Text)
        Text btnText = nodeButton2.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            byte currentAlpha = (byte)(btnText.color.a * 255);
            btnText.color = isUnlocked ?
                new Color32(255, 255, 255, currentAlpha) :
                new Color32(90, 87, 87, currentAlpha);
        }
    }

    public void ToggleRebuildMode()
    {
        if (!hasUnlockedRebuild) return;
        isRebuildModeActive = !isRebuildModeActive;

        if (isRebuildModeActive) EnterRebuildMode();
        else ExitRebuildMode();
    }

    public void UnlockAndEnter()
    {
        hasUnlockedRebuild = true;
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
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(false);

        // 确保进入模式时按钮状态正确
        UpdateNode2Visuals(hasUnlockedNode2);

        // 如果之前有取出的证物，重新进入模式时显示它
        if (TakenOutEvidenceUI.Instance != null && !string.IsNullOrEmpty(TakenOutEvidenceUI.Instance.currentEvidenceID))
        {
            TakenOutEvidenceUI.Instance.gameObject.SetActive(true);
        }
    }

    private void ExitRebuildMode()
    {
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(false);
        if (mainPlayer != null) mainPlayer.SetActive(true);
        if (normalHUDPanel != null) normalHUDPanel.SetActive(true);
        if (rebuildModePanel != null) rebuildModePanel.SetActive(false);

        RefreshAbilityIcon();

        // 退出重建模式，强行隐藏悬浮证物（但不清除数据，保证下次进来还在）
        if (TakenOutEvidenceUI.Instance != null)
        {
            TakenOutEvidenceUI.Instance.gameObject.SetActive(false);
        }
    }

    public void RefreshAbilityIcon()
    {
        if (normalUIAbilityIcon == null) return;
        bool shouldShow = hasUnlockedRebuild && !isRebuildModeActive;
        normalUIAbilityIcon.SetActive(shouldShow);
    }
}