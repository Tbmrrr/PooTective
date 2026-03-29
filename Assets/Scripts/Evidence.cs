using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class SearchableKeyword
{
    [Tooltip("高亮显示的词组")]
    public string keyword;

    [Tooltip("拖入搜索框后显示的搜索结果")]
    [TextArea(2, 5)]
    public string searchResult;

    [Tooltip("搜索后追加到证物描述的内容（留空则不更新描述）")]
    [TextArea(2, 5)]
    public string descAppendOnSearch;
}


[System.Serializable]
public class ConditionalDialogue
{
    [Tooltip("触发此对话的前置条件（留空 = 无条件，作为默认对话）")]
    public InteractionCondition[] conditions;

    [Tooltip("满足条件时显示的对话")]
    [TextArea(3, 5)]
    public string[] dialogueLines;

    [Tooltip("条件说明（仅用于Inspector中方便查看，不影响逻辑）")]
    public string conditionDescription;
}

public class Evidence : MonoBehaviour
{
    [Header("核心逻辑设置")]
    public string evidenceID;

    public bool canPickUp = true;
    public bool canAskNPC = true;

    // ✅ 新增：特殊功能开关
    [Header("特殊功能")]
    [Tooltip("勾选后，第一次交互将解锁重建模式的时间节点2")]
    public bool unlockRebuildNode2 = false;

    [Header("证物基本信息 (用于背包)")]
    public string evidenceName;
    public Sprite evidenceIcon;
    public Sprite evidenceFullImage;

    [TextArea(3, 5)]
    public string description;

    [Header("搜索功能")]
    public List<SearchableKeyword> searchableKeywords = new List<SearchableKeyword>();
    public bool HasSearchFeature => searchableKeywords != null && searchableKeywords.Count > 0;

    [Header("调查对话 - 简单模式（无条件）")]
    public string[] interactLines;

    [Header("调查对话 - 条件模式（高级）")]
    public ConditionalDialogue[] conditionalDialogues;

    [Header("3D展示模型")]
    [Tooltip("这里请拖入你调好角度（如 0, 90, 0）的那个子物体模型")]
    public GameObject displayModel;

    [Header("交互反馈")]
    public GameObject interactPrompt;

    private bool hasInteracted = false;
    private bool isShowingEvidence = false;

    // 本地交互锁，防止按键过快导致的重复触发
    private bool isProcessing = false;

    private void Start()
    {
        hasInteracted = false;
        isProcessing = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    public virtual void OnInteract()
    {
        // 1. 如果正在处理交互中，或者对话管理器已经处于激活状态，直接拦截
        if (isProcessing) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

        // 2. 上锁：表示当前正在启动交互流程
        isProcessing = true;

        bool isFirstTime = !hasInteracted;

        if (isFirstTime)
        {
            hasInteracted = true;

            // ✅ 处理时间节点解锁逻辑
            if (unlockRebuildNode2 && RebuildModeManager.Instance != null)
            {
                RebuildModeManager.Instance.UnlockTimeNode2();
            }

            // ✅ 只有在勾选了 canPickUp 时，才记录到历史并加入背包
            if (canPickUp)
            {
                if (InteractionHistoryManager.Instance != null)
                {
                    InteractionHistoryManager.Instance.RecordEvidenceInteraction(evidenceID);
                }

                if (NoteManager.Instance != null)
                {
                    NoteManager.Instance.AddEvidence(this);
                }
                Debug.Log($"[Evidence] {evidenceName} 已加入背包。");
            }
            else
            {
                Debug.Log($"[Evidence] {evidenceName} 仅供调查，不加入背包。");
            }
        }

        // ===== ✅ 核心展示逻辑：使用你调好的 Rotation =====
        if (EvidenceDisplayManager.Instance != null && displayModel != null)
        {
            // 获取你在场景/Inspector里给这个子模型调好的角度（比如报纸的 0, 90, 0）
            Quaternion customRotation = displayModel.transform.localRotation;

            // ：只传模型和起点（Transform）
            EvidenceDisplayManager.Instance.ShowEvidence(displayModel, transform);
            isShowingEvidence = true;
        }

        // ===== 获取并启动对话 =====
        string[] dialogueToShow = GetDialogueToShow();

        if (DialogueManager.Instance != null && dialogueToShow != null && dialogueToShow.Length > 0)
        {
            string[] formattedLines = new string[dialogueToShow.Length];
            for (int i = 0; i < dialogueToShow.Length; i++)
            {
                formattedLines[i] = "侦探：" + dialogueToShow[i];
            }

            // 启动对话
            DialogueManager.Instance.StartDialogue(formattedLines, null);

            // 3. 启动协程等待对话结束
            StartCoroutine(WaitForDialogueEnd());
        }
        else
        {
            // 如果没有对话，直接解锁
            isProcessing = false;
        }

        ShowPrompt(false);
    }

    private IEnumerator WaitForDialogueEnd()
    {
        // 先等待一帧，确保 DialogueManager 已经正确设置了 isDialogueActive 状态
        yield return null;

        // 循环等待：只要对话还在继续，就一直停在这里
        while (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            yield return null;
        }

        // 对话结束后执行清理逻辑
        OnDialogueComplete();

        // 4. 解锁：对话完全结束并清理完证物展示后，才允许下一次交互
        isProcessing = false;
    }

    private string[] GetDialogueToShow()
    {
        if (conditionalDialogues != null && conditionalDialogues.Length > 0)
        {
            foreach (var condDialogue in conditionalDialogues)
            {
                bool conditionsMet = InteractionHistoryManager.Instance != null
                    ? InteractionHistoryManager.Instance.CheckConditions(condDialogue.conditions)
                    : (condDialogue.conditions == null || condDialogue.conditions.Length == 0);

                if (conditionsMet)
                {
                    Debug.Log($"[证物对话] {evidenceName} 使用条件对话: {condDialogue.conditionDescription}");
                    return condDialogue.dialogueLines;
                }
            }
        }

        return interactLines;
    }

    private void OnDialogueComplete()
    {
        if (isShowingEvidence)
        {
            if (EvidenceDisplayManager.Instance != null)
            {
                EvidenceDisplayManager.Instance.HideEvidence();
            }
            isShowingEvidence = false;
        }
    }

    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(show);
        }
    }
}