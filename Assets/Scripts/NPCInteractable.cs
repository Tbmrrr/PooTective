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
    private bool isDialogueJustFinished = false;

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

    public void OnDialogueComplete()
    {
        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);

        isDialogueJustFinished = true;

        SetPlayerMovement(true);

        // ✅ 修复：同时判断 ID 和描述都不为空，留空 updatedDescription 则不做任何更新
        if (!string.IsNullOrEmpty(pendingUpdateID) && !string.IsNullOrEmpty(pendingUpdateDesc))
        {
            NoteManager.Instance.UpdateEvidenceInfo(pendingUpdateID, pendingUpdateDesc);
        }
        // 无论是否更新，都清空暂存数据
        pendingUpdateID = null;
        pendingUpdateDesc = null;

        Invoke("ReleaseDialogueLock", 0.5f);
    }

    private void ReleaseDialogueLock()
    {
        isDialogueJustFinished = false;
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
        Debug.Log(canMove ? "解锁移动" : "锁定移动并开启 1/2 选项");
    }
}