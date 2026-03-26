using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TMP_Text detailDesc;

    [Header("搜索功能提示")]
    [Tooltip("当前选中证物支持搜索时显示的按键提示图片（按C进入搜索）")]
    public GameObject searchHintObject;  // 在 Inspector 拖入提示图片的 GameObject

    [Header("提交系统")]
    public Button presentSubmitBtn;
    private NPCInteractable activeNPC;

    [Header("角色档案数据")]
    [Tooltip("在这里手动添加角色信息，只有解锁员工名单后才会显示")]
    public List<EvidenceData> characterFiles = new List<EvidenceData>();

    private List<EvidenceData> collectedEvidence = new List<EvidenceData>();
    private EvidenceData selectedData;

    // 当前选中证物对应的 Evidence 组件（用于获取关键词）
    private Evidence selectedEvidenceComponent;

    // 控制角色档案页签是否解锁
    private bool isStaffFilesUnlocked = false;

    // 记录"刚更新过、还未被玩家查看"的证物 ID
    private HashSet<string> pendingUpdateIDs = new HashSet<string>();

    // 是否正处于搜索模式（防止 C 键和 N 键冲突）
    private bool isSearchModeActive = false;

    // ✅ 新增内部状态：记录已经完成过搜索的证物 ID，防止重复搜索
    private HashSet<string> completedSearchIDs = new HashSet<string>();

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
        if (searchHintObject != null) searchHintObject.SetActive(false);

        if (evidenceTabBtn != null) evidenceTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Evidence));
        if (characterTabBtn != null) characterTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Character));
        if (presentSubmitBtn != null) presentSubmitBtn.onClick.AddListener(OnSubmitToNPC);
    }

    void Update()
    {
        // 搜索模式下，C 键关闭搜索面板
        if (isSearchModeActive)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SearchPanelManager.Instance.CloseSearchPanel();
            }
            return; // 搜索模式下屏蔽其他按键
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

        // 背包打开时，C 键进入搜索模式
        if (notePanel.activeSelf && Input.GetKeyDown(KeyCode.C))
        {
            // ✅ 逻辑增强：只有未完成搜索的证物才能进入搜索模式
            if (selectedData.evidenceID != null && !completedSearchIDs.Contains(selectedData.evidenceID))
            {
                TryEnterSearchMode();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleNote();
        }
    }

    // 尝试进入搜索模式（当前选中证物需要有搜索关键词）
    private void TryEnterSearchMode()
    {
        if (selectedEvidenceComponent == null || !selectedEvidenceComponent.HasSearchFeature)
        {
            Debug.Log("当前证物不支持搜索功能。");
            return;
        }

        // ✅ 逻辑增强：如果 ID 已经在完成列表中，拦截进入
        if (completedSearchIDs.Contains(selectedData.evidenceID))
        {
            return;
        }

        if (SearchPanelManager.Instance == null)
        {
            Debug.LogWarning("场景中没有 SearchPanelManager！");
            return;
        }

        isSearchModeActive = true;

        // 隐藏背包面板（但不走 ToggleNote 完整流程，避免重置状态）
        if (notePanel != null) notePanel.SetActive(false);

        SearchPanelManager.Instance.OpenSearchPanel(selectedData, selectedEvidenceComponent.searchableKeywords);
    }

    // ✅ 新增：由 SearchPanelManager 在成功匹配关键词后调用
    public void MarkSearchAsCompleted(string evidenceID)
    {
        if (!completedSearchIDs.Contains(evidenceID))
        {
            completedSearchIDs.Add(evidenceID);
            // 立即关闭当前的搜索提示图
            if (searchHintObject != null) searchHintObject.SetActive(false);
        }
    }

    // 搜索面板关闭后回调（由 SearchPanelManager 调用）
    public void OnSearchPanelClosed(string evidenceID)
    {
        isSearchModeActive = false;

        // 重新打开背包面板，恢复到上次查看的证物
        if (notePanel != null) notePanel.SetActive(true);

        // 刷新列表和详情（描述可能已更新）
        RefreshList();

        // 找到对应的最新数据重新显示详情
        foreach (var data in collectedEvidence)
        {
            if (data.evidenceID == evidenceID)
            {
                ShowDetail(data);
                break;
            }
        }
    }

    public void UnlockStaffFiles()
    {
        isStaffFilesUnlocked = true;
        Debug.Log("所有员工档案已解锁！");
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
        if (searchHintObject != null) searchHintObject.SetActive(false);
        selectedEvidenceComponent = null;
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
            currentTab = NoteTab.Evidence;

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
            activeNPC = null;
            selectedEvidenceComponent = null;
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);
            if (searchHintObject != null) searchHintObject.SetActive(false);

            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive)
            {
                if (normal2DUI != null) normal2DUI.SetActive(false);
                if (RebuildModeManager.Instance.rebuildModePanel != null)
                {
                    RebuildModeManager.Instance.rebuildModePanel.SetActive(true);
                }
            }
            else
            {
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

        if (currentTab == NoteTab.Character && !isStaffFilesUnlocked)
        {
            Debug.Log("员工档案尚未解锁，列表不显示。");
            return;
        }

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

        // 线索更新提示逻辑
        if (pendingUpdateIDs.Contains(data.evidenceID))
        {
            if (detailDesc != null) detailDesc.text = "【线索更新】\n" + data.desc;
            pendingUpdateIDs.Remove(data.evidenceID);
        }
        else
        {
            if (detailDesc != null) detailDesc.text = data.desc;
        }

        // 查找场景中对应的 Evidence 组件，判断是否支持搜索
        selectedEvidenceComponent = FindEvidenceComponentByID(data.evidenceID);

        // ✅ 逻辑增强：只有支持搜索 且 尚未完成搜索 的证物才显示提示图片
        bool hasSearch = selectedEvidenceComponent != null
                        && selectedEvidenceComponent.HasSearchFeature
                        && !completedSearchIDs.Contains(data.evidenceID);

        if (searchHintObject != null) searchHintObject.SetActive(hasSearch);

        // 提交按钮显示逻辑
        if (activeNPC != null && currentTab == NoteTab.Evidence)
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(true);
        }
        else
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);
        }
    }

    // 通过 evidenceID 在场景中查找对应的 Evidence 组件
    private Evidence FindEvidenceComponentByID(string id)
    {
        Evidence[] allEvidence = FindObjectsOfType<Evidence>();
        foreach (var e in allEvidence)
        {
            if (e.evidenceID == id) return e;
        }
        return null;
    }

    private void OnSubmitToNPC()
    {
        if (activeNPC == null || string.IsNullOrEmpty(selectedData.evidenceID))
        {
            Debug.LogWarning("提交失败：未选中有效证物或当前没有交互的 NPC。");
            return;
        }

        string idToSubmit = selectedData.evidenceID;
        NPCInteractable npcToNotify = activeNPC;

        Debug.Log("正在向 " + npcToNotify.npcDisplayName + " 提交证物: " + idToSubmit);

        ToggleNote();

        npcToNotify.ReceiveEvidence(idToSubmit);
    }

    // 由 NPCInteractable 调用（NPC对话后更新描述）
    public void UpdateEvidenceInfo(string id, string newDesc)
    {
        for (int i = 0; i < collectedEvidence.Count; i++)
        {
            if (collectedEvidence[i].evidenceID == id)
            {
                EvidenceData updatedData = collectedEvidence[i];
                updatedData.desc = newDesc;
                collectedEvidence[i] = updatedData;
                pendingUpdateIDs.Add(id);
                Debug.Log("证物信息已更新: " + id);
                break;
            }
        }
    }

    // ✅ 新增：由 SearchPanelManager 调用，直接更新描述（不触发【线索更新】提示）
    public void UpdateEvidenceDescDirectly(string id, string newDesc)
    {
        for (int i = 0; i < collectedEvidence.Count; i++)
        {
            if (collectedEvidence[i].evidenceID == id)
            {
                EvidenceData updatedData = collectedEvidence[i];
                updatedData.desc = newDesc;
                collectedEvidence[i] = updatedData;
                Debug.Log("证物描述已通过搜索更新: " + id);
                break;
            }
        }
    }
}