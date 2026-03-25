using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode interactKey = KeyCode.E; // 统一叫交互键
    public KeyCode pooKey = KeyCode.R;

    private NPCInteractable currentNPC;
    private PooInteractable currentPoo;
    private Evidence currentEvidence; // 新增：记录当前靠近的证物

    void Update()
    {
        // 1. 交互逻辑 (按 E)
        if (DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive)
        {
            // 优先检测证物，其次是 NPC
            if (currentEvidence != null && Input.GetKeyDown(interactKey))
            {
                currentEvidence.OnInteract();
            }
            else if (currentNPC != null && Input.GetKeyDown(interactKey))
            {
                currentNPC.OnInteract();
            }
        }

        // 2. 重建模式 (R) —— 保持你原有的逻辑不变
        if (Input.GetKeyDown(pooKey))
        {
            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.hasUnlockedRebuild)
            {
                RebuildModeManager.Instance.ToggleRebuildMode();
            }
            else if (FocusModeManager.Instance != null && FocusModeManager.Instance.isFocusModeActive)
            {
                if (currentPoo != null) currentPoo.OnPooInteract();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检测 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null) { currentNPC = npc; return; }

        // 检测 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null) { currentPoo = poo; return; }

        // 新增：检测证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null)
        {
            currentEvidence = evidence;
            currentEvidence.ShowPrompt(true); // 显示提示
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开逻辑保持同步
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC) currentNPC = null;

        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null && poo == currentPoo) currentPoo = null;

        // 新增：离开证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null && evidence == currentEvidence)
        {
            currentEvidence.ShowPrompt(false); // 隐藏提示
            currentEvidence = null;
        }
    }
}