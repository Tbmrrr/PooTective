using UnityEngine;
using System.Collections;

public class DoorInteractable : MonoBehaviour
{
    [Header("动画设置")]
    public Animator doorAnimator;
    private string animStateName = "opendoor";
    private string speedParam = "AnimSpeed";
    public float autoCloseDelay = 3f;

    [Header("状态设置")]
    public bool isOpen = false;
    public bool isLocked = false;
    public string lockedHint = "门锁住了。";

    [Header("交互提示")]
    public GameObject interactPrompt;

    private Coroutine closeCoroutine;
    private MeshCollider doorMeshCollider;

    private void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (doorAnimator != null) doorAnimator.SetFloat(speedParam, 0);
        doorMeshCollider = GetComponentInChildren<MeshCollider>();
    }

    public void OnInteract()
    {
        if (isLocked && !isOpen)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[] { "侦探：" + lockedHint }, null);
            }
            return;
        }

        if (!isOpen) OpenDoor();
        else CloseDoor();
    }

    private void OpenDoor()
    {
        isOpen = true;

        // ✅ 核心修复：判断是否在重建模式
        bool isRebuild = RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive;

        if (doorMeshCollider != null)
        {
            if (isRebuild)
            {
                // 如果是重建模式，直接把碰撞体禁用，确保 100% 能穿过去
                doorMeshCollider.enabled = false;
            }
            else
            {
                // 正常模式保持你原来的 Trigger 逻辑
                doorMeshCollider.isTrigger = true;
            }
        }

        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, 1f);
            doorAnimator.Play(animStateName, 0, 0f);
        }

        if (closeCoroutine != null) StopCoroutine(closeCoroutine);
        closeCoroutine = StartCoroutine(AutoCloseTimer());
    }

    private void CloseDoor()
    {
        isOpen = false;

        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, -1f);
            doorAnimator.Play(animStateName, 0, 1f);
        }

        // ✅ 恢复碰撞体
        if (doorMeshCollider != null)
        {
            doorMeshCollider.enabled = true; // 确保启用
            doorMeshCollider.isTrigger = false;
        }

        if (closeCoroutine != null) StopCoroutine(closeCoroutine);
    }

    IEnumerator AutoCloseTimer()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpen) CloseDoor();
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null) interactPrompt.SetActive(show);
    }
}