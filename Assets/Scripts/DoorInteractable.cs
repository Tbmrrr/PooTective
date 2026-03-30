using UnityEngine;
using System.Collections;

public class DoorInteractable : MonoBehaviour
{
    [Header("组件引用")]
    public Animator doorAnimator;
    public GameObject interactPrompt;

    [Header("设置")]
    public float interactDistance = 2.5f;
    public bool isOpen = false;
    public bool isLocked = false;
    public string lockedHint = "门锁住了。";
    public float autoCloseDelay = 3f; // 自动关闭延迟

    private string animStateName = "opendoor";
    private MeshCollider doorMeshCollider;
    private Transform playerTransform;
    private Coroutine autoCloseCoroutine;

    private void Start()
    {
        doorMeshCollider = GetComponentInChildren<MeshCollider>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            if (Camera.main != null) playerTransform = Camera.main.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // 只有当门没开时，显示按 E 提示
        bool shouldShow = dist <= interactDistance && !isOpen;
        ShowPrompt(shouldShow);

        // 距离感应交互（防止射线检测失效时的保底）
        if (shouldShow && Input.GetKeyDown(KeyCode.E))
        {
            if (Time.timeScale > 0 && (NoteManager.Instance == null || !NoteManager.Instance.notePanel.activeSelf))
            {
                OnInteract();
            }
        }
    }

    // ✅ 核心接口：供外部脚本（如 PlayerInteraction）调用
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
        ExecuteAnimation(1f, 0f); // 速度1，从0开始播

        // 开门时，将碰撞体设为 Trigger，方便玩家穿过
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = true;

        // 开启自动关闭计时器
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoCloseTimer());

        Debug.Log("<color=green>门已开启，3秒后自动关闭</color>");
    }

    private void CloseDoor()
    {
        isOpen = false;
        ExecuteAnimation(-1f, 1f); // 速度-1，从末尾倒着播

        // 关门瞬间恢复物理实体，防止玩家穿墙
        if (doorMeshCollider != null) doorMeshCollider.isTrigger = false;

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        Debug.Log("<color=yellow>门已关闭</color>");
    }

    private void ExecuteAnimation(float speed, float startTime)
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetFloat("AnimSpeed", speed);
            doorAnimator.Play(animStateName, 0, startTime);
        }
    }

    // ✅ 补回失踪的 ShowPrompt 接口
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
            CloseDoor();
        }
    }
}