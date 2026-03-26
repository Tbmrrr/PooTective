using UnityEngine;

public class Evidence : MonoBehaviour
{
    [Header("核心逻辑设置")]
    [Tooltip("必须与 NPCInteractable 中的 evidenceID 完全一致")]
    public string evidenceID;
    public bool canPickUp = true;
    public bool canAskNPC = true;

    [Header("证物基本信息 (用于背包)")]
    public string evidenceName;
    public Sprite evidenceIcon;
    public Sprite evidenceFullImage;
    [TextArea(3, 5)]
    public string description;

    [Header("调查对话 (交互时弹出)")]
    [Tooltip("调查时显示的文字，会自动加上'侦探：'")]
    public string[] interactLines;

    [Header("交互反馈")]
    public GameObject interactPrompt;

    private bool hasInteracted = false;

    private void Start()
    {
        hasInteracted = false; // 显式初始化，确保第一次能触发
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    public void OnInteract()
    {
        // 1. 拦截已调查的情况
        if (hasInteracted)
        {
            Debug.Log(evidenceName + " 已经调查过了，不再响应。");
            return;
        }

        // 2. 立即标记为已调查 (防止连按或重复触发)
        hasInteracted = true;

        // 3. 执行对话逻辑
        if (DialogueManager.Instance != null && interactLines != null && interactLines.Length > 0)
        {
            string[] formattedLines = new string[interactLines.Length];
            for (int i = 0; i < interactLines.Length; i++)
            {
                formattedLines[i] = "侦探：" + interactLines[i];
            }
            DialogueManager.Instance.StartDialogue(formattedLines, null);
        }

        // 4. 处理笔记本逻辑
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.AddEvidence(this);
            Debug.Log(evidenceName + " 已加入笔记本。ID: " + evidenceID);
        }

        // 5. 调查完立即隐藏提示
        ShowPrompt(false);

        // --- 核心修复：移除禁用 Collider 的代码 ---
        // 不要在这里 GetComponent<Collider>().enabled = false; 
        // 否则你第二次靠近或者射线检测就会完全失效。
    }

    public void ShowPrompt(bool show)
    {
        // 如果已经调查过了，永远不显示提示
        if (hasInteracted)
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(show);
            if (show) Debug.Log("靠近了证物：" + evidenceName); // 辅助调试：看控制台有没有这行
        }
    }
}