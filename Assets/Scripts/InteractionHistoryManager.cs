using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 交互历史管理器
/// 记录玩家已经交互过的证物和已经质询过的NPC证物组合
/// </summary>
public class InteractionHistoryManager : MonoBehaviour
{
    public static InteractionHistoryManager Instance { get; private set; }

    // 已交互过的证物ID集合
    private HashSet<string> interactedEvidences = new HashSet<string>();

    // 已质询过的组合：NPC名称 + 证物ID
    private HashSet<string> presentedEvidences = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 记录已交互的证物
    /// </summary>
    public void RecordEvidenceInteraction(string evidenceID)
    {
        if (!string.IsNullOrEmpty(evidenceID))
        {
            interactedEvidences.Add(evidenceID);
            Debug.Log($"[交互历史] 已记录证物交互: {evidenceID}");
        }
    }

    /// <summary>
    /// 检查是否已交互过某证物
    /// </summary>
    public bool HasInteractedWith(string evidenceID)
    {
        return interactedEvidences.Contains(evidenceID);
    }

    /// <summary>
    /// 记录已向NPC出示过的证物
    /// </summary>
    public void RecordEvidencePresented(string npcName, string evidenceID)
    {
        string key = $"{npcName}_{evidenceID}";
        presentedEvidences.Add(key);
        Debug.Log($"[交互历史] 已记录质询: {npcName} - {evidenceID}");
    }

    /// <summary>
    /// 检查是否已向某NPC出示过某证物
    /// </summary>
    public bool HasPresentedTo(string npcName, string evidenceID)
    {
        string key = $"{npcName}_{evidenceID}";
        return presentedEvidences.Contains(key);
    }

    /// <summary>
    /// 批量检查是否满足所有前置条件
    /// </summary>
    public bool CheckConditions(InteractionCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true; // 没有条件则默认满足

        foreach (var condition in conditions)
        {
            if (!CheckSingleCondition(condition))
            {
                return false; // 只要有一个不满足就返回false
            }
        }
        return true; // 所有条件都满足
    }

    /// <summary>
    /// 检查单个条件
    /// </summary>
    private bool CheckSingleCondition(InteractionCondition condition)
    {
        switch (condition.type)
        {
            case ConditionType.InteractedWithEvidence:
                return HasInteractedWith(condition.evidenceID);

            case ConditionType.PresentedToNPC:
                return HasPresentedTo(condition.npcName, condition.evidenceID);

            default:
                return true;
        }
    }

    /// <summary>
    /// 清空所有历史（用于重置游戏或测试）
    /// </summary>
    public void ClearHistory()
    {
        interactedEvidences.Clear();
        presentedEvidences.Clear();
        Debug.Log("[交互历史] 已清空所有历史记录");
    }

    // ===== Debug 功能 =====
    public void PrintHistory()
    {
        Debug.Log("=== 交互历史记录 ===");
        Debug.Log($"已交互证物: {string.Join(", ", interactedEvidences)}");
        Debug.Log($"已质询记录: {string.Join(", ", presentedEvidences)}");
    }
}

/// <summary>
/// 条件类型枚举
/// </summary>
public enum ConditionType
{
    InteractedWithEvidence,  // 已交互过某证物
    PresentedToNPC          // 已向某NPC出示过某证物
}

/// <summary>
/// 单个交互条件
/// </summary>
[System.Serializable]
public struct InteractionCondition
{
    [Tooltip("条件类型")]
    public ConditionType type;

    [Tooltip("证物ID（用于两种条件类型）")]
    public string evidenceID;

    [Tooltip("NPC名称（仅用于'已质询过某NPC'条件）")]
    public string npcName;
}