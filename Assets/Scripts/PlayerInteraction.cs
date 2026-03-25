using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode npcKey = KeyCode.E;
    public KeyCode pooKey = KeyCode.R; // 新增：Poo 的交互按键

    // 内部记录当前靠近的对象
    private NPCInteractable currentNPC;
    private PooInteractable currentPoo; // 新增：记录当前的 Poo

    void Update()
    {
        // 1. NPC 交互逻辑 (按 E)
        if (DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive)
        {
            if (currentNPC != null && Input.GetKeyDown(npcKey))
            {
                currentNPC.OnInteract();
            }
        }

        // 2. 重建模式与 Poo 交互 (R)
        if (Input.GetKeyDown(pooKey))
        {
            // 情况 A：已经解锁了模式，随时可以开关 (不依赖 Focus 模式或靠近 Poo)
            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.hasUnlockedRebuild)
            {
                RebuildModeManager.Instance.ToggleRebuildMode();
            }
            // 情况 B：还没解锁，必须在 Focus 模式下靠近 Poo 才能按 R
            else if (FocusModeManager.Instance != null && FocusModeManager.Instance.isFocusModeActive)
            {
                if (currentPoo != null)
                {
                    currentPoo.OnPooInteract();
                }
            }
        }
    }

    // --- 触发器检测 ---

    private void OnTriggerEnter(Collider other)
    {
        // 检测 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null)
        {
            currentNPC = npc;
            return;
        }

        // 检测 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null)
        {
            currentPoo = poo;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC)
        {
            currentNPC = null;
        }

        // 离开 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null && poo == currentPoo)
        {
            currentPoo = null;
        }
    }
}