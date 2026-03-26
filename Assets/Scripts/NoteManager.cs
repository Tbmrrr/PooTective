using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    public enum NoteTab { Evidence, Character }
    private NoteTab currentTab = NoteTab.Evidence;

    [Header("UI 面板")]
    public GameObject notePanel;
    public Transform listParent;
    public GameObject listItemPrefab;
    public ScrollRect scrollRect;

    [Header("常规 UI (打开笔记本时需隐藏)")]
    public GameObject normal2DUI;

    [Header("分类按钮")]
    public Button evidenceTabBtn;
    public Button characterTabBtn;

    [Header("右侧详情显示")]
    public GameObject rightDetailGroup;
    public Image detailImage;
    public Text detailName;
    public Text detailDesc;

    [Header("提交系统")]
    public Button presentSubmitBtn;
    private NPCInteractable activeNPC;

    [Header("角色档案数据")]
    [Tooltip("在这里手动添加角色信息，游戏开始就会显示")]
    public List<EvidenceData> characterFiles = new List<EvidenceData>();

    private List<EvidenceData> collectedEvidence = new List<EvidenceData>();
    private EvidenceData selectedData;

    [System.Serializable]
    public struct EvidenceData
    {
        public string evidenceID;
        public string name;
        public Sprite icon;
        public Sprite fullImage;
        [TextArea(3, 10)]
        public string desc;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (notePanel != null) notePanel.SetActive(false);
        if (rightDetailGroup != null) rightDetailGroup.SetActive(false);
        if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);

        if (evidenceTabBtn != null) evidenceTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Evidence));
        if (characterTabBtn != null) characterTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Character));
        if (presentSubmitBtn != null) presentSubmitBtn.onClick.AddListener(OnSubmitToNPC);
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleNote();
        }
    }

    public void EnterPresentMode(NPCInteractable npc)
    {
        activeNPC = npc;
        currentTab = NoteTab.Evidence;

        if (!notePanel.activeSelf)
        {
            ToggleNote();
        }
    }

    public void SwitchTab(NoteTab newTab)
    {
        currentTab = newTab;
        if (rightDetailGroup != null) rightDetailGroup.SetActive(false);
        RefreshList();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void AddEvidence(Evidence evidence)
    {
        EvidenceData data = new EvidenceData
        {
            evidenceID = evidence.evidenceID,
            name = evidence.evidenceName,
            icon = evidence.evidenceIcon,
            fullImage = evidence.evidenceFullImage,
            desc = evidence.description
        };
        collectedEvidence.Add(data);
    }

    void ToggleNote()
    {
        bool isActive = !notePanel.activeSelf;
        notePanel.SetActive(isActive);

        if (isActive)
        {
            // 打开笔记本时：所有底层 UI 全部关闭
            if (normal2DUI != null) normal2DUI.SetActive(false);
            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.rebuildModePanel != null)
            {
                RebuildModeManager.Instance.rebuildModePanel.SetActive(false);
            }

            SwitchTab(currentTab);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 关闭笔记本时：判断当前到底在哪个模式
            activeNPC = null;
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);

            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive)
            {
                // 如果在重建模式：只开重建模式 UI，确保正常 UI 保持关闭
                if (normal2DUI != null) normal2DUI.SetActive(false);
                if (RebuildModeManager.Instance.rebuildModePanel != null)
                {
                    RebuildModeManager.Instance.rebuildModePanel.SetActive(true);
                }
            }
            else
            {
                // 如果在普通模式：开启正常 UI，关闭重建模式 UI
                if (normal2DUI != null) normal2DUI.SetActive(true);
                if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.rebuildModePanel != null)
                {
                    RebuildModeManager.Instance.rebuildModePanel.SetActive(false);
                }

                if (RebuildModeManager.Instance != null)
                {
                    RebuildModeManager.Instance.RefreshAbilityIcon();
                }
            }
        }
    }

    void RefreshList()
    {
        foreach (Transform child in listParent) Destroy(child.gameObject);

        List<EvidenceData> targetData = (currentTab == NoteTab.Evidence) ? collectedEvidence : characterFiles;

        foreach (var data in targetData)
        {
            GameObject item = Instantiate(listItemPrefab, listParent);
            Button btn = item.GetComponent<Button>();
            if (btn == null) btn = item.GetComponentInChildren<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                EvidenceData capturedData = data;

                btn.onClick.AddListener(() => {
                    Debug.Log("点击了项目按钮: " + capturedData.name);
                    ShowDetail(capturedData);
                });
            }

            Text itemText = item.GetComponentInChildren<Text>();
            if (itemText != null) itemText.text = data.name;

            Transform iconTrans = item.transform.Find("Icon");
            if (iconTrans != null) iconTrans.GetComponent<Image>().sprite = data.icon;
        }
    }

    public void ShowDetail(EvidenceData data)
    {
        if (string.IsNullOrEmpty(data.evidenceID)) return;

        selectedData = data;
        if (rightDetailGroup != null) rightDetailGroup.SetActive(true);

        if (detailImage != null) detailImage.sprite = data.fullImage;
        if (detailName != null) detailName.text = data.name;
        if (detailDesc != null) detailDesc.text = data.desc;

        if (activeNPC != null && currentTab == NoteTab.Evidence)
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(true);
        }
        else
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);
        }
    }

    // ✅ 修复：在调用 ToggleNote() 之前，先将 activeNPC 缓存到局部变量
    private void OnSubmitToNPC()
    {
        if (activeNPC == null || string.IsNullOrEmpty(selectedData.evidenceID))
        {
            Debug.LogWarning("提交失败：未选中有效证物或当前没有交互的 NPC。");
            return;
        }

        string idToSubmit = selectedData.evidenceID;
        NPCInteractable npcToNotify = activeNPC; // ✅ 关键修复：关闭背包前先缓存引用

        Debug.Log("正在向 " + npcToNotify.npcDisplayName + " 提交证物: " + idToSubmit);

        ToggleNote(); // 此处会执行 activeNPC = null，但我们已经安全保存了引用

        npcToNotify.ReceiveEvidence(idToSubmit); // ✅ 使用缓存的引用，不会报错
    }

    public void UpdateEvidenceInfo(string id, string newDesc)
    {
        for (int i = 0; i < collectedEvidence.Count; i++)
        {
            if (collectedEvidence[i].evidenceID == id)
            {
                EvidenceData updatedData = collectedEvidence[i];
                updatedData.desc = "【线索更新】\n" + newDesc;
                collectedEvidence[i] = updatedData;
                Debug.Log("证物信息已更新: " + id);
                break;
            }
        }
    }
}