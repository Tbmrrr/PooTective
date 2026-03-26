using UnityEngine;
using System.Collections;

public class DoorInteractable : MonoBehaviour
{
    [Header("动画设置")]
    public Animator doorAnimator;
    private string animStateName = "opendoor"; // 你的动画状态名
    private string speedParam = "AnimSpeed";   // Multiplier 参数名
    public float autoCloseDelay = 3f;          // 自动关门延迟

    [Header("状态设置")]
    public bool isOpen = false;
    public bool isLocked = false;
    public string lockedHint = "门锁住了。";

    [Header("交互提示")]
    public GameObject interactPrompt;

    private Coroutine closeCoroutine;

    private MeshCollider doorMeshCollider; // 用于存储子物体的 MeshCollider

    private void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        // 初始确保动画不播放
        if (doorAnimator != null) doorAnimator.SetFloat(speedParam, 0);
        doorMeshCollider = GetComponentInChildren<MeshCollider>();
    }

    public void OnInteract()
    {
        // 1. 处理锁门逻辑
        if (isLocked && !isOpen)
        {
            if (DialogueManager.Instance != null)
            {
                // 统一显示“侦探：”前缀
                DialogueManager.Instance.StartDialogue(new string[] { "侦探：" + lockedHint }, null);
            }
            return;
        }

        // 2. 交互切换
        if (!isOpen)
        {
            OpenDoor();
        }
        else
        {
            // 如果门开着的时候你又按了E，立刻手动关门
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;
        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, 1f); // 正放
            doorAnimator.Play(animStateName, 0, 0f); // 从 0% 开始播
        }

        // 开启 3 秒自动关门倒计时
        if (closeCoroutine != null) StopCoroutine(closeCoroutine);
        closeCoroutine = StartCoroutine(AutoCloseTimer());
    }

    private void CloseDoor()
    {
        isOpen = false;
        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, -1f); // 倒放
            doorAnimator.Play(animStateName, 0, 1f); // 从 100% 处开始往回播
        }
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = false;

        if (closeCoroutine != null) StopCoroutine(closeCoroutine);
    }

    IEnumerator AutoCloseTimer()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpen)
        {
            CloseDoor();
        }
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null) interactPrompt.SetActive(show);
    }
}