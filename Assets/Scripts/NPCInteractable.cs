using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct EvidenceMapping
{
    public string evidenceID;
    [TextArea(5, 10)] public string[] responseDialogue;
    public string updatedDescription;
}

public class NPCInteractable : MonoBehaviour
{
    [Header("基础配置")]
    public TextAsset dialogueFile;
    public string npcDisplayName = "NPC";

    [Header("世界空间提示 (World Space UI)")]
    public GameObject pressEPrompt;
    public GameObject optionsMenu;

    [Header("证物系统配置")]
    public List<EvidenceMapping> evidenceResponses;
    [TextArea(2, 3)] public string[] defaultWrongResponse = { "证人：我不认识这个东西。" };

    private string[] normalDialogueLines;
    private bool isWaitingForChoice = false;
    private bool isDialogueJustFinished = false; // 👆 新增：防止连续触发的锁

    private string pendingUpdateID;
    private string pendingUpdateDesc;

    void Start()
    {
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);

        if (dialogueFile != null)
        {
            normalDialogueLines = dialogueFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    void Update()
    {
        // 只有在等待选择时才监听 1 和 2
        if (isWaitingForChoice)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnChoiceSelected(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) OnChoiceSelected(2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !DialogueManager.Instance.isDialogueActive)
        {
            // 只有在没有对话、没有在选、且没有刚结束对话时才显示“按E”
            if (!isWaitingForChoice && !isDialogueJustFinished && pressEPrompt != null)
                pressEPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ResetInteraction();
        }
    }

    public void OnInteract()
    {
        // 核心修复：如果对话正在进行，或者刚刚结束（锁还没开），直接拦截
        if (DialogueManager.Instance.isDialogueActive || isWaitingForChoice || isDialogueJustFinished) return;

        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(true);

        SetPlayerMovement(false);
        isWaitingForChoice = true;
    }

    private void OnChoiceSelected(int choice)
    {
        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);

        if (choice == 1) StartNormalQuestion();
        else if (choice == 2) OpenEvidenceToPresent();
    }

    public void StartNormalQuestion()
    {
        if (normalDialogueLines != null && normalDialogueLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(normalDialogueLines, this);
        }
        else
        {
            OnDialogueComplete();
        }
    }

    public void OpenEvidenceToPresent()
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.EnterPresentMode(this);
        }
    }

    public void ReceiveEvidence(string evidenceID)
    {
        if (optionsMenu != null) optionsMenu.SetActive(false);

        foreach (var mapping in evidenceResponses)
        {
            if (mapping.evidenceID == evidenceID)
            {
                pendingUpdateID = evidenceID;
                pendingUpdateDesc = mapping.updatedDescription;
                DialogueManager.Instance.StartDialogue(mapping.responseDialogue, this);
                return;
            }
        }
        DialogueManager.Instance.StartDialogue(defaultWrongResponse, this);
    }

    // --- 当对话框消失时回调 ---
    public void OnDialogueComplete()
    {
        // 1. 立即清理所有 UI
        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);

        // 2. 开启“防止误触发”锁
        isDialogueJustFinished = true;

        // 3. 恢复玩家移动
        SetPlayerMovement(true);

        // 4. 处理数据更新
        if (!string.IsNullOrEmpty(pendingUpdateID))
        {
            NoteManager.Instance.UpdateEvidenceInfo(pendingUpdateID, pendingUpdateDesc);
            pendingUpdateID = null;
            pendingUpdateDesc = null;
        }

        // 5. 延迟一小段时间后解锁，让系统有时间处理按键释放
        Invoke("ReleaseDialogueLock", 0.5f);
    }

    private void ReleaseDialogueLock()
    {
        isDialogueJustFinished = false;
        // 如果玩家还在范围内，重新显示“按E”
        // 这里可以根据实际需要决定是否重新显示
    }

    private void ResetInteraction()
    {
        isWaitingForChoice = false;
        isDialogueJustFinished = false;
        SetPlayerMovement(true);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
    }

    private void SetPlayerMovement(bool canMove)
    {
        // 实际对接你的玩家控制脚本
        Debug.Log(canMove ? "解锁移动" : "锁定移动并开启 1/2 选项");
    }
}