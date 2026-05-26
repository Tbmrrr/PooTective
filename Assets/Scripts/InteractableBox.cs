using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractableBox : MonoBehaviour
{
    [Header("交互设置")]
    [Tooltip("触发范围半径")]
    public float interactRange = 2f;

    [Tooltip("玩家的Tag")]
    public string playerTag = "Player";

    [Header("UI设置")]
    [Tooltip("提示图标UI（拖入Canvas下的Image或Panel）")]
    public GameObject interactUI;

    [Header("动画设置")]
    [Tooltip("箱子的Animator组件")]
    public Animator boxAnimator;

    [Tooltip("开箱动画的触发器名称")]
    public string openAnimTrigger = "Open";

    // ─────────────────────────────
    // 私有变量
    // ─────────────────────────────
    private bool _playerInRange = false;   // 玩家是否在范围内
    private bool _isOpened = false;   // 是否已开启
    private Transform _playerTransform;    // 玩家Transform缓存

    // ─────────────────────────────
    void Start()
    {
        // 初始隐藏交互UI
        if (interactUI != null)
            interactUI.SetActive(false);

        // 如果没手动拖入Animator，尝试自动获取
        if (boxAnimator == null)
            boxAnimator = GetComponent<Animator>();
    }

    // ─────────────────────────────
    void Update()
    {
        // 已开启 → 不再做任何检测
        if (_isOpened) return;

        DetectPlayer();
        HandleInput();
    }

    // ─────────────────────────────
    /// <summary>
    /// 检测玩家是否进入/离开范围
    /// </summary>
    void DetectPlayer()
    {
        // 用 OverlapSphere 检测范围内的碰撞体
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactRange
        );

        bool found = false;
        foreach (Collider col in hits)
        {
            if (col.CompareTag(playerTag))
            {
                found = true;
                _playerTransform = col.transform;
                break;
            }
        }

        // 状态发生变化时才更新UI
        if (found != _playerInRange)
        {
            _playerInRange = found;
            UpdateInteractUI(_playerInRange);
        }
    }

    // ─────────────────────────────
    /// <summary>
    /// 监听按键输入
    /// </summary>
    void HandleInput()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenBox();
        }
    }

    // ─────────────────────────────
    /// <summary>
    /// 开启箱子
    /// </summary>
    void OpenBox()
    {
        _isOpened = true;

        // 隐藏交互UI
        UpdateInteractUI(false);

        // 播放开箱动画
        if (boxAnimator != null)
        {
            boxAnimator.SetTrigger(openAnimTrigger);
            // 动画播放完后停在最后一帧
            StartCoroutine(FreezeAtLastFrame());
        }

        Debug.Log("[InteractableBox] 箱子已开启！");
    }

    // ─────────────────────────────
    /// <summary>
    /// 等待动画播放完毕 → 冻结在最后一帧
    /// </summary>
    IEnumerator FreezeAtLastFrame()
    {
        // 等一帧，确保动画状态已切换
        yield return null;

        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo =
            boxAnimator.GetCurrentAnimatorStateInfo(0);

        // 等待动画播放完毕（根据动画长度等待）
        yield return new WaitForSeconds(stateInfo.length);

        // ✅ 关键：Speed设为0，冻结在最后一帧
        boxAnimator.speed = 0f;

        Debug.Log("[InteractableBox] 动画已冻结在最后一帧");
    }

    // ─────────────────────────────
    /// <summary>
    /// 控制交互提示UI显示/隐藏
    /// </summary>
    void UpdateInteractUI(bool show)
    {
        if (interactUI != null)
            interactUI.SetActive(show);
    }

    // ─────────────────────────────
    /// <summary>
    /// 在Scene视图中显示交互范围（方便调试）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}