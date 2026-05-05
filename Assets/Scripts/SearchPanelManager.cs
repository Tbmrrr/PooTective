using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class SearchPanelManager : MonoBehaviour
{
    public static SearchPanelManager Instance { get; private set; }

    [Header("UI 根节点")]
    public GameObject searchPanel;

    [Header("A区：线索列表")]
    public Transform clueListParent;
    public GameObject clueItemPrefab;

    [Header("B区：搜索输入")]
    public TMP_InputField searchInputField;
    public TMP_Text searchResultDisplay;
    public ScrollRect resultScrollRect;

    [Header("连线设置")]
    public RectTransform lineContainer;
    public Image linePrefab;
    public Color highlightColor = Color.yellow;
    public Color successColor = Color.green;

    [Header("交互控制")]
    [Tooltip("在搜索面板打开时需要禁用的脚本列表")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    private class ClueState
    {
        public string originalText;
        public string targetLinkID;
        public string resultText;
        public bool isSolved;
        public GameObject clueUI;
    }

    // 状态存储：证物ID -> 线索列表
    private Dictionary<string, List<ClueState>> allEvidenceStates = new Dictionary<string, List<ClueState>>();

    private List<ClueState> currentClueStates = new List<ClueState>();
    private string currentEvidenceID;

    private Image activeLine;
    private int selectedClueIndex = -1;
    private Vector2 lineFixedStartPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (searchPanel != null) searchPanel.SetActive(false);
        searchInputField.onEndEdit.AddListener(OnSearchSubmit);
    }

    void Update()
    {
        if (!searchPanel.activeSelf) return;

        // ✅ 修复3：线段跟随鼠标 - 每帧更新，不在HandleLineDrawing中更新
        if (selectedClueIndex != -1 && activeLine != null)
        {
            UpdateLine(activeLine.rectTransform, lineFixedStartPos, Input.mousePosition);
        }

        HandleLineDrawing();

        if (Input.GetMouseButtonDown(1)) CancelSelection();
    }

    public void OpenSearchPanel(NoteManager.EvidenceData evidenceData, List<SearchableKeyword> keywords)
    {
        ToggleExternalScripts(false);
        currentEvidenceID = evidenceData.evidenceID;

        // 状态持久化判断
        if (!allEvidenceStates.ContainsKey(currentEvidenceID))
        {
            InitializeClues(evidenceData.desc, keywords);
        }
        else
        {
            currentClueStates = allEvidenceStates[currentEvidenceID];
        }

        RefreshClueUI();

        // ✅ 修复2：每次打开都清空搜索结果
        searchResultDisplay.text = "";
        searchInputField.text = "";

        searchPanel.SetActive(true);
        searchInputField.ActivateInputField();
    }

    private void InitializeClues(string fullDesc, List<SearchableKeyword> keywords)
    {
        List<ClueState> newStates = new List<ClueState>();
        string[] lines = fullDesc.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var state = new ClueState { originalText = lines[i], isSolved = false };
            if (i < keywords.Count)
            {
                state.targetLinkID = keywords[i].keyword;
                state.resultText = keywords[i].newDescriptionOnSearch;
            }
            newStates.Add(state);
        }

        allEvidenceStates[currentEvidenceID] = newStates;
        currentClueStates = newStates;
    }

    private void RefreshClueUI()
    {
        foreach (Transform child in clueListParent) Destroy(child.gameObject);

        for (int i = 0; i < currentClueStates.Count; i++)
        {
            int index = i;
            GameObject item = Instantiate(clueItemPrefab, clueListParent);
            currentClueStates[i].clueUI = item;

            TMP_Text txt = item.GetComponentInChildren<TMP_Text>();

            if (currentClueStates[i].isSolved)
            {
                txt.text = currentClueStates[i].resultText;
                txt.color = successColor;
            }
            else
            {
                txt.text = currentClueStates[i].originalText;
                txt.color = Color.white;
            }

            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() => OnClueClicked(index));
        }
    }

    private void OnClueClicked(int index)
    {
        if (currentClueStates[index].isSolved) return;

        if (selectedClueIndex != -1)
        {
            CancelSelection();
        }

        selectedClueIndex = index;
        lineFixedStartPos = Input.mousePosition;

        foreach (var state in currentClueStates)
        {
            if (!state.isSolved && state.clueUI != null)
                state.clueUI.GetComponentInChildren<TMP_Text>().color = Color.white;
        }

        currentClueStates[index].clueUI.GetComponentInChildren<TMP_Text>().color = highlightColor;

        activeLine = Instantiate(linePrefab, lineContainer);
        activeLine.gameObject.SetActive(true);
        activeLine.color = highlightColor;
    }

    private void HandleLineDrawing()
    {
        if (selectedClueIndex == -1 || activeLine == null) return;

        // ✅ 线段更新移到Update中每帧执行

        if (Input.GetMouseButtonDown(0))
        {
            string linkID = GetResultLinkAtMouse();
            if (!string.IsNullOrEmpty(linkID))
            {
                CheckMatch(linkID);
            }
        }
    }

    private void CheckMatch(string linkID)
    {
        var currentClue = currentClueStates[selectedClueIndex];
        if (linkID == currentClue.targetLinkID)
        {
            Image lineToFade = activeLine;
            int indexToSolve = selectedClueIndex;

            activeLine = null;
            selectedClueIndex = -1;

            StartCoroutine(SuccessEffect(lineToFade, indexToSolve));
        }
        else
        {
            CancelSelection();
        }
    }

    private System.Collections.IEnumerator SuccessEffect(Image line, int index)
    {
        Debug.Log($"=== SuccessEffect Started === Line: {(line != null ? line.gameObject.name : "NULL")}, Index: {index}");
        Debug.Log($"Time.timeScale: {Time.timeScale}, GameObject active: {gameObject.activeInHierarchy}");

        if (line == null)
        {
            Debug.LogWarning("Line is null in SuccessEffect - ABORT");
            yield break;
        }

        // 立即标记为已解决并更新UI文本
        currentClueStates[index].isSolved = true;
        Debug.Log($"Marked clue {index} as solved");

        if (currentClueStates[index].clueUI != null)
        {
            TMP_Text txt = currentClueStates[index].clueUI.GetComponentInChildren<TMP_Text>();
            txt.text = currentClueStates[index].resultText;
            txt.color = successColor;
            Debug.Log($"Updated UI text to: {currentClueStates[index].resultText}");
        }

        // ✅ 线段变绿
        line.color = successColor;
        Debug.Log($"Line color changed to green");

        // ✅ 使用 WaitForSecondsRealtime 代替 WaitForSeconds
        Debug.Log($"Waiting 0.5 seconds before cleanup... (using realtime)");
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(0.5f);
        float endTime = Time.realtimeSinceStartup;
        Debug.Log($"Wait completed! Actual time: {endTime - startTime}s");

        // ✅ 删除 lineContainer 的所有子物体
        Debug.Log($"=== Starting Line Cleanup ===");
        Debug.Log($"LineContainer child count BEFORE: {lineContainer.childCount}");

        int destroyedCount = 0;
        foreach (Transform child in lineContainer)
        {
            Debug.Log($"Destroying child: {child.gameObject.name}");
            Destroy(child.gameObject);
            destroyedCount++;
        }

        Debug.Log($"Destroyed {destroyedCount} line objects");
        Debug.Log($"LineContainer child count AFTER destroy call: {lineContainer.childCount}");

        // 等待一帧后再检查
        yield return null;
        Debug.Log($"LineContainer child count AFTER one frame: {lineContainer.childCount}");

        Debug.Log($"=== SuccessEffect Completed ===");
        CheckAllComplete();
    }

    private void CheckAllComplete()
    {
        Debug.Log($"CheckAllComplete called. All solved? {currentClueStates.All(s => s.isSolved)}");

        if (currentClueStates.All(s => s.isSolved))
        {
            Debug.Log("=== All clues solved! Updating evidence description ===");

            string finalDesc = string.Join("\n", currentClueStates.Select(s => s.resultText));
            Debug.Log($"Final description: {finalDesc}");

            // ✅ 1. 更新 NoteManager 中存储的数据
            NoteManager.Instance.UpdateEvidenceDescDirectly(currentEvidenceID, finalDesc);
            Debug.Log($"Called UpdateEvidenceDescDirectly for {currentEvidenceID}");

            // ✅ 2. 更新 Evidence 组件的 description
            Evidence ev = NoteManager.Instance.GetEvidenceComponent(currentEvidenceID);
            if (ev != null)
            {
                ev.description = finalDesc;
                ev.SetUpdateFlag(); // 这个会在下次打开时显示红字提示
                Debug.Log($"Updated Evidence component description");
            }
            else
            {
                Debug.LogWarning($"Evidence component not found for {currentEvidenceID}");
            }

            // ✅ 3. 标记搜索完成
            NoteManager.Instance.MarkSearchAsCompleted(currentEvidenceID);
            Debug.Log($"Marked search as completed for {currentEvidenceID}");

            // ✅ 4. 【关键新增】立即通知 NoteManager 添加"线索更新"红字标记
            // 这样玩家关闭搜索面板回到背包时，会看到【线索更新】提示
            NoteManager.Instance.UpdateEvidenceInfo(currentEvidenceID, finalDesc);
            Debug.Log($"Added pending update flag for {currentEvidenceID}");
        }
    }

    private void OnSearchSubmit(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        string result = "未找到相关结果。";
        string lowerInput = input.ToLower(); // 转换为小写方便匹配

        // 1. 长颈鹿 / 植食 / 排泄
        if (lowerInput.Contains("长颈鹿") || lowerInput.Contains("植食") || lowerInput.Contains("排泄"))
        {
            result = "<b>长颈鹿 植食性动物</b>\n" + "<b>【饮食习惯】</b>\n" +
                     "每天需摄入大量高纤维、低营养的植物性食物，以合欢树叶为主。单日进食量可达30至60公斤以满足能量需求。\n" +
                     "此外，水果、胡萝卜、南瓜等高糖分蔬果是深受其欢迎的零食，通常作为正餐之外的补充。\n\n" +
                     "<b>【排泄情况】</b>\n" +
                     "<b>1. 排便</b>\n" +
                     "性状：健康个体排出大量颗粒状、<link=\"Link_Fiber\">较为干燥</link>的<link=\"Link_Carrot\">深褐色</link>固体粪球。\n" +
                     "成分：粪便中几乎全是未消化的植物纤维，散发<link=\"Link_Sugar\">草料发酵气味</link>，无明显臭味。\n" +
                     "频量：<link=\"Link_Starvation\">日排泄量可达15公斤左右</link>，排便次数约10至15次。控制能力较弱。\n\n" +
                     "<b>2. 排尿</b>\n" +
                     "每天约3至5次，单次尿量很大。长颈鹿对排尿具有较好的主动控制能力，会刻意避开睡觉区域。";
        }
        // 2. 乌鸦 / 杂食
        else if (lowerInput.Contains("乌鸦") || lowerInput.Contains("杂食"))
        {
            result = "<b>乌鸦 杂食性动物</b>\n" + "<b>【饮食习惯】</b>\n" +
                     "以烹煮后的谷物（如米饭、面包）、多种果实（浆果、苹果等）为主，也摄取昆虫制品（如炸蝗虫、蚕蛹）和烹煮过的蛋类。食性杂乱，是动物城中典型的杂食居民。\n\n" +
                     "<b>【排泄情况】</b>\n" +
                     "性状：粪便与尿液混合排出，呈液态或半液态，常混有<link=\"Link_UricAcid\">白色尿酸沉淀</link>。气味淡薄，略带酸腐。\n" +
                     "成分：代谢废物主要以尿酸形式快速排出，以减轻飞行负荷。\n" +
                     "频量：单次排泄量虽小（每次约数毫升），但全天排泄频繁，每日可达20次以上，日排量少于30克。由于飞行需要减轻体重，对排泄的控制能力较弱，难以主动憋住。";
        }
        // 3. 胡萝卜
        else if (lowerInput.Contains("胡萝卜"))
        {
            result = "<b>【百科：胡萝卜】</b>\n" +
                     "标志性的橙色源自丰富的<link=\"Link_Carotene\">β-胡萝卜素</link>，既可生食也可熟烹，营养价值极高。\n\n" +
                     "其清甜的味道深受兔类及长颈鹿等植食动物的喜爱。过量摄入会导致体液或排泄物颜色发生<link=\"Link_OrangeSubstance\">暂时性改变</link>。";
        }
        // 4. 橙色物质 / 颜色 / 色素
        else if (lowerInput.Contains("橙色物质") || lowerInput.Contains("橙色") || lowerInput.Contains("色素"))
        {
            result = "<b>【成因分析：橙色物质】</b>\n\n" +
                     "<b>1. 天然食物</b>\n" +
                     "大量食用胡萝卜、南瓜、甘薯等富含胡萝卜素的蔬果后，未被完全吸收的色素会随粪便排出。兔子小镇的居民对此早已见怪不怪。\n\n" +
                     "<b>2. 合成色素</b>\n" +
                     "某些橙色系的食用色素不易被消化，摄入后基本保持原色通过肠道。常见于彩色糖果、果冻、饮料等加工食品。";
        }
        // 4. 狒狒
        else if (lowerInput.Contains("狒狒") || lowerInput.Contains("灵长类"))
        {
            result = "<b>狒狒 杂食性动物</b>\n" + "<b>【饮食习惯】</b>\n" +
                     "以烹煮后的谷物、多种蔬果（苹果、香蕉、胡萝卜）为主，也摄入昆虫制品（如炸蝗虫、蚕蛹罐头）。食性杂乱，是典型的杂食者。\n\n" +
                     "<b>【排泄情况】</b>\n" +
                     "<b>1. 排便</b>\n" +
                     "性状：深褐色短柱状，表面常粘有未消化的<link=\"Link_Fiber\">植物纤维</link>和<link=\"Link_Shell\">昆虫外壳</link>。气味中等偏酸，带发酵果香。\n" +
                     "频量：日排便3~5次。具有强主动控制能力，会自觉划分功能区。\n\n" +
                     "<b>2. 排尿</b>\n" +
                     "尿液清澈，成年雄性气味较浓。可主动憋尿，行为常受<link=\"Link_SocialRank\">社会等级</link>影响。";
        }
        // 4. 野猪
        else if (lowerInput.Contains("野猪") || lowerInput.Contains("山猪"))
        {
            result = "<b>野猪 杂食性动物</b>\n" + "<b>【饮食习惯】</b>\n" +
                     "喜食根茎类蔬菜（土豆、红薯）、蘑菇、豆制品及面包虫干。食量巨大，偏好高纤维餐食。\n\n" +
                     "<b>【排泄情况】</b>\n" +
                     "<b>1. 排便</b>\n" +
                     "性状：深褐至黑色的不规则团块，内含大量<link=\"Link_Mycelium\">菌丝</link>。气味浓烈刺鼻，混有泥土腥臭。\n" +
                     "频量：日排便6~10次。控制能力较弱，基本随处排泄，但会避开休息区。\n\n" +
                     "<b>2. 排尿</b>\n" +
                     "单次尿量大。成年雄性会利用尿液进行<link=\"Link_Marking\">气味标记</link>（如树干、墙角）以争夺地盘。";
        }
        // 5. 白猪
        else if (lowerInput.Contains("白猪") || lowerInput.Contains("家猪"))
        {
            result = "<b>白猪 杂食性动物</b>\n" + "<b>【饮食习惯】</b>\n" +
                     "主要取食谷物（玉米、豆粕），偏爱南瓜、白菜、西瓜皮。是动物城中最常见的大型居民。\n\n" +
                     "<b>【排泄情况】</b>\n" +
                     "<b>1. 排便</b>\n" +
                     "性状：黄褐色软圆柱状或糊状。气味较重，带有明显的<link=\"Link_Ammonia\">氨味</link>。\n" +
                     "频量：日排便5~8次。极爱干净，能接受<link=\"Link_ToiletTraining\">定点排便训练</link>，但憋便耐力有限。\n\n" +
                     "<b>2. 排尿</b>\n" +
                     "尿液淡黄，排尿频繁。在良好的居住环境下会主动区分排泄区。";
        }

        searchResultDisplay.text = result;

        // 强制更新布局并重置滚动条位置
        Canvas.ForceUpdateCanvases();
        resultScrollRect.verticalNormalizedPosition = 1f;

        searchInputField.text = "";
    }

    private void UpdateLine(RectTransform lineRect, Vector2 startScreenPos, Vector2 endScreenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, startScreenPos, null, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, endScreenPos, null, out Vector2 localEnd);

        Vector2 dir = localEnd - localStart;
        float distance = dir.magnitude;

        // ✅ 修复：设置锚点在左侧（线段起点）
        lineRect.pivot = new Vector2(0, 0.5f); // 锚点在左边中心

        // ✅ 线段起点位置
        lineRect.anchoredPosition = localStart;

        // ✅ 线段长度（宽度=距离，高度=粗细）
        lineRect.sizeDelta = new Vector2(distance, 5f);

        // ✅ 线段旋转角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private string GetResultLinkAtMouse()
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(searchResultDisplay, Input.mousePosition, null);
        if (linkIndex != -1) return searchResultDisplay.textInfo.linkInfo[linkIndex].GetLinkID();
        return null;
    }

    private void CancelSelection()
    {
        selectedClueIndex = -1;
        if (activeLine != null)
        {
            Destroy(activeLine.gameObject);
            activeLine = null;
        }

        foreach (var state in currentClueStates)
        {
            if (!state.isSolved && state.clueUI != null)
                state.clueUI.GetComponentInChildren<TMP_Text>().color = Color.white;
        }
    }

    public void CloseSearchPanel()
    {
        CancelSelection();
        searchPanel.SetActive(false);
        ToggleExternalScripts(true);
        NoteManager.Instance.OnSearchPanelClosed(currentEvidenceID);
    }

    private void ToggleExternalScripts(bool isEnabled)
    {
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = isEnabled;
        }
    }
}