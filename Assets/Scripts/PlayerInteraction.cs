using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode pooKey = KeyCode.R;

    // 内部记录当前靠近的对象
    private NPCInteractable currentNPC;
    private PooInteractable currentPoo;
    private Evidence currentEvidence;
    private DoorInteractable currentDoor; // 新增：当前的门

    void Update()
    {
        // 1. 统一交互逻辑 (按 E)
        if (DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive)
        {
            if (currentEvidence != null && Input.GetKeyDown(interactKey))
            {
                currentEvidence.OnInteract();
                currentEvidence = null; // 拾取或交互后清空，防止循环
            }
            else if (currentDoor != null && Input.GetKeyDown(interactKey))
            {
                currentDoor.OnInteract(); // 执行开/关门
            }
            else if (currentNPC != null && Input.GetKeyDown(interactKey))
            {
                currentNPC.OnInteract();
            }
        }

        // 2. 重建模式与 Poo 交互 (R) —— 保留你原有的逻辑
        if (Input.GetKeyDown(pooKey))
        {
            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.hasUnlockedRebuild)
            {
                RebuildModeManager.Instance.ToggleRebuildMode();
            }
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
        if (npc != null) { currentNPC = npc; return; }

        // 检测 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null) { currentPoo = poo; return; }

        // 检测证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null)
        {
            currentEvidence = evidence;
            currentEvidence.ShowPrompt(true);
            return;
        }

        // 新增：检测门
        DoorInteractable door = other.GetComponent<DoorInteractable>();
        if (door != null)
        {
            currentDoor = door;
            currentDoor.ShowPrompt(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC) currentNPC = null;

        // 离开 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null && poo == currentPoo) currentPoo = null;

        // 离开证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null && evidence == currentEvidence)
        {
            currentEvidence.ShowPrompt(false);
            currentEvidence = null;
        }

        // 新增：离开门
        DoorInteractable door = other.GetComponent<DoorInteractable>();
        if (door != null && door == currentDoor)
        {
            currentDoor.ShowPrompt(false);
            currentDoor = null;
        }
    }
}