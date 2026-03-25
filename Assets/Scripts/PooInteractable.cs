using UnityEngine;

public class PooInteractable : MonoBehaviour
{
    [Header("UI 提示 (World Space)")]
    public GameObject rPromptObject; // 拖入那个写着“按R进入模式”的图片物体

    void Start()
    {
        // 初始隐藏提示
        if (rPromptObject != null) rPromptObject.SetActive(false);
    }

    // 当玩家进入触发区
    private void OnTriggerEnter(Collider other)
    {
        // 只有在专注模式下，才显示“按R”的提示
        if (other.CompareTag("Player") && FocusModeManager.Instance.isFocusModeActive)
        {
            if (rPromptObject != null) rPromptObject.SetActive(true);
        }
    }

    // 当玩家离开触发区
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (rPromptObject != null) rPromptObject.SetActive(false);
        }
    }

    // 被 PlayerInteraction 调用
    public void OnPooInteract()
    {
        // 只有在还没解锁过的时候，才通过 Poo 触发
        if (RebuildModeManager.Instance != null && !RebuildModeManager.Instance.hasUnlockedRebuild)
        {
            Debug.Log("第一次发现线索，解锁重建模式！");
            RebuildModeManager.Instance.UnlockAndEnter();

            // 交互后隐藏 Poo 的 R 键提示
            if (rPromptObject != null) rPromptObject.SetActive(false);
        }
    }

    // 提供一个方法给 FocusModeManager，防止关闭模式后提示还亮着
    private void OnDisable()
    {
        if (rPromptObject != null) rPromptObject.SetActive(false);
    }
}