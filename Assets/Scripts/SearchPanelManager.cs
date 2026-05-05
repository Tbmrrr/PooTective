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

        if (input.Contains("长颈鹿") || input.Contains("植食") || input.Contains("排泄"))
        {
            result = "<b>【饮食习惯】</b>\n" +
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

        searchResultDisplay.text = result;
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