using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractableBox : MonoBehaviour
{
    [Header("交互设置")]
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

    [Header("事件设置")]
    [Tooltip("开始播放动画时就启用的物体（例如特效、音效或弹出提示）")]
    public GameObject objectToEnable;

    // ─────────────────────────────
    // 私有变量
    // ─────────────────────────────
    private bool _playerInRange = false;   // 玩家是否在范围内
    private bool _isOpened = false;   // 是否已开启

    // ─────────────────────────────
    void Start()
    {
        // 初始隐藏交互UI
        if (interactUI != null)
            interactUI.SetActive(false);

        // 确保目标物体在初始状态下是隐藏的
        if (objectToEnable != null)
            objectToEnable.SetActive(false);

        // 如果没手动拖入Animator，尝试自动获取
        if (boxAnimator == null)
            boxAnimator = GetComponent<Animator>();
    }

    // ─────────────────────────────
    void Update()
    {
        // 已开启 → 不再做任何检测
        if (_isOpened) return;

        // 保留输入监听！这样按 E 才能生效
        HandleInput();
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
    // 💡 替换掉了原本复杂的 Physics.OverlapSphere 检测
    // 💡 采用 Unity 原生的 Trigger 触发器，直接读取你拉大的 Collider
    // ─────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (_isOpened) return;

        if (other.CompareTag(playerTag))
        {
            _playerInRange = true;
            UpdateInteractUI(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isOpened) return;

        if (other.CompareTag(playerTag))
        {
            _playerInRange = false;
            UpdateInteractUI(false);
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

        // 动画开始播放的瞬间，立即启用物体
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
            Debug.Log("[InteractableBox] 动画开始播放，关联物体已启用！");
        }

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

        // 关键：Speed设为0，冻结在最后一帧
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
}