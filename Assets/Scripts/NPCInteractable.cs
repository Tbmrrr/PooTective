using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 条件质询响应
/// 根据前置条件显示不同的质询对话
/// </summary>
[System.Serializable]
public struct ConditionalEvidenceMapping
{
    public string evidenceID;
    public InteractionCondition[] conditions;
    [TextArea(5, 10)] public string[] responseDialogue;

    [Header("更新-证物")]
    public string updatedDescription;

    [Header("更新-角色档案 (新增)")]
    public string characterIDToUpdate; // 要更新的角色ID
    [TextArea(3, 5)] public string newCharacterDesc; // 该角色的新描述

    public string conditionDescription;
}

[System.Serializable]
public struct EvidenceMapping
{
    public string evidenceID;
    [TextArea(5, 10)] public string[] responseDialogue;

    public string updatedDescription;

    [Header("更新-角色档案 (新增)")]
    public string characterIDToUpdate;
    [TextArea(3, 5)] public string newCharacterDesc;
}

public class NPCInteractable : MonoBehaviour
{
    [Header("基础配置")]
    public TextAsset dialogueFile;
    public string npcDisplayName = "NPC";

    [Header("世界空间提示 (World Space UI)")]
    public GameObject pressEPrompt;
    public GameObject optionsMenu;
    // ✅ 新增：NPC 名字 UI 的引用
    [Tooltip("显示 NPC 名字的 UI 物体")]
    public GameObject npcNameUI;

    [Header("证物系统配置 - 简单模式")]
    [Tooltip("简单质询响应（无条件）")]
    public List<EvidenceMapping> evidenceResponses;

    [Header("证物系统配置 - 条件模式（高级）")]
    [Tooltip("条件质询响应：根据前置条件显示不同内容。优先级高于简单模式。")]
    public List<ConditionalEvidenceMapping> conditionalEvidenceResponses;

    [TextArea(2, 3)] public string[] defaultWrongResponse = { "证人：我不认识这个东西。" };

    private string[] normalDialogueLines;
    private bool isWaitingForChoice = false;
    private bool isDialogueJustFinished = false;

    private string pendingUpdateID;
    private string pendingUpdateDesc;

    // ✅ 新增：角色档案暂存
    private string pendingCharID;
    private string pendingCharDesc;

    void Start()
    {
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        // ✅ 初始隐藏名字
        if (npcNameUI != null) npcNameUI.SetActive(false);

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
            if (!isWaitingForChoice && !isDialogueJustFinished)
            {
                if (pressEPrompt != null) pressEPrompt.SetActive(true);
                // ✅ 走进范围显示名字
                if (npcNameUI != null) npcNameUI.SetActive(true);
            }
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
        // ✅ 交互开始时（选择菜单开启时）通常保持名字显示或根据需求关闭，这里保持一致性
        if (optionsMenu != null) optionsMenu.SetActive(true);

        SetPlayerMovement(false);
        isWaitingForChoice = true;
    }

    private void OnChoiceSelected(int choice)
    {
        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
        // ✅ 选好后进入对话，隐藏名字 UI 以免遮挡对话框
        if (npcNameUI != null) npcNameUI.SetActive(false);

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
        // ✅ 提交证物进入对话前隐藏名字
        if (npcNameUI != null) npcNameUI.SetActive(false);

        // ===== 记录质询历史 =====
        if (InteractionHistoryManager.Instance != null)
        {
            InteractionHistoryManager.Instance.RecordEvidencePresented(npcDisplayName, evidenceID);
        }

        // ===== 优先检查条件质询 =====
        if (conditionalEvidenceResponses != null && conditionalEvidenceResponses.Count > 0)
        {
            foreach (var condMapping in conditionalEvidenceResponses)
            {
                if (condMapping.evidenceID == evidenceID)
                {
                    // 检查条件
                    bool conditionsMet = InteractionHistoryManager.Instance != null
                        ? InteractionHistoryManager.Instance.CheckConditions(condMapping.conditions)
                        : (condMapping.conditions == null || condMapping.conditions.Length == 0);

                    if (conditionsMet)
                    {
                        Debug.Log($"[条件质询] {npcDisplayName} 使用条件响应: {condMapping.conditionDescription}");
                        pendingUpdateID = evidenceID;
                        pendingUpdateDesc = condMapping.updatedDescription;
                        // ✅ 记录角色更新
                        pendingCharID = condMapping.characterIDToUpdate;
                        pendingCharDesc = condMapping.newCharacterDesc;
                        DialogueManager.Instance.StartDialogue(condMapping.responseDialogue, this);
                        return;
                    }
                }
            }
        }

        // ===== 检查简单质询 =====
        foreach (var mapping in evidenceResponses)
        {
            if (mapping.evidenceID == evidenceID)
            {
                pendingUpdateID = evidenceID;
                pendingUpdateDesc = mapping.updatedDescription;
                // ✅ 记录角色更新
                pendingCharID = mapping.characterIDToUpdate;
                pendingCharDesc = mapping.newCharacterDesc;
                DialogueManager.Instance.StartDialogue(mapping.responseDialogue, this);
                return;
            }
        }

        // ===== 没有匹配的响应，使用默认错误响应 =====
        DialogueManager.Instance.StartDialogue(defaultWrongResponse, this);
    }

    public void OnDialogueComplete()
    {
        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        // ✅ 对话结束完全重置 UI 状态
        if (npcNameUI != null) npcNameUI.SetActive(false);

        isDialogueJustFinished = true;

        SetPlayerMovement(true);

        // 同时判断 ID 和描述都不为空，留空 updatedDescription 则不做任何更新
        if (!string.IsNullOrEmpty(pendingUpdateID) && !string.IsNullOrEmpty(pendingUpdateDesc))
        {
            NoteManager.Instance.UpdateEvidenceInfo(pendingUpdateID, pendingUpdateDesc);
        }
        // ✅ 新增：更新角色档案
        if (!string.IsNullOrEmpty(pendingCharID) && !string.IsNullOrEmpty(pendingCharDesc))
        {
            NoteManager.Instance.UpdateCharacterInfo(pendingCharID, pendingCharDesc);
        }
        // 清空所有暂存
        pendingUpdateID = null; pendingUpdateDesc = null;
        pendingCharID = null; pendingCharDesc = null;

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
        // ✅ 离开范围隐藏名字
        if (npcNameUI != null) npcNameUI.SetActive(false);
    }

    private void SetPlayerMovement(bool canMove)
    {
        Debug.Log(canMove ? "解锁移动" : "锁定移动并开启 1/2 选项");
    }
}