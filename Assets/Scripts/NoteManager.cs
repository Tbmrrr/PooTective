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
    [Tooltip("在这里手动添加角色信息，只有解锁员工名单后才会显示")]
    public List<EvidenceData> characterFiles = new List<EvidenceData>();

    private List<EvidenceData> collectedEvidence = new List<EvidenceData>();
    private EvidenceData selectedData;

    // ✅ 新增：控制角色档案页签是否解锁
    private bool isStaffFilesUnlocked = false;

    // ✅ 记录"刚更新过、还未被玩家查看"的证物 ID
    private HashSet<string> pendingUpdateIDs = new HashSet<string>();

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

    // ✅ 新增：供 StaffList 脚本调用，解锁所有员工档案
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
            // ✅ 核心修改：每次打开时，强制重置为证物栏目
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
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);

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

        // ✅ 核心修改：如果是角色页签且未解锁，则不显示任何列表项
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

        if (pendingUpdateIDs.Contains(data.evidenceID))
        {
            if (detailDesc != null) detailDesc.text = "【线索更新】\n" + data.desc;
            pendingUpdateIDs.Remove(data.evidenceID);
        }
        else
        {
            if (detailDesc != null) detailDesc.text = data.desc;
        }

        if (activeNPC != null && currentTab == NoteTab.Evidence)
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(true);
        }
        else
        {
            if (presentSubmitBtn != null) presentSubmitBtn.gameObject.SetActive(false);
        }
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
}