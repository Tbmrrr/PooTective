using UnityEngine;
using UnityEngine.UI;

public class NPCInteractable : MonoBehaviour
{
    [Header("配置")]
    public TextAsset dialogueFile;
    public string npcDisplayName = "狒狒";

    [Header("2D UI提示面板 (屏幕常态UI)")]
    public GameObject promptPanel;
    public Text promptText; // 指向显示“按E对话”的那个Text组件

    private string[] dialogueLines;
    private bool isFinished = false; // 记录该NPC对话是否已完成

    void Start()
    {
        if (promptPanel != null) promptPanel.SetActive(false);

        if (dialogueFile != null)
        {
            dialogueLines = dialogueFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 只有没完成过对话，且当前没在对话中，才显示提示
        if (!isFinished && other.CompareTag("Player") && !DialogueManager.Instance.isDialogueActive)
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(true);
                // 修复问题1：这里显示“按E对话”，或者你想显示“[狒狒] 按E对话”
                if (promptText != null) promptText.text = "[" + npcDisplayName + "] 按E对话";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (promptPanel != null) promptPanel.SetActive(false);
            // 如果还没聊完就走开了，不标记为完成，下次回来还能聊
        }
    }

    public void OnInteract()
    {
        if (isFinished || DialogueManager.Instance.isDialogueActive) return;

        if (promptPanel != null) promptPanel.SetActive(false);

        // 开启对话，并告诉管理器：如果聊完了，记得回调我的 OnDialogueComplete 函数
        DialogueManager.Instance.StartDialogue(dialogueLines, this);
    }

    // 当对话真正结束时，由 DialogueManager 调用这个方法
    public void OnDialogueComplete()
    {
        isFinished = true; // 标记为已完成
        if (promptPanel != null) promptPanel.SetActive(false);
        Debug.Log(npcDisplayName + " 的对话已终结，不再触发。");
    }
}