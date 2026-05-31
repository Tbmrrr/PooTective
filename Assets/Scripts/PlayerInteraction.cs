using UnityEngine;
using System.Linq;
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
        bool isNoteOpen = NoteManager.Instance != null
            && NoteManager.Instance.notePanel != null
            && NoteManager.Instance.notePanel.activeSelf;
        bool isDialogueActive = DialogueManager.Instance != null
            && DialogueManager.Instance.isDialogueActive;

        if (isDialogueActive || isNoteOpen) return;

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log($"E pressed | NPC={currentNPC} | Evidence={currentEvidence} | dialogue={isDialogueActive} | note={isNoteOpen} | choosing={NPCInteractable.isChoosingOption}");

            if (currentEvidence != null)
            {
                currentEvidence.OnInteract();
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

        if (Input.GetKeyDown(pooKey))
        {
            if (currentEvidence != null && currentEvidence.evidenceID == "Poo2")
            {
                currentEvidence.OnSpecialInteractR();
                currentEvidence = null;
                return;
            }

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
        Debug.Log($"OnTriggerEnter: {other.gameObject.name}");
        // 检测证物
        Evidence evidence = other.GetComponent<Evidence>();
        if (evidence != null)
        {
            currentEvidence = evidence;
            currentEvidence.ShowPrompt(true);
            return; // 优先检测证物，检测到了就返回
        }

        // 检测门
        DoorInteractable door = other.GetComponentInParent<DoorInteractable>();
        if (door != null)
        {
            Debug.Log($"检测到门: {door.gameObject.name}");
            currentDoor = door;
            currentDoor.ShowPrompt(true);
            door.OnInteract();
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
        if (npc != null && npc == currentNPC)
        {
            // ✅ 额外确认：只有玩家真的离开 NPC 附近才清空
            // 防止 StaffList 等其他物体消失时误触发 Exit 把 currentNPC 清掉
            NPCInteractable[] nearbyNPCs = Physics.OverlapSphere(transform.position, 0.5f)
                .Select(c => c.GetComponent<NPCInteractable>())
                .Where(n => n != null && n == currentNPC)
                .ToArray();

            if (nearbyNPCs.Length == 0)
            {
                currentNPC = null;
            }
        }

        // 离开 Poo
        PooInteractable poo = other.GetComponent<PooInteractable>();
        if (poo != null && poo == currentPoo) currentPoo = null;
    }

    // 在 PlayerInteraction.cs 中添加
    public void ClearCurrentEvidence(Evidence evidence)
    {
        // 安全检查：只有当要清空的证据确实是自己时，才清空（防止手快走到下一个证据前把新的清掉了）
        if (currentEvidence == evidence)
        {
            currentEvidence = null;
            Debug.Log($"[PlayerInteraction] {evidence.name} 已被安全清空引用。");
        }
    }
}