using UnityEngine;

public class PooInteractable : MonoBehaviour
{
    [Header("UI 提示 (World Space)")]
    public GameObject rPromptObject;

    [Header("引用的证物组件")]
    public Evidence evidenceComponent;

    private bool isPlayerInZone = false;
    private bool hasAddedToNote = false; // 内部记录，防止单次运行重复加背包

    void Start()
    {
        if (rPromptObject != null) rPromptObject.SetActive(false);
        if (evidenceComponent == null) evidenceComponent = GetComponent<Evidence>();
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.R))
        {
            if (FocusModeManager.Instance != null && FocusModeManager.Instance.isFocusModeActive)
            {
                OnPooInteract();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            if (FocusModeManager.Instance != null && FocusModeManager.Instance.isFocusModeActive)
            {
                if (rPromptObject != null) rPromptObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (rPromptObject != null) rPromptObject.SetActive(false);
        }
    }

    public void OnPooInteract()
    {
        // 1. 重建模式逻辑：直接调用你之前能跑通的方法
        if (RebuildModeManager.Instance != null)
        {
            // 统一调用 UnlockAndEnter，它内部应该已经处理了“已解锁则直接进入”的逻辑
            RebuildModeManager.Instance.UnlockAndEnter();
        }

        // 2. 背包逻辑：只在第一次交互时添加
        if (!hasAddedToNote && evidenceComponent != null)
        {
            if (InteractionHistoryManager.Instance != null)
            {
                InteractionHistoryManager.Instance.RecordEvidenceInteraction(evidenceComponent.evidenceID);
            }

            if (NoteManager.Instance != null)
            {
                NoteManager.Instance.AddEvidence(evidenceComponent);
            }

            hasAddedToNote = true; // 设为 true 后，下次按 R 就不会再进这个 if 了
            Debug.Log($"[Poo] {evidenceComponent.evidenceName} 已加入背包。");
        }
    }

    private void OnDisable()
    {
        if (rPromptObject != null) rPromptObject.SetActive(false);
    }
}