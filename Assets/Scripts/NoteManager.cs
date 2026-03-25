using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    // 定义当前显示的页签类别
    public enum NoteTab { Evidence, Character }
    private NoteTab currentTab = NoteTab.Evidence;

    [Header("UI 面板")]
    public GameObject notePanel;
    public Transform listParent;        // 拖入 Viewport 下的 Content
    public GameObject listItemPrefab;
    public ScrollRect scrollRect;       // 建议拖入 ScrollRect 方便刷新位置

    [Header("分类按钮")]
    public Button evidenceTabBtn;       // 按钮1：切换到证物
    public Button characterTabBtn;      // 按钮2：切换到角色

    [Header("右侧详情显示")]
    public GameObject rightDetailGroup;
    public Image detailImage;
    public Text detailName;
    public Text detailDesc;

    [Header("角色档案数据")]
    [Tooltip("在这里手动添加角色信息，游戏开始就会显示")]
    public List<EvidenceData> characterFiles = new List<EvidenceData>();

    // 动态收集到的证物数据
    private List<EvidenceData> collectedEvidence = new List<EvidenceData>();

    [System.Serializable]
    public struct EvidenceData
    {
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
        // 初始状态下隐藏右侧详情
        if (rightDetailGroup != null) rightDetailGroup.SetActive(false);

        // 绑定页签按钮点击事件
        if (evidenceTabBtn != null) evidenceTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Evidence));
        if (characterTabBtn != null) characterTabBtn.onClick.AddListener(() => SwitchTab(NoteTab.Character));
    }

    void Update()
    {
        // 增加判断：对话过程中不允许打开笔记，防止 UI 冲突
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleNote();
        }
    }

    // 新增：切换页签的逻辑
    public void SwitchTab(NoteTab newTab)
    {
        currentTab = newTab;

        // 切换页签时，右侧详情默认隐藏，直到点击具体项
        if (rightDetailGroup != null) rightDetailGroup.SetActive(false);

        RefreshList();

        // 重置滚动条位置到顶部
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void AddEvidence(Evidence evidence)
    {
        EvidenceData data = new EvidenceData
        {
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
            // 每次打开笔记，默认显示当前页签
            SwitchTab(currentTab);

            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void RefreshList()
    {
        // 清理旧列表
        foreach (Transform child in listParent) Destroy(child.gameObject);

        // --- 核心逻辑：根据当前页签选择数据源 ---
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

            // 设置文本和图标（保持原有逻辑不变）
            Text itemText = item.GetComponentInChildren<Text>();
            if (itemText != null) itemText.text = data.name;

            Transform iconTrans = item.transform.Find("Icon");
            if (iconTrans != null) iconTrans.GetComponent<Image>().sprite = data.icon;
        }
    }

    public void ShowDetail(EvidenceData data)
    {
        // 点击后才显示右侧面板
        if (rightDetailGroup != null) rightDetailGroup.SetActive(true);

        if (detailImage != null) detailImage.sprite = data.fullImage;
        if (detailName != null) detailName.text = data.name;
        if (detailDesc != null) detailDesc.text = data.desc;

        Debug.Log("正在显示详情: " + data.name);
    }
}