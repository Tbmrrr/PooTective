using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionTrigger : MonoBehaviour
{
    public InteractionCondition[] conditions; // 需要满足的条件
    private EvidenceReceiver receiver;

    void Awake() => receiver = GetComponent<EvidenceReceiver>();

    void Update()
    {
        // 1. 如果 receiver 为空（没拿到组件），直接返回
        if (receiver == null) return;

        // 2. 如果已经激活了，就没必要再跑了，直接禁用这个脚本节省性能
        if (receiver.isActivated)
        {
            this.enabled = false;
            return;
        }

        // 3. ✅ 核心修复：检查单例是否存在
        // 如果管理器还没加载出来，先跳过这一帧，等它加载好
        if (InteractionHistoryManager.Instance == null) return;

        // 4. 检查条件
        if (InteractionHistoryManager.Instance.CheckConditions(conditions))
        {
            receiver.ActivateQuestion();
        }
    }
}