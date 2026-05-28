using UnityEngine;

public class StaffList : Evidence
{
    [Header("员工名单专属设置")]
    [Tooltip("交互后物体消失的延迟时间")]
    public float destroyDelay = 0.1f;

    private bool isInteracted = false;

    public override void OnInteract()
    {
        if (isInteracted) return;
        isInteracted = true;

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.UnlockStaffFiles();
        }

        if (DialogueManager.Instance != null && interactLines != null && interactLines.Length > 0)
        {
            string[] formattedLines = new string[interactLines.Length];
            for (int i = 0; i < interactLines.Length; i++)
            {
                formattedLines[i] = "侦探：" + interactLines[i];
            }

            // ✅ 传 this 而不是 null，对话结束后由 OnDialogueComplete 收尾
            DialogueManager.Instance.StartDialogue(formattedLines, this);
        }

        ShowPrompt(false);
        Debug.Log("员工名单交互成功，逻辑已锁定。");
    }

    // ✅ 新增：对话结束回调，负责重置 DialogueManager 状态并销毁物体
    // ✅ 去掉 override，直接定义
    public void OnDialogueComplete()
    {
        Debug.Log("StaffList.OnDialogueComplete called");
        Invoke("DisableObject", destroyDelay);
        Invoke("DisableObject", destroyDelay);
    }

    // StaffList.DisableObject() 改为先禁用 Collider
    private void DisableObject()
    {
        // 1. 找到玩家脚本并主动清空引用
        PlayerInteraction player = FindObjectOfType<PlayerInteraction>();
        if (player != null)
        {
            player.ClearCurrentEvidence(this);
        }

        // 2. 原有的隐藏逻辑
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        gameObject.SetActive(false);
    }
}