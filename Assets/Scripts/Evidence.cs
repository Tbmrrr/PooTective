using UnityEngine;
using System.Collections.Generic;

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

    [Header("搜索功能")]
    [Tooltip("填写后该证物启用搜索功能")]
    public List<SearchableKeyword> searchableKeywords = new List<SearchableKeyword>();
    // 是否有搜索功能
    public bool HasSearchFeature => searchableKeywords != null && searchableKeywords.Count > 0;

    [Header("调查对话 (交互时弹出)")]
    [Tooltip("调查时显示的文字，会自动加上'侦探：'")]
    public string[] interactLines;

    [Header("交互反馈")]
    public GameObject interactPrompt;

    private bool hasInteracted = false;

    private void Start()
    {
        hasInteracted = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    public virtual void OnInteract()
    {
        if (hasInteracted)
        {
            Debug.Log(evidenceName + " 已经调查过了，不再响应。");
            return;
        }

        hasInteracted = true;

        if (DialogueManager.Instance != null && interactLines != null && interactLines.Length > 0)
        {
            string[] formattedLines = new string[interactLines.Length];
            for (int i = 0; i < interactLines.Length; i++)
            {
                formattedLines[i] = "侦探：" + interactLines[i];
            }
            DialogueManager.Instance.StartDialogue(formattedLines, null);
        }

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.AddEvidence(this);
            Debug.Log(evidenceName + " 已加入笔记本。ID: " + evidenceID);
        }

        ShowPrompt(false);
    }

    public void ShowPrompt(bool show)
    {
        if (hasInteracted)
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(show);
            if (show) Debug.Log("靠近了证物：" + evidenceName);
        }
    }
}