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
    private Coroutine waitAnimFinishCoroutine; // 新增：用于等待关闭动画结束的协程
    private MeshCollider doorMeshCollider;

    private void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        // 初始状态下，确保动画速度为0且在起始帧
        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, 0);
        }
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

        // 如果之前正在等待关闭动画结束，强行停止
        if (waitAnimFinishCoroutine != null) StopCoroutine(waitAnimFinishCoroutine);

        // ✅ 判断是否在重建模式
        bool isRebuild = RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive;

        if (doorMeshCollider != null)
        {
            if (isRebuild)
            {
                // 重建模式下直接禁用，确保万无一失
                doorMeshCollider.enabled = false;
            }
            else
            {
                // 正常模式设为触发器，允许穿过
                doorMeshCollider.isTrigger = true;
                doorMeshCollider.enabled = true;
            }
        }

        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParam, 1f);
            // 从当前时间点开始正向播放，或者从0开始
            doorAnimator.Play(animStateName, 0, Mathf.Clamp01(doorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime));
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
            // 从当前动画位置开始反向播放
            float currentTime = doorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            doorAnimator.Play(animStateName, 0, Mathf.Clamp01(currentTime));
        }

        // ✅ 核心修改：不要在这里直接恢复碰撞体，启动协程等待
        if (waitAnimFinishCoroutine != null) StopCoroutine(waitAnimFinishCoroutine);
        waitAnimFinishCoroutine = StartCoroutine(WaitCloseAnimToFinish());

        if (closeCoroutine != null) StopCoroutine(closeCoroutine);
    }

    // ✅ 新增：等待关闭动画播放完的协程
    IEnumerator WaitCloseAnimToFinish()
    {
        if (doorAnimator == null) yield break;

        // 等待直到动画的 normalizedTime 回到起始点 (因为是反向播放速度为-1)
        // normalizedTime 在反向播放时会递减
        while (doorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.01f)
        {
            yield return null;
        }

        // 动画基本回到了起始位置，此时恢复碰撞体
        if (doorMeshCollider != null)
        {
            doorMeshCollider.enabled = true;
            doorMeshCollider.isTrigger = false;
            Debug.Log("门已完全关闭，碰撞体已恢复。");
        }

        waitAnimFinishCoroutine = null;
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