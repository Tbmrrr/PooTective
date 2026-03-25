using UnityEngine;
using UnityEngine.UI;

public class NPCInteractable : MonoBehaviour
{
    [Header("配置")]
    public TextAsset dialogueFile;
    public string npcDisplayName = "狒狒";

    // --- 修改部分：从常态UI改为世界空间提示物体 ---
    [Header("世界空间 (World Space) 提示物体")]
    [Tooltip("拖入NPC头顶/身边的 Canvas 物体 (确保其 Render Mode 为 World Space)")]
    public GameObject worldPromptObject; // 拖入 NPC 子层级下的 Canvas 物体

    private string[] dialogueLines;
    private bool isFinished = false; // 记录该NPC对话是否已完成

    void Start()
    {
        // 初始隐藏世界空间提示
        if (worldPromptObject != null) worldPromptObject.SetActive(false);

        if (dialogueFile != null)
        {
            dialogueLines = dialogueFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 只有没完成过对话，且当前没在对话中，且物体 Tag 正确，才显示提示
        if (!isFinished && other.CompareTag("Player") && !DialogueManager.Instance.isDialogueActive)
        {
            if (worldPromptObject != null)
            {
                // 显示 NPC 身边的图片物体
                worldPromptObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (worldPromptObject != null)
            {
                // 隐藏 NPC 身边的图片物体
                worldPromptObject.SetActive(false);
            }
        }
    }

    public void OnInteract()
    {
        // 冲突检查：如果对话已完成，或者其他对话正在进行，则不响应
        if (isFinished || DialogueManager.Instance.isDialogueActive) return;

        // 交互开始，立即隐藏提示图片
        if (worldPromptObject != null) worldPromptObject.SetActive(false);

        // 开启对话，并告诉管理器：如果聊完了，记得回调我的 OnDialogueComplete 函数
        DialogueManager.Instance.StartDialogue(dialogueLines, this);
    }

    // 当对话真正结束时，由 DialogueManager 调用这个方法
    public void OnDialogueComplete()
    {
        isFinished = true; // 标记为已完成
        // 确保彻底隐藏
        if (worldPromptObject != null) worldPromptObject.SetActive(false);
        Debug.Log(npcDisplayName + " 的对话已终结，不再触发提示。");
    }
}