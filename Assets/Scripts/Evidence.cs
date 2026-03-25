using UnityEngine;

public class Evidence : MonoBehaviour
{
    [Header("证物基本信息 (用于背包)")]
    public string evidenceName;
    public Sprite evidenceIcon;
    public Sprite evidenceFullImage;
    [TextArea(3, 5)]
    public string description;

    [Header("调查对话 (交互时弹出)")]
    [Tooltip("调查时显示的文字，会自动加上'侦探：'")]
    public string[] interactLines;

    [Header("逻辑设置")]
    public bool canPickUp = true;       // 虽然不消失，但这个布尔值可以保留，用来标记是否已收集
    public bool canAskNPC = true;

    [Header("交互反馈")]
    public GameObject interactPrompt;

    private bool hasInteracted = false; // 核心：交互锁

    private void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    public void OnInteract()
    {
        // 1. 如果已经调查过，直接拦截，不再触发对话
        if (hasInteracted) return;

        if (DialogueManager.Instance != null && interactLines != null && interactLines.Length > 0)
        {
            hasInteracted = true; // 锁定状态

            // 2. 格式化对话：强制增加“侦探：”
            string[] formattedLines = new string[interactLines.Length];
            for (int i = 0; i < interactLines.Length; i++)
            {
                formattedLines[i] = "侦探：" + interactLines[i];
            }

            // 3. 开启对话
            DialogueManager.Instance.StartDialogue(formattedLines, null);
        }

        // 4. 处理笔记本逻辑
        if (NoteManager.Instance != null)
        {
            // 将证物信息“登记”到笔记本中
            NoteManager.Instance.AddEvidence(this);

            // 调查完后立刻隐藏“按E交互”的 UI 提示
            ShowPrompt(false);

            // --- 核心改动：物体不消失，但切断物理检测 ---
            // 禁用碰撞体，这样 PlayerInteraction 就再也感应不到它了
            Collider c = GetComponent<Collider>();
            if (c != null) c.enabled = false;

            Debug.Log(evidenceName + " 调查完成，物体保留在场景中。");
        }
    }

    public void ShowPrompt(bool show)
    {
        // 如果已经调查过了，彻底封死提示框的显示
        if (hasInteracted)
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (interactPrompt != null) interactPrompt.SetActive(show);
    }
}