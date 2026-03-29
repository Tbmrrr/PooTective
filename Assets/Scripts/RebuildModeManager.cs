using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重建模式管理器：负责常态与重建模式切换、时间节点解锁及带旋转的坐标传送
/// </summary>
public class RebuildModeManager : MonoBehaviour
{
    public static RebuildModeManager Instance { get; private set; }

    [Header("角色与相机引用")]
    [Tooltip("场景中的主主角（例如：狗狗）")]
    public GameObject mainPlayer;
    [Tooltip("重建模式里的第一人称控制器")]
    public GameObject rebuildFPSPlayer;

    [Header("场景传送点 (包含位置与旋转信息)")]
    [Tooltip("点击节点1时去的位置与朝向")]
    public Transform node1Point;
    [Tooltip("点击节点2时去的位置与朝向")]
    public Transform node2Point;

    [Header("UI 引用")]
    public GameObject normalHUDPanel;      // 常态 HUD
    public GameObject rebuildModePanel;    // 重建模式控制台
    public GameObject normalUIAbilityIcon; // 常态下的能力入口图标

    [Header("时间节点按钮")]
    public Button nodeButton1;
    public Button nodeButton2;

    [Header("状态属性")]
    public bool isRebuildModeActive = false;
    public bool hasUnlockedRebuild = false;
    public bool hasUnlockedNode2 = false;

    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 初始状态清理
        if (rebuildModePanel != null) rebuildModePanel.SetActive(false);
        if (normalUIAbilityIcon != null) normalUIAbilityIcon.SetActive(false);
        if (rebuildFPSPlayer != null) rebuildFPSPlayer.SetActive(false);

        // 绑定按钮点击事件
        if (nodeButton1 != null) nodeButton1.onClick.AddListener(OnNode1Clicked);
        if (nodeButton2 != null) nodeButton2.onClick.AddListener(OnNode2Clicked);

        // 初始化节点2的视觉状态（锁定/解锁）
        UpdateNode2Visuals(hasUnlockedNode2);
    }

    #region 时间节点控制逻辑 (带旋转的传送)

    /// <summary>
    /// 点击节点 1：回到最初的起点（包含预设朝向）
    /// </summary>
    public void OnNode1Clicked()
    {
        TeleportToPoint(node1Point);
        Debug.Log("<color=#FFD700>[重建模式]</color> 已回溯至时间节点 1 (位置与旋转已同步)");
    }

    /// <summary>
    /// 点击节点 2：跳跃到未来的片段（包含预设朝向）
    /// </summary>
    public void OnNode2Clicked()
    {
        if (!hasUnlockedNode2)
        {
            Debug.LogWarning("节点 2 尚未解锁，无法传送！");
            return;
        }
        TeleportToPoint(node2Point);
        Debug.Log("<color=#FFD700>[重建模式]</color> 已跳跃至时间节点 2 (位置与旋转已同步)");
    }

    /// <summary>
    /// 核心传送执行函数：同时处理坐标和旋转，并解决 CharacterController 冲突
    /// </summary>
    private void TeleportToPoint(Transform targetPoint)
    {
        if (rebuildFPSPlayer == null || targetPoint == null)
        {
            Debug.LogError("传送失败：检查 rebuildFPSPlayer 或 targetPoint 是否为空！");
            return;
        }

        // 1. 获取 CharacterController
        CharacterController cc = rebuildFPSPlayer.GetComponent<CharacterController>();

        // 2. 暂时禁用控制器（否则无法手动修改 Transform）
        if (cc != null) cc.enabled = false;

        // 3. 执行位置同步
        rebuildFPSPlayer.transform.position = targetPoint.position;

        // 4. 执行旋转同步（让玩家面朝空物体的正面）
        rebuildFPSPlayer.transform.rotation = targetPoint.rotation;

        // 5. 恢复控制器
        if (cc != null) cc.enabled = true;

        // 6. 特殊处理：如果是第一人称控制器，可能需要重置其内部的 MouseLook 旋转（如有必要）
        // 比如：rebuildFPSPlayer.GetComponent<YourFPSController>().ResetMouseLook();
    }

    #endregion

    #region 重建模式切换与解锁逻辑

    public void UnlockTimeNode2()
    {
        hasUnlockedRebuild = true;
        hasUnlockedNode2 = true;
        UpdateNode2Visuals(true);
        Debug.Log("<color=#5A5757>[重建模式]</color> 时间节点 2 已解锁！");
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

        // 进入模式时，默认初始化到节点 1 的位置和朝向
        OnNode1Clicked();

        UpdateNode2Visuals(hasUnlockedNode2);

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

        if (TakenOutEvidenceUI.Instance != null)
        {
            TakenOutEvidenceUI.Instance.gameObject.SetActive(false);
        }
    }

    #endregion

    #region UI 辅助逻辑

    public void RefreshAbilityIcon()
    {
        if (normalUIAbilityIcon == null) return;
        bool shouldShow = hasUnlockedRebuild && !isRebuildModeActive;
        normalUIAbilityIcon.SetActive(shouldShow);
    }

    private void UpdateNode2Visuals(bool isUnlocked)
    {
        if (nodeButton2 == null) return;

        nodeButton2.interactable = isUnlocked;

        Image btnImage = nodeButton2.GetComponent<Image>();
        if (btnImage != null)
        {
            byte currentAlpha = (byte)(btnImage.color.a * 255);
            btnImage.color = isUnlocked ?
                new Color32(255, 255, 255, currentAlpha) :
                new Color32(90, 87, 87, currentAlpha);
        }

        Text btnText = nodeButton2.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            byte currentAlpha = (byte)(btnText.color.a * 255);
            btnText.color = isUnlocked ?
                new Color32(255, 255, 255, currentAlpha) :
                new Color32(90, 87, 87, currentAlpha);
        }
    }

    #endregion
}