using UnityEngine;
using UnityEngine.Events;

public class EvidenceReceiver : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("此模型对应需要的证据 ID（需与 NoteManager 中的 ID 一致）")]
    public string requiredEvidenceID;

    [Header("成功后的反馈")]
    public UnityEvent onSuccess; // 在 Inspector 里可以挂载成功后的逻辑，如播放动画、解锁等

    // 这个方法由 UI 脚本检测成功后调用
    public bool TryMatchEvidence(string incomingID)
    {
        if (incomingID == requiredEvidenceID)
        {
            Debug.Log($"<color=green>匹配成功！</color> 将 {incomingID} 应用到了 {gameObject.name}");
            onSuccess?.Invoke();
            return true;
        }

        Debug.Log($"<color=red>匹配失败。</color> 需要: {requiredEvidenceID}, 但你给了: {incomingID}");
        return false;
    }
}