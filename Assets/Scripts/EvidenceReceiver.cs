using UnityEngine;
using UnityEngine.Events;

public class EvidenceReceiver : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("此位置需要的 ID（可以是证物 ID，也可以是员工 ID）")]
    public string requiredID;

    [Tooltip("匹配成功后是否立即销毁/隐藏当前取出的 UI 物品")]
    public bool hideTakenOutItemOnSuccess = true;

    [Header("成功反馈")]
    public UnityEvent onSuccess;

    // ✅ 修改后的匹配方法：支持传入 ID
    public bool TryMatch(string incomingID)
    {
        if (string.IsNullOrEmpty(incomingID)) return false;

        if (incomingID == requiredID)
        {
            Debug.Log($"<color=green>[匹配成功]</color> 目标 {gameObject.name} 已接收: {incomingID}");

            // 执行成功后的逻辑（如：员工站到了正确位置 / 证物放回了原处）
            onSuccess?.Invoke();

            // 如果匹配成功，通知 UI 关闭悬浮图标
            if (hideTakenOutItemOnSuccess && TakenOutEvidenceUI.Instance != null)
            {
                TakenOutEvidenceUI.Instance.Close();
            }

            return true;
        }

        Debug.Log($"<color=yellow>[匹配失败]</color> {gameObject.name} 需要 {requiredID}，但接收到的是 {incomingID}");
        return false;
    }
}