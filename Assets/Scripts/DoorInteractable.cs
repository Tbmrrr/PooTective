using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorInteractable : MonoBehaviour
{
    [Header("组件引用")]
    public Animator doorAnimator;
    public GameObject interactPrompt;
    public Collider solidCollider;

    [Header("设置")]
    public bool isLocked = false;
    public string lockedHint = "门锁住了。";
    public float closeDoorAnimDuration = 1f;

    private string animStateName = "opendoor";
    private bool isOpen = false;

    // 恢复你原本的 GameObject 追踪方式，但加上了空值清理
    private HashSet<GameObject> playersInRange = new HashSet<GameObject>();
    private Coroutine restoreColliderCoroutine;

    private void Start()
    {
        if (solidCollider != null) solidCollider.isTrigger = false;
    }

    public void OnPlayerEnter(Collider player)
    {
        if (player == null) return;

        playersInRange.Add(player.transform.root.gameObject);
        Debug.Log($"<color=green>OnPlayerEnter | inRange={playersInRange.Count}</color>");

        if (isLocked)
        {
            if (DialogueManager.Instance != null && !isOpen)
                DialogueManager.Instance.StartDialogue(new string[] { "侦探：" + lockedHint }, null);
            return;
        }

        if (!isOpen) OpenDoor();
    }

    public void OnPlayerExit(Collider player)
    {
        if (player == null) return;

        playersInRange.Remove(player.transform.root.gameObject);

        // 自动清理已经被销毁或禁用的对象，防止残留导致永远关不上门
        playersInRange.RemoveWhere(obj => obj == null || !obj.activeInHierarchy);

        Debug.Log($"<color=yellow>OnPlayerExit | inRange={playersInRange.Count} | isOpen={isOpen}</color>");

        if (playersInRange.Count == 0 && isOpen)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;

        if (restoreColliderCoroutine != null)
        {
            StopCoroutine(restoreColliderCoroutine);
            restoreColliderCoroutine = null;
        }

        if (solidCollider != null) solidCollider.isTrigger = true;

        PlayAnim(1f);
    }

    private void CloseDoor()
    {
        if (!isOpen) return;
        isOpen = false;

        if (solidCollider != null) solidCollider.isTrigger = true;

        PlayAnim(-1f);

        if (restoreColliderCoroutine != null) StopCoroutine(restoreColliderCoroutine);
        restoreColliderCoroutine = StartCoroutine(RestoreCollider());
    }

    private IEnumerator RestoreCollider()
    {
        yield return new WaitForSeconds(closeDoorAnimDuration);

        // 恢复前再次确认状态
        playersInRange.RemoveWhere(obj => obj == null || !obj.activeInHierarchy);
        if (!isOpen && playersInRange.Count == 0 && solidCollider != null)
        {
            solidCollider.isTrigger = false;
            Debug.Log("<color=yellow>碰撞体已恢复实体</color>");
        }
        restoreColliderCoroutine = null;
    }

    private void PlayAnim(float speed)
    {
        if (doorAnimator == null) return;

        doorAnimator.SetFloat("AnimSpeed", speed);
        AnimatorStateInfo stateInfo = doorAnimator.GetCurrentAnimatorStateInfo(0);

        float startTime;

        // 判断当前 Animator 是否在“opendoor”状态下
        if (stateInfo.IsName(animStateName))
        {
            // ✅ 核心修复：使用 Mathf.Clamp01 替代原代码的 % 1f
            // 作用：如果动画播到头了 (比如 1.5)，它会严格限制在 1.0 (最后一帧)。
            // 这样强行倒放时，就会精准从结尾回退，而不会因为 % 1f 计算成 0.5 导致错位和延迟。
            startTime = Mathf.Clamp01(stateInfo.normalizedTime);
        }
        else
        {
            // 如果在别的状态（比如 Idle 状态），开门直接从 0 播，关门直接从 1 播
            startTime = speed > 0 ? 0f : 1f;
        }

        // 无论如何必须强制调用 Play，唤醒 Animator 重新评估播放状态
        doorAnimator.Play(animStateName, 0, startTime);
        Debug.Log($"<color=cyan>播放动画 | 速度: {speed} | 起始时间: {startTime}</color>");
    }

    public void OnInteract()
    {
        if (isLocked && !isOpen)
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(new string[] { "侦探：" + lockedHint }, null);
            return;
        }
        if (!isOpen) OpenDoor();
        else CloseDoor();
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != show)
            interactPrompt.SetActive(show);
    }
}