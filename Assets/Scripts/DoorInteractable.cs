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

    // 关门动画时长，设置为你的动画实际时长
    public float closeDoorAnimDuration = 1f;

    private string animStateName = "opendoor";
    private MeshCollider doorMeshCollider;
    private Coroutine autoCloseCoroutine;

    private void Start()
    {
        doorMeshCollider = GetComponentInChildren<MeshCollider>();

        // 初始状态：门关着，恢复实体碰撞
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = false;
    }

    // ✅ 玩家进入触发区域自动开门
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (isLocked)
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(new string[] { "侦探：" + lockedHint }, null);
            return;
        }

        if (!isOpen) OpenDoor();
    }

    // ✅ 保留 OnInteract 供外部调用兼容
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
        isOpen = true;

        // ✅ 开门前就设为 Trigger，整个开门动画期间不会撞玩家
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;

        ExecuteAnimation(1f, 0f);

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoCloseTimer());

        Debug.Log("<color=green>门已开启，自动关闭计时开始</color>");
    }

    private void CloseDoor()
    {
        isOpen = false;

        // ✅ 关门动画期间也保持 Trigger，动画结束后才恢复实体
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;

        ExecuteAnimation(-1f, 1f);

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        // ✅ 等关门动画播完再恢复碰撞体
        StartCoroutine(RestoreColliderAfterClose());

        Debug.Log("<color=yellow>门正在关闭</color>");
    }

    private IEnumerator RestoreColliderAfterClose()
    {
        yield return new WaitForSeconds(closeDoorAnimDuration);
        if (!isOpen && doorMeshCollider != null)
        {
            doorMeshCollider.isTrigger = false;
            Debug.Log("<color=yellow>门碰撞体已恢复</color>");
        }
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
        if (isOpen) CloseDoor();
    }
}