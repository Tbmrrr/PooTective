using UnityEngine;

public class StaffList : Evidence
{
    [Header("员工名单专属设置")]
    [Tooltip("交互后物体消失的延迟时间")]
    public float destroyDelay = 0.1f;

    private bool isInteracted = false; // ✅ 增加防止重复触发的锁

    // 使用 override 确保 PlayerInteraction 调用的是这个版本
    public override void OnInteract()
    {
        // 如果已经交互过了，直接拦截，不再执行任何逻辑
        if (isInteracted) return;

        isInteracted = true; // ✅ 进门第一件事：先把门锁死

        // 1. 调用解锁逻辑
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.UnlockStaffFiles();
        }

        // 2. 触发对话
        if (DialogueManager.Instance != null && interactLines != null && interactLines.Length > 0)
        {
            // 构造对话内容
            string[] formattedLines = new string[interactLines.Length];
            for (int i = 0; i < interactLines.Length; i++)
            {
                formattedLines[i] = "侦探：" + interactLines[i];
            }

            // 启动对话
            DialogueManager.Instance.StartDialogue(formattedLines, null);
        }

        // 3. 隐藏提示并准备消失
        ShowPrompt(false);

        // 延迟消失
        Invoke("DisableObject", destroyDelay);

        Debug.Log("员工名单交互成功，逻辑已锁定。");
    }

    private void DisableObject()
    {
        // 彻底关闭物体，这样 Trigger 也会随之失效
        gameObject.SetActive(false);
    }
}