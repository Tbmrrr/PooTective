using UnityEngine;
using UnityEngine.UI;
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

    // 新增：是否已经被打开过
    private bool hasBeenOpened = false;

    // 恢复你原本的 GameObject 追踪方式，但加上了空值清理
    private HashSet<GameObject> playersInRange = new HashSet<GameObject>();
    private Coroutine restoreColliderCoroutine;

    private void Start()
    {
        if (solidCollider != null)
            solidCollider.isTrigger = false;
    }

    public void OnPlayerEnter(Collider player)
    {
        if (player == null) return;

        playersInRange.Add(player.transform.root.gameObject);
        Debug.Log($"<color=green>OnPlayerEnter | inRange={playersInRange.Count}</color>");

        if (isLocked)
        {
            if (DialogueManager.Instance != null && !isOpen)
                DialogueManager.Instance.StartDialogue(
                    new string[] { "侦探：" + lockedHint }, null);

            return;
        }

        if (!isOpen)
            OpenDoor();
    }

    public void OnPlayerExit(Collider player)
    {
        if (player == null) return;

        playersInRange.Remove(player.transform.root.gameObject);

        // 自动清理已经被销毁或禁用的对象，防止残留导致永远关不上门
        playersInRange.RemoveWhere(obj => obj == null || !obj.activeInHierarchy);

        Debug.Log($"<color=yellow>OnPlayerExit | inRange={playersInRange.Count} | isOpen={isOpen}</color>");

        // 已经打开过的门永久保持开启
        if (hasBeenOpened)
            return;

        if (playersInRange.Count == 0 && isOpen)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;

        // 标记为已开启
        hasBeenOpened = true;

        if (restoreColliderCoroutine != null)
        {
            StopCoroutine(restoreColliderCoroutine);
            restoreColliderCoroutine = null;
        }

        if (solidCollider != null)
            solidCollider.isTrigger = true;

        PlayAnim(1f);
    }

    private void CloseDoor()
    {
        // 已打开过的门禁止关闭
        if (hasBeenOpened)
            return;

        if (!isOpen) return;

        isOpen = false;

        if (solidCollider != null)
            solidCollider.isTrigger = true;

        PlayAnim(-1f);

        if (restoreColliderCoroutine != null)
            StopCoroutine(restoreColliderCoroutine);

        restoreColliderCoroutine = StartCoroutine(RestoreCollider());
    }

    private IEnumerator RestoreCollider()
    {
        yield return new WaitForSeconds(closeDoorAnimDuration);

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

        AnimatorStateInfo stateInfo =
            doorAnimator.GetCurrentAnimatorStateInfo(0);

        float startTime;

        if (stateInfo.IsName(animStateName))
        {
            startTime = Mathf.Clamp01(stateInfo.normalizedTime);
        }
        else
        {
            startTime = speed > 0 ? 0f : 1f;
        }

        doorAnimator.Play(animStateName, 0, startTime);

        Debug.Log(
            $"<color=cyan>播放动画 | 速度: {speed} | 起始时间: {startTime}</color>");
    }

    public void OnInteract()
    {
        if (isLocked && !isOpen)
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(
                    new string[] { "侦探：" + lockedHint }, null);

            return;
        }

        // 已经打开过，什么都不做
        if (hasBeenOpened)
            return;

        if (!isOpen)
            OpenDoor();
        else
            CloseDoor();
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null &&
            interactPrompt.activeSelf != show)
        {
            interactPrompt.SetActive(show);
        }
    }
}