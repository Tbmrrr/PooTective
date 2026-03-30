using UnityEngine;

public class PooInteractable : MonoBehaviour
{
    [Header("设置：该物体对应哪个重建节点？")]
    [Tooltip("1 代表 Poo1 (节点1)，2 代表 Poo2 (节点2)")]
    public int targetNode = 1;

    [Header("UI 提示图片 (World Space)")]
    public GameObject rPromptObject;

    [Header("引用的证物组件")]
    public Evidence evidenceComponent;

    private bool isPlayerInZone = false;
    private bool hasAddedToNote = false;

    void Start()
    {
        ResetState();
        if (evidenceComponent == null) evidenceComponent = GetComponent<Evidence>();
    }

    // ✅ 新增：重置状态的方法，供外部或内部初始化调用
    public void ResetState()
    {
        isPlayerInZone = false;
        if (rPromptObject != null) rPromptObject.SetActive(false);
    }

    void Update()
    {
        // 判定显示逻辑：范围内 + 专注模式开启 + 还没交互过
        bool canShow = isPlayerInZone &&
                       FocusModeManager.Instance != null &&
                       FocusModeManager.Instance.isFocusModeActive &&
                       !hasAddedToNote;

        if (rPromptObject != null) rPromptObject.SetActive(canShow);

        // ✅ 核心修改：如果当前可以触发特殊交互，我们在这里“截断”输入
        if (canShow && Input.GetKeyDown(KeyCode.R))
        {
            // 告诉系统，我们正在处理特殊的证物 R 键逻辑
            OnPooInteract();

            // 这一帧的 R 键已经被我们消耗掉了
            return;
        }
    }

    public void OnPooInteract()
    {
        if (RebuildModeManager.Instance == null) return;

        // 1. 处理特殊重建逻辑
        if (targetNode == 1)
        {
            RebuildModeManager.Instance.UnlockAndEnter();
        }
        else if (targetNode == 2)
        {
            // ✅ 先解锁节点2，再进入，最后强制切换坐标
            RebuildModeManager.Instance.UnlockTimeNode2();
            RebuildModeManager.Instance.UnlockAndEnter();
            RebuildModeManager.Instance.OnNode2Clicked();
            Debug.Log("<color=green>[Poo2]</color> 特殊 R 键交互：解锁并跳转至节点 2");
        }

        // 2. 静默入包
        if (!hasAddedToNote && evidenceComponent != null)
        {
            if (NoteManager.Instance != null && !NoteManager.Instance.HasEvidence(evidenceComponent.evidenceID))
            {
                NoteManager.Instance.AddEvidence(evidenceComponent);
                if (InteractionHistoryManager.Instance != null)
                {
                    InteractionHistoryManager.Instance.RecordEvidenceInteraction(evidenceID: evidenceComponent.evidenceID);
                }
                hasAddedToNote = true;
            }
        }

        // 交互完关闭提示
        if (rPromptObject != null) rPromptObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ResetState();
        }
    }

    // ✅ 当物体被隐藏或销毁时（比如场景重置），重置交互状态
    private void OnDisable()
    {
        ResetState();
    }
}