using UnityEngine;

public class DebugEvidenceDataChanger : MonoBehaviour
{
    [Header("调试配置")]
    public KeyCode debugKey = KeyCode.P;

    [Tooltip("想要修改的证物 ID（必须与 Evidence 脚本及背包内的 ID 一致）")]
    public string targetEvidenceID = "Poo_01";

    [Tooltip("新的证物描述内容")]
    [TextArea(5, 10)]
    public string newTestDescription = "<color=#FF4500>【线索更新】</color>\n这是通过调试脚本强制修改后的描述内容。";

    void Update()
    {
        // 按下 P 键触发
        if (Input.GetKeyDown(debugKey))
        {
            TryUpdateEvidence();
        }
    }

    private void TryUpdateEvidence()
    {
        if (NoteManager.Instance == null)
        {
            Debug.LogError("场景中缺少 NoteManager 实例！");
            return;
        }

        // 1. 先检查背包里有没有这个东西
        if (NoteManager.Instance.HasEvidence(targetEvidenceID))
        {
            // 2. 调用你代码里现成的 UpdateEvidenceDescDirectly 方法
            NoteManager.Instance.UpdateEvidenceDescDirectly(targetEvidenceID, newTestDescription);

            Debug.Log($"<color=orange>[Debug]</color> 证物 {targetEvidenceID} 的描述已成功修改。");
            Debug.Log("提示：请在背包中重新点击该证物以刷新 UI 显示。");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Debug]</color> 修改失败：背包里没有 ID 为 {targetEvidenceID} 的证物。请先在游戏中拾取它！");
        }
    }
}