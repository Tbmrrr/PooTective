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
    public string characterIDToUpdate;
    [TextArea(3, 5)] public string newCharacterDesc;

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
    [Tooltip("显示 NPC 名字的 UI 物体")]
    public GameObject npcNameUI;

    [Header("选项按钮 RectTransform")]
    public RectTransform choice1Rect;
    public RectTransform choice2Rect;

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

    private string pendingCharID;
    private string pendingCharDesc;

    private bool isPlayerInRange = false;

    private float choosingTimer = 0f;

    // ✅ 全局标志：玩家正在选择对话选项（按下E后、选1/2前）
    public static bool isChoosingOption = false;

    void Start()
    {
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (npcNameUI != null) npcNameUI.SetActive(false);

        if (dialogueFile != null)
        {
            normalDialogueLines = dialogueFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    void Update()
    {
        if (isChoosingOption)
        {
            choosingTimer += Time.deltaTime;

            if (choosingTimer > 10f)
            {
                Debug.LogError("[NPCInteractable] Choosing timeout. Force reset.");
                isChoosingOption = false;
                isWaitingForChoice = false;
                choosingTimer = 0f;
            }
        }
        else
        {
            choosingTimer = 0f;
        }
        if (isWaitingForChoice)
        {
            // 键盘
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnChoiceSelected(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) OnChoiceSelected(2);

            // 鼠标点击（使用 RectTransform 检测，不依赖 Collider）
            if (Input.GetMouseButtonDown(0))
            {
                if (choice1Rect != null && RectTransformUtility.RectangleContainsScreenPoint(
                    choice1Rect, Input.mousePosition, Camera.main))
                {
                    OnChoiceSelected(1);
                }
                else if (choice2Rect != null && RectTransformUtility.RectangleContainsScreenPoint(
                    choice2Rect, Input.mousePosition, Camera.main))
                {
                    OnChoiceSelected(2);
                }
            }
        }
    }

    private void OnDisable()
    {
        ForceReleaseInteraction("OnDisable");
    }

    private void OnDestroy()
    {
        ForceReleaseInteraction("OnDestroy");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !DialogueManager.Instance.isDialogueActive)
        {
            isPlayerInRange = true;
            if (!isWaitingForChoice && !isDialogueJustFinished)
            {
                if (pressEPrompt != null) pressEPrompt.SetActive(true);
                if (npcNameUI != null) npcNameUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            ResetInteraction();
        }
    }


    public void OnInteract()
    {
        Debug.Log($"OnInteract | dialogueActive={DialogueManager.Instance.isDialogueActive} | isWaiting={isWaitingForChoice} | justFinished={isDialogueJustFinished}");
        if (DialogueManager.Instance.isDialogueActive || isWaitingForChoice || isDialogueJustFinished) return;

        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(true);

        // ✅ 按下E的瞬间就锁视角、释放鼠标
        isChoosingOption = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerMovement(false);
        isWaitingForChoice = true;
    }

    // ✅ 供 UI Button OnClick 调用
    public void OnClickChoice1() { if (isWaitingForChoice) OnChoiceSelected(1); }
    public void OnClickChoice2() { if (isWaitingForChoice) OnChoiceSelected(2); }

    private void OnChoiceSelected(int choice)
    {
        // ✅ 选完后交由对话/背包状态接管，清除选项标志
        isChoosingOption = false;

        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
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
        if (npcNameUI != null) npcNameUI.SetActive(false);

        if (InteractionHistoryManager.Instance != null)
        {
            InteractionHistoryManager.Instance.RecordEvidencePresented(npcDisplayName, evidenceID);
        }

        if (conditionalEvidenceResponses != null && conditionalEvidenceResponses.Count > 0)
        {
            foreach (var condMapping in conditionalEvidenceResponses)
            {
                if (condMapping.evidenceID == evidenceID)
                {
                    bool conditionsMet = InteractionHistoryManager.Instance != null
                        ? InteractionHistoryManager.Instance.CheckConditions(condMapping.conditions)
                        : (condMapping.conditions == null || condMapping.conditions.Length == 0);

                    if (conditionsMet)
                    {
                        Debug.Log($"[条件质询] {npcDisplayName} 使用条件响应: {condMapping.conditionDescription}");
                        pendingUpdateID = evidenceID;
                        pendingUpdateDesc = condMapping.updatedDescription;
                        pendingCharID = condMapping.characterIDToUpdate;
                        pendingCharDesc = condMapping.newCharacterDesc;
                        DialogueManager.Instance.StartDialogue(condMapping.responseDialogue, this);
                        return;
                    }
                }
            }
        }

        foreach (var mapping in evidenceResponses)
        {
            if (mapping.evidenceID == evidenceID)
            {
                pendingUpdateID = evidenceID;
                pendingUpdateDesc = mapping.updatedDescription;
                pendingCharID = mapping.characterIDToUpdate;
                pendingCharDesc = mapping.newCharacterDesc;
                DialogueManager.Instance.StartDialogue(mapping.responseDialogue, this);
                return;
            }
        }

        DialogueManager.Instance.StartDialogue(defaultWrongResponse, this);
    }

    public void OnDialogueComplete()
    {
        // ✅ 对话结束时也确保清除标志（防止异常状态残留）
        isChoosingOption = false;

        isWaitingForChoice = false;
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (npcNameUI != null) npcNameUI.SetActive(false);

        isDialogueJustFinished = true;

        SetPlayerMovement(true);

        if (!string.IsNullOrEmpty(pendingUpdateID) && !string.IsNullOrEmpty(pendingUpdateDesc))
        {
            NoteManager.Instance.UpdateEvidenceInfo(pendingUpdateID, pendingUpdateDesc);
        }
        if (!string.IsNullOrEmpty(pendingCharID) && !string.IsNullOrEmpty(pendingCharDesc))
        {
            NoteManager.Instance.UpdateCharacterInfo(pendingCharID, pendingCharDesc);
        }

        pendingUpdateID = null; pendingUpdateDesc = null;
        pendingCharID = null; pendingCharDesc = null;

        Invoke("ReleaseDialogueLock", 0.5f);
    }

    private void ReleaseDialogueLock()
    {
        isDialogueJustFinished = false;

        // 如果玩家还在范围内，重新显示按E提示
        if (isPlayerInRange)
        {
            if (pressEPrompt != null) pressEPrompt.SetActive(true);
            if (npcNameUI != null) npcNameUI.SetActive(true);
        }
    }

    private void ResetInteraction()
    {
        // ✅ 离开范围也要清除标志，防止玩家跑出范围后视角永久锁死
        isChoosingOption = false;

        isWaitingForChoice = false;
        isDialogueJustFinished = false;
        SetPlayerMovement(true);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (npcNameUI != null) npcNameUI.SetActive(false);
    }

    private void ForceReleaseInteraction(string reason)
    {
        if (!isChoosingOption && !isWaitingForChoice && !isDialogueJustFinished)
        {
            return;
        }

        Debug.LogWarning($"[NPCInteractable] ForceReleaseInteraction triggered by {reason} on {name}");

        isChoosingOption = false;
        isWaitingForChoice = false;
        isDialogueJustFinished = false;

        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (npcNameUI != null) npcNameUI.SetActive(false);
    }

    private void SetPlayerMovement(bool canMove)
    {
        Debug.Log(canMove ? "解锁移动" : "锁定移动并开启 1/2 选项");
    }

    // ✅ 供背包/证物界面关闭时调用，恢复按E提示
    public void OnPresentModeCancelled()
    {
        Debug.Log($"OnPresentModeCancelled 被调用 | isPlayerInRange={isPlayerInRange}");
        // ...其余不变
        isWaitingForChoice = false;
        isChoosingOption = false;
        isDialogueJustFinished = false;

        SetPlayerMovement(true);

        if (isPlayerInRange)
        {
            if (pressEPrompt != null) pressEPrompt.SetActive(true);
            if (npcNameUI != null) npcNameUI.SetActive(true);
        }
    }
}