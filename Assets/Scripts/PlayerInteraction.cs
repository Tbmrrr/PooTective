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
    private DoorInteractable currentDoor;

    void Update()
    {
        // --- 核心修复 1：增加笔记本打开状态的判断 ---
        bool isNoteOpen = NoteManager.Instance != null && NoteManager.Instance.notePanel.activeSelf;
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;

        // 如果正在对话，或者正在看笔记本，禁止一切 E 键交互
        if (isDialogueActive || isNoteOpen) return;

        // 统一交互逻辑 (按 E)
        if (Input.GetKeyDown(interactKey))
        {
            // 优先级判定：证物 > 门 > NPC
            if (currentEvidence != null)
            {
                currentEvidence.OnInteract();
                // --- 核心修复 2：不要在这里手动设为 null ---
                // 交给 OnTriggerExit 或者 Evidence 脚本内部的 hasInteracted 去控制逻辑
            }
            else if (currentDoor != null)
            {
                currentDoor.OnInteract();
            }
            else if (currentNPC != null)
            {
                currentNPC.OnInteract();
            }
        }

        // 2. 重建模式逻辑 (R) 保持不变
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
        // 检测证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null)
        {
            currentEvidence = evidence;
            currentEvidence.ShowPrompt(true);
            return; // 优先检测证物，检测到了就返回
        }

        // 检测门
        DoorInteractable door = other.GetComponent<DoorInteractable>();
        if (door != null)
        {
            currentDoor = door;
            currentDoor.ShowPrompt(true);
            return;
        }

        // 检测 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null)
        {
            currentNPC = npc;
            // 注意：NPCInteractable 内部自己会处理 pressEPrompt 的显示
            return;
        }

        // 检测 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null) { currentPoo = poo; return; }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null && evidence == currentEvidence)
        {
            currentEvidence.ShowPrompt(false);
            currentEvidence = null;
        }

        // 离开门
        DoorInteractable door = other.GetComponent<DoorInteractable>();
        if (door != null && door == currentDoor)
        {
            currentDoor.ShowPrompt(false);
            currentDoor = null;
        }

        // 离开 NPC
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC) currentNPC = null;

        // 离开 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null && poo == currentPoo) currentPoo = null;
    }
}