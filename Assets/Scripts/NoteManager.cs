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

    // ✅ 新增：左侧背景图引用及其对应的图片资源
    public Image leftBackgroundImage;
    public Sprite evidenceTabBG;   // 证物页签时的背景图
    public Sprite characterTabBG;  // 角色档案页签时的背景图

    [Header("常规 UI (打开笔记本时需隐藏)")]
    public GameObject normal2DUI;

    [Header("分类按钮")]
    public Button evidenceTabBtn;
    public Button characterTabBtn;
    public Button closeNoteBtn; // 👈 【新增】关闭笔记本按钮

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

    [Header("取出功能 (新增)")]
    public Button takeOutBtn; // 右侧详情面板里的“取出”按钮
    private string currentTakenOutID = null; // 当前被取出的证物/角色ID

    private List<EvidenceData> collectedEvidence = new List<EvidenceData>();
    private EvidenceData selectedData;

    // 当前选中证物对应的 Evidence 组件（用于获取关键词）
    private Evidence selectedEvidenceComponent;

    // 控制角色档案页签是否解锁
    private bool isStaffFilesUnlocked = false;

    // 记录"刚更新过、还未被玩家查看"的证物 ID
    private HashSet<string> pendingUpdateIDs = new HashSet<string>();

    // 是否正处于搜索模式（防止 C 键 and N 键冲突）
    private bool isSearchModeActive = false;

    // ✅ 记录已经完成过搜索的证物 ID，防止重复搜索
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

        // 绑定功能：取出按钮
        if (takeOutBtn != null) takeOutBtn.onClick.AddListener(OnTakeOutClicked);

        // 👈 【新增】绑定关闭功能
        // 因为按钮只有在笔记本打开时才能点击，此时调用 ToggleNote() 会自动走 else 分支，完美执行所有关闭逻辑。
        if (closeNoteBtn != null) closeNoteBtn.onClick.AddListener(ToggleNote);
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
            // 只有在证物栏、且未完成搜索的证物才能进入搜索模式
            if (currentTab == NoteTab.Evidence && selectedData.evidenceID != null && !completedSearchIDs.Contains(selectedData.evidenceID))
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

    // --- 取出功能逻辑 (已对齐员工和证物) ---
    public void OnTakeOutClicked()
    {
        // 获取当前选中的数据 ID
        string id = selectedData.evidenceID;

        // 1. 获取 UI 脚本（先看单例，没有就去场景里现抓）
        TakenOutEvidenceUI targetUI = TakenOutEvidenceUI.Instance;
        if (targetUI == null)
        {
            targetUI = FindObjectOfType<TakenOutEvidenceUI>(true);
        }

        Debug.Log($"<color=cyan>[执行检查]</color> ID: {id}, UI 目标是否存在: {targetUI != null}");

        // 2. 只有 ID 存在且找到了 UI 脚本，才执行
        if (!string.IsNullOrEmpty(id) && targetUI != null)
        {
            currentTakenOutID = id;

            // ✅ 关键：如果物体是隐藏的，必须先显示它，否则协程和代码都不跑
            targetUI.gameObject.SetActive(true);

            targetUI.TakeOut(selectedData);
            RefreshTakeOutButtonState();

            Debug.Log("<color=green>[取出动作已发出]</color>");
        }
        else
        {
            Debug.LogError("无法取出：请检查场景中是否有 TakenOutEvidenceUI 物体，或者它是否被意外 Destroy 了。");
        }
    }

    public void OnTakenOutItemClosed()
    {
        currentTakenOutID = null;
        RefreshTakeOutButtonState();
    }

    // --- 完善按钮显示逻辑 (员工档案现在也能取出) ---
    private void RefreshTakeOutButtonState()
    {
        if (takeOutBtn == null) return;

        // 严谨的显示判定：
        // 1. 必须处于【重建模式】(RebuildModeManager.Instance.isRebuildModeActive)
        // 2. 必须【已经选中】了一个有效的 ID (无论是证物还是员工)
        // 3. 当前屏幕上【没有】正在悬浮的物品

        bool isRebuildMode = (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive);

        bool canShow = isRebuildMode &&
                       (!string.IsNullOrEmpty(selectedData.evidenceID)) &&
                       string.IsNullOrEmpty(currentTakenOutID);

        takeOutBtn.gameObject.SetActive(canShow);
    }

    // 尝试进入搜索模式
    private void TryEnterSearchMode()
    {
        if (selectedEvidenceComponent == null || !selectedEvidenceComponent.HasSearchFeature)
        {
            Debug.Log("当前证物不支持搜索功能。");
            return;
        }

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

        // 隐藏背包面板
        if (notePanel != null) notePanel.SetActive(false);

        SearchPanelManager.Instance.OpenSearchPanel(selectedData, selectedEvidenceComponent.searchableKeywords);
    }

    // 由 SearchPanelManager 在成功匹配关键词后调用
    public void MarkSearchAsCompleted(string evidenceID)
    {
        if (!completedSearchIDs.Contains(evidenceID))
        {
            completedSearchIDs.Add(evidenceID);
            if (searchHintObject != null) searchHintObject.SetActive(false);
        }
    }

    // 搜索面板关闭后回调
    public void OnSearchPanelClosed(string evidenceID)
    {
        isSearchModeActive = false;

        if (notePanel != null) notePanel.SetActive(true);

        RefreshList();

        // 找到最新数据重新显示详情
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

        // ✅ 新增：根据页签切换左侧底图
        if (leftBackgroundImage != null)
        {
            if (currentTab == NoteTab.Evidence)
            {
                if (evidenceTabBG != null) leftBackgroundImage.sprite = evidenceTabBG;
            }
            else
            {
                if (characterTabBG != null) leftBackgroundImage.sprite = characterTabBG;
            }
        }

        if (rightDetailGroup != null) rightDetailGroup.SetActive(false);
        if (searchHintObject != null) searchHintObject.SetActive(false);

        selectedEvidenceComponent = null;
        selectedData = default;

        RefreshList();
        RefreshTakeOutButtonState();

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void AddEvidence(Evidence evidence)
    {
        // 如果证据脚本里有 searchableKeywords，
        // 说明它可能有“搜索后”的描述。
        // 但进包这一刻，我们希望它是“干净”的。

        EvidenceData data = new EvidenceData
        {
            evidenceID = evidence.evidenceID,
            name = evidence.evidenceName,
            icon = evidence.evidenceIcon,
            fullImage = evidence.evidenceFullImage,

            // ✅ 核心修复：这里不再直接拿 evidence.description
            // 而是检查一下，如果 description 已经被改得和初始不一样了（或者存在 keyword 逻辑），
            // 我们需要确保这里填入的是“初始简短描述”。
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
            // 每次打开笔记本，先清空上一次选中的数据
            selectedData = default;
            selectedEvidenceComponent = null;

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
            // ✅ 新增：如果是从提交证物模式退出（没有提交），通知 NPC 恢复按E提示
            NPCInteractable npcToNotify = activeNPC;
            if (npcToNotify != null)
            {
                Debug.Log($"准备调用 OnPresentModeCancelled，npcToNotify={npcToNotify.name}");
                npcToNotify.OnPresentModeCancelled();
            }
            else
            {
                Debug.Log("npcToNotify 是 null，没有调用 OnPresentModeCancelled");
            }
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

                if (!string.IsNullOrEmpty(currentTakenOutID) && TakenOutEvidenceUI.Instance != null)
                {
                    TakenOutEvidenceUI.Instance.gameObject.SetActive(true);
                }
            }
            else
            {
                if (normal2DUI != null) normal2DUI.SetActive(true);
                if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.rebuildModePanel != null)
                {
                    RebuildModeManager.Instance.rebuildModePanel.SetActive(false);
                }

                if (TakenOutEvidenceUI.Instance != null)
                {
                    TakenOutEvidenceUI.Instance.gameObject.SetActive(false);
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
        // 1. 探路 Log
        Debug.Log($"<color=cyan>--- RefreshList 开始执行！当前页面是: {currentTab} ---</color>");

        // 🚨 【安全检查 1】防止 listParent 为空导致后续代码全部崩溃
        if (listParent == null)
        {
            Debug.LogError("<color=red>【❗ 致命错误】你的 NoteManager 脚本上 [List Parent] 槽位是空的！请把 UI 里的 Content 物体拖进去！</color>");
            return; // 拦截，不让程序崩溃
        }

        foreach (Transform child in listParent) Destroy(child.gameObject);

        if (currentTab == NoteTab.Character && !isStaffFilesUnlocked)
        {
            Debug.Log("<color=yellow>拦截：员工档案尚未解锁，列表不显示。</color>");
            return;
        }

        List<EvidenceData> targetData = (currentTab == NoteTab.Evidence) ? collectedEvidence : characterFiles;

        // 2. 数量 Log（确保放在循环外面）
        Debug.Log($"<color=cyan>--- 当前准备生成的列表数量为: {targetData.Count} ---</color>");

        // 🚨 【安全检查 2】如果数据是 0，直接打道回府
        if (targetData.Count == 0)
        {
            Debug.LogWarning("<color=yellow>【⚠️ 提示】当前背包里的数据量为 0！如果你还没在游戏里捡起过任何证物，列表自然是空的，请先去捡个证物再看。</color>");
            return;
        }

        // 3. 开始渲染图标
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
                    ShowDetail(capturedData);
                });
            }

            // 寻找图片组件
            Image itemImage = null;
            Transform iconTrans = item.transform.Find("Icon");
            if (iconTrans != null)
            {
                itemImage = iconTrans.GetComponent<Image>();
            }
            else
            {
                itemImage = item.GetComponent<Image>();
            }

            // 图片赋值
            if (itemImage != null)
            {
                if (data.icon != null)
                {
                    itemImage.sprite = data.icon;
                    itemImage.preserveAspect = true;
                    // 💡 【核心新增】加上这句绿字，亲眼见证循环跑没跑！
                    Debug.Log($"<color=green>【🎉 证据确凿】循环成功执行！已为 [{data.name}] 实例化了 UI，并把图片塞进去了！</color>");
                }
                else
                {
                    Debug.LogError($"<color=red>【❌ 错误】证物 [{data.name}] 存在，但它的 Icon 图片在预制体/数据里是空的！</color>");
                }
            }
            else
            {
                Debug.LogError("<color=red>【❌ 错误】实例化的 Prefab 上既没有叫 'Icon' 的子物体，自身也没有 Image 组件！</color>");
            }
        }
    }

    public void ShowDetail(EvidenceData data)
    {
        if (string.IsNullOrEmpty(data.evidenceID)) return;

        selectedData = data;
        if (rightDetailGroup != null) rightDetailGroup.SetActive(true);

        if (detailImage != null) detailImage.sprite = data.fullImage;
        if (detailName != null) detailName.text = data.name;

        // --- 修改开始 ---

        // 1. 先检查是否有待更新的标记（用于显示“【线索更新】”红字）
        if (pendingUpdateIDs.Contains(data.evidenceID))
        {
            if (detailDesc != null) detailDesc.text = "<color=#FF4500>【线索更新】</color>\n" + data.desc;
            pendingUpdateIDs.Remove(data.evidenceID); // 玩家看了，移除红字标记
        }
        else
        {
            // 2. 直接使用 data.desc (这是在 UpdateEvidenceInfo 中被更新过的值)
            if (detailDesc != null) detailDesc.text = data.desc;
        }

        // 3. 搜索功能依然需要组件支持，所以保留组件寻找，但仅用于搜索逻辑
        selectedEvidenceComponent = FindEvidenceComponentByID(data.evidenceID);

        // --- 修改结束 ---

        // 搜索提示图逻辑 (保持不变)
        bool hasSearch = (currentTab == NoteTab.Evidence)
                        && selectedEvidenceComponent != null
                        && selectedEvidenceComponent.HasSearchFeature
                        && !completedSearchIDs.Contains(data.evidenceID);

        if (searchHintObject != null) searchHintObject.SetActive(hasSearch);

        // 按钮显示逻辑 (保持不变)
        if (activeNPC != null && currentTab == NoteTab.Evidence)
            presentSubmitBtn?.gameObject.SetActive(true);
        else
            presentSubmitBtn?.gameObject.SetActive(false);

        RefreshTakeOutButtonState();
    }

    private Evidence FindEvidenceComponentByID(string id)
    {
        // ✅ 核心修复：增加 true 参数，允许在隐藏（Inactive）物体中查找
        Evidence[] allEvidence = GameObject.FindObjectsByType<Evidence>(FindObjectsInactive.Include, FindObjectsSortMode.None);

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
            return;
        }

        string idToSubmit = selectedData.evidenceID;
        NPCInteractable npcToNotify = activeNPC;

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
                break;
            }
        }
    }

    public void UpdateEvidenceDescDirectly(string id, string newDesc)
    {
        for (int i = 0; i < collectedEvidence.Count; i++)
        {
            if (collectedEvidence[i].evidenceID == id)
            {
                EvidenceData updatedData = collectedEvidence[i];
                updatedData.desc = newDesc;
                collectedEvidence[i] = updatedData;
                break;
            }
        }
    }

    // ✅ 新增：更新角色档案的方法
    public void UpdateCharacterInfo(string charID, string newDesc)
    {
        bool found = false;
        for (int i = 0; i < characterFiles.Count; i++)
        {
            if (characterFiles[i].evidenceID == charID)
            {
                EvidenceData updatedData = characterFiles[i];
                updatedData.desc = newDesc;
                characterFiles[i] = updatedData;

                // 同样加入红字提示标记
                pendingUpdateIDs.Add(charID);
                found = true;
                Debug.Log($"<color=orange>[档案更新]</color> 角色 {charID} 的描述已更新。");
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"未找到 ID 为 {charID} 的角色档案，无法更新描述。");
        }
    }

    // 在 NoteManager.cs 中添加
    public bool HasEvidence(string id)
    {
        // 检查已收集的证物列表中是否已有该 ID
        return collectedEvidence.Exists(e => e.evidenceID == id);
    }

    // ✅ 新增：提供给外部获取 Evidence 脚本实例的方法
    public Evidence GetEvidenceComponent(string id)
    {
        // 调用你现有的私有方法即可，因为它内部使用了 FindObjectsOfType<Evidence>(true)
        // 这里的关键是 FindObjectsOfType 必须包含隐藏物体
        Evidence[] allEvidence = Resources.FindObjectsOfTypeAll<Evidence>();
        foreach (var e in allEvidence)
        {
            // 排除掉预制体，只找场景中的物体
            if (e.gameObject.scene.name != null && e.evidenceID == id) return e;
        }
        return null;
    }

    // ✅ 在 NoteManager.cs 里的末尾添加这个方法
    public bool IsSearchCompleted(string id)
    {
        // 如果这个 ID 在已完成搜索的 Hashset 里，就返回 true
        return completedSearchIDs.Contains(id);
    }
}