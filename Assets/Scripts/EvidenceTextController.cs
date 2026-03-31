using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class EvidenceTextController : MonoBehaviour
{
    [Header("绑定设置")]
    public string targetEvidenceID; // 填 Poo2

    [Header("显示逻辑")]
    public bool showOnlyInFocus = false;

    [Header("文本内容")]
    [TextArea(2, 5)]
    public string originalText = "初始线索...";
    [TextArea(2, 5)]
    public string recalledText = "真相文字！";

    private TextMeshPro tmpText;
    private MeshRenderer meshRenderer;
    private bool isAlreadyRecalled = false;

    private void Awake()
    {
        tmpText = GetComponent<TextMeshPro>();
        meshRenderer = GetComponent<MeshRenderer>();
        tmpText.text = originalText;
    }

    private void Update()
    {
        HandleVisibility();
        // 如果还没变过色，就持续检测状态（万一你不想手动调用刷新，Update 也能兜底）
        if (!isAlreadyRecalled)
        {
            RefreshStatus();
        }
    }

    private void HandleVisibility()
    {
        if (FocusModeManager.Instance == null) return;

        if (showOnlyInFocus)
            meshRenderer.enabled = FocusModeManager.Instance.isFocusModeActive;
        else
            meshRenderer.enabled = true;
    }

    /// <summary>
    /// ✅ 核心：这是刚才漏掉的公有方法
    /// </summary>
    public void RefreshStatus()
    {
        if (isAlreadyRecalled) return;

        if (NoteManager.Instance != null && NoteManager.Instance.IsSearchCompleted(targetEvidenceID))
        {
            ApplyChange();
        }
    }

    private void ApplyChange()
    {
        isAlreadyRecalled = true;
        tmpText.text = recalledText;
        tmpText.color = new Color(1f, 0.85f, 0f); // 金色
        Debug.Log($"<color=yellow>[3D文字刷新]</color> {targetEvidenceID} 状态已更新！");
    }
}