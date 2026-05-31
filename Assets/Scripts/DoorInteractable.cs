using UnityEngine;
using System.Collections;

public class DoorInteractable : MonoBehaviour
{
    [Header("组件引用")]
    public Animator doorAnimator;
    public GameObject interactPrompt;

    [Header("设置")]
    public bool isOpen = false;
    public bool isLocked = false;
    public string lockedHint = "门锁住了。";
    public float autoCloseDelay = 3f;
    public float closeDoorAnimDuration = 1f;

    private string animStateName = "opendoor";
    private Collider doorMeshCollider;
    private Coroutine autoCloseCoroutine;
    private bool isAnimating = false;

    private void Start()
    {
        doorMeshCollider = GetComponent<Collider>();
        Debug.Log($"找到的 Collider 在: {(doorMeshCollider != null ? doorMeshCollider.gameObject.name : "null")}");
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = false;
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

    private void OpenDoor()
    {
        if (isAnimating) return;

        isOpen = true;
        isAnimating = true;

        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;

        ExecuteAnimation(1f, 0f);

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoCloseTimer());

        Debug.Log("<color=green>门已开启，自动关闭计时开始</color>");
    }

    private void CloseDoor()
    {
        if (isAnimating) return;

        isOpen = false;
        isAnimating = true;

        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;

        ExecuteAnimation(-1f, 1f);

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        StartCoroutine(RestoreColliderAfterClose());

        Debug.Log("<color=yellow>门正在关闭</color>");
    }

    private IEnumerator RestoreColliderAfterClose()
    {
        yield return new WaitForSeconds(closeDoorAnimDuration);
        if (!isOpen && doorMeshCollider != null)
        {
            doorMeshCollider.isTrigger = false;
        }
        isAnimating = false;
        Debug.Log("<color=yellow>门碰撞体已恢复</color>");
    }

    private void ExecuteAnimation(float speed, float startTime)
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetFloat("AnimSpeed", speed);
            doorAnimator.Play(animStateName, 0, startTime);
        }
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != show)
        {
            interactPrompt.SetActive(show);
        }
    }

    IEnumerator AutoCloseTimer()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpen)
        {
            isAnimating = false; // ✅ 开门动画时间结束，解锁后关门
            CloseDoor();
        }
    }
}