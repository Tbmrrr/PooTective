using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public KeyCode interactKey = KeyCode.E;

    // 内部记录当前靠近的那个NPC
    private NPCInteractable currentNPC;

    void Update()
    {
        // 核心逻辑：只有在【对话管理器】没开启对话时，才允许按E开始对话
        if (DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive)
        {
            if (currentNPC != null && Input.GetKeyDown(interactKey))
            {
                // 触发NPC的交互函数
                currentNPC.OnInteract();
            }
        }

        // 注意：对话开启后的 E 键逻辑（翻页/跳过）由 DialogueManager 独立处理
    }

    // --- 触发器检测 ---

    private void OnTriggerEnter(Collider other)
    {
        // 获取碰撞体身上的 NPCInteractable 组件
        NPCInteractable npc = other.GetComponent<NPCInteractable>();

        if (npc != null)
        {
            currentNPC = npc;
            // 修正后的变量名：npcDisplayName
            Debug.Log("靠近了 NPC: " + npc.npcDisplayName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NPCInteractable npc = other.GetComponent<NPCInteractable>();

        if (npc != null && npc == currentNPC)
        {
            currentNPC = null;
            Debug.Log("离开了 NPC");
        }
    }
}