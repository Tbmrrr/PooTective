using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class SearchPanelManager : MonoBehaviour
{
    public static SearchPanelManager Instance { get; private set; }

    [System.Serializable]
    public class ClueSlotConfig
    {
        [Tooltip("TextMeshPro 文本中对应的 link ID（例如：Link_Fiber）")]
        public string targetLinkID;

        [Tooltip("A区：场景中已有的图片按钮")]
        public Button clueButton;

        [Tooltip("A区：对应成功后出现的对应结果图片")]
        public GameObject successImage;
    }

    [Header("UI 根节点")]
    public GameObject searchPanel;
    public Button closeButton;

    [Header("A区：线索绑定配置（直接在面板关联 Link、按钮和结果图）")]
    public List<ClueSlotConfig> clueSlots = new List<ClueSlotConfig>();

    [Header("B区：结果显示")]
    public TMP_Text searchResultDisplay;
    public ScrollRect resultScrollRect;

    [Header("连线设置")]
    public RectTransform lineContainer;
    public Image linePrefab;
    public Color highlightColor = Color.yellow;
    public Color successColor = Color.green;
    // ✨ 新增：公开的线宽控制，默认设为 2（原本硬编码是 5）
    [Tooltip("连线的宽度/粗细，数值越小线越细")]
    public float lineWidth = 2f;

    [Header("交互控制")]
    [Tooltip("在搜索面板打开时需要禁用的脚本列表")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("自定义图片轮播")]
    public Button customSequenceButton;
    public GameObject customImage1;
    public GameObject customImage2;
    public GameObject customImage3;

    private class ClueState
    {
        public string targetLinkID;
        public string resultText;
        public bool isSolved;
    }

    private Dictionary<string, List<ClueState>> allEvidenceStates = new Dictionary<string, List<ClueState>>();
    private List<ClueState> currentClueStates = new List<ClueState>();
    private string currentEvidenceID;

    private Image activeLine;
    private int selectedClueIndex = -1;
    private Vector2 lineFixedStartPos;

    private int hoveredLinkIndex = -1;
    private Color32 hoverColor32 = new Color32(173, 109, 74, 255);

    private int currentSequenceIndex = -1;
    private bool skipFirstClickFrame = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (searchPanel != null) searchPanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSearchPanel);
        }

        if (customSequenceButton != null)
        {
            customSequenceButton.onClick.AddListener(StartImageSequence);
        }

        ResetCustomImages();
    }

    void Update()
    {
        if (!searchPanel.activeSelf) return;

        if (currentSequenceIndex >= 0)
        {
            HandleImageSequence();
            return;
        }

        if (selectedClueIndex != -1 && activeLine != null)
        {
            UpdateLine(activeLine.rectTransform, lineFixedStartPos, Input.mousePosition);
        }

        HandleLineDrawing();
        UpdateLinkHover();

        if (Input.GetMouseButtonDown(1)) CancelSelection();
    }

    private void StartImageSequence()
    {
        if (customImage1 == null || customImage2 == null || customImage3 == null)
        {
            Debug.LogWarning("轮播图片未在 Inspector 中配置完整！");
            return;
        }

        customImage1.SetActive(true);
        customImage2.SetActive(false);
        customImage3.SetActive(false);

        currentSequenceIndex = 0;
        skipFirstClickFrame = true;
    }

    private void HandleImageSequence()
    {
        if (skipFirstClickFrame)
        {
            skipFirstClickFrame = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            currentSequenceIndex++;
            ResetCustomImages();

            if (currentSequenceIndex == 1)
            {
                customImage2.SetActive(true);
            }
            else if (currentSequenceIndex == 2)
            {
                customImage3.SetActive(true);
            }
            else
            {
                currentSequenceIndex = -1;
                Debug.Log("图片轮播播放完毕，关闭所有轮播图。");
            }
        }
    }

    private void ResetCustomImages()
    {
        if (customImage1 != null) customImage1.SetActive(false);
        if (customImage2 != null) customImage2.SetActive(false);
        if (customImage3 != null) customImage3.SetActive(false);
    }

    public void OpenSearchPanel(NoteManager.EvidenceData evidenceData, List<SearchableKeyword> keywords)
    {
        ToggleExternalScripts(false);
        currentEvidenceID = evidenceData.evidenceID;

        if (!allEvidenceStates.ContainsKey(currentEvidenceID))
        {
            InitializeClues(evidenceData.desc, keywords);
        }
        else
        {
            currentClueStates = allEvidenceStates[currentEvidenceID];
        }

        RefreshClueUI();
        ShowDefaultResult();
        searchPanel.SetActive(true);
    }

    private void ShowDefaultResult()
    {
        string giraffeText = "<b>饮食：</b>\n" +
                     "        每天需要摄入大量高纤维、低营养的植物性食物，以合欢树叶、金合欢叶为主食，单日进食量可达30至60公斤。为了满足庞大的能量需求，长颈鹿一天中会花费大量时间进食。此外，水果（如苹果、香蕉）、胡萝卜、南瓜、西瓜等高糖分蔬果，是深受长颈鹿欢迎的零食，通常作为正餐之外的补充。\n\n" +
                     "<b>排泄情况：</b>\n" +
                     "<b>排便</b>\n" +
                     " · 性状：\n" +
                     "        健康的长颈鹿每天排出大量<link=\"Link_wrong\">颗粒状</link>、质地较为<link=\"Link_Fiber\">干燥紧密</link>的<link=\"Link_Carrot\">深褐色</link><link=\"Link_Fiber\">固体</link>粪球，表面<link=\"Link_wrong\">光滑</link>。\n" +
                     " · 成分：\n" +
                     "        粪便中几乎全是未消化的植物纤维，散发<link=\"Link_Sugar\">草料发酵气味</link>，无明显臭味。\n" +
                     " · 频量：\n" +
                     "        <link=\"Link_Starvation\">日排泄量可达15公斤左右</link>，排便次数约10至15次。控制能力较弱。\n\n" +
                     "<b>排尿</b>\n" +
                     "        排尿次数较少，每天约3至5次，单次尿量很大（一次可排出数升）。与排便不同，长颈鹿对排尿具有较好的主动控制能力，会刻意避开自己睡觉和进食的区域。";

        searchResultDisplay.text = giraffeText;
        Canvas.ForceUpdateCanvases();
        resultScrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateLinkHover()
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(searchResultDisplay, Input.mousePosition, null);

        if (linkIndex != hoveredLinkIndex)
        {
            hoveredLinkIndex = linkIndex;
            searchResultDisplay.ForceMeshUpdate();

            if (hoveredLinkIndex != -1)
            {
                TMP_LinkInfo linkInfo = searchResultDisplay.textInfo.linkInfo[hoveredLinkIndex];

                for (int i = 0; i < linkInfo.linkTextLength; i++)
                {
                    int charIndex = linkInfo.linkTextfirstCharacterIndex + i;
                    TMP_CharacterInfo charInfo = searchResultDisplay.textInfo.characterInfo[charIndex];

                    if (!charInfo.isVisible) continue;

                    int meshIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Color32[] vertexColors = searchResultDisplay.textInfo.meshInfo[meshIndex].colors32;

                    vertexColors[vertexIndex + 0] = hoverColor32;
                    vertexColors[vertexIndex + 1] = hoverColor32;
                    vertexColors[vertexIndex + 2] = hoverColor32;
                    vertexColors[vertexIndex + 3] = hoverColor32;
                }
                searchResultDisplay.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }
    }

    private void InitializeClues(string fullDesc, List<SearchableKeyword> keywords)
    {
        List<ClueState> newStates = new List<ClueState>();

        for (int i = 0; i < clueSlots.Count; i++)
        {
            var slot = clueSlots[i];
            var state = new ClueState
            {
                targetLinkID = slot.targetLinkID,
                isSolved = false,
                resultText = ""
            };

            if (!string.IsNullOrEmpty(slot.targetLinkID) && keywords != null)
            {
                var matchedKeyword = keywords.FirstOrDefault(k => k.keyword == slot.targetLinkID);
                if (matchedKeyword != null)
                {
                    state.resultText = matchedKeyword.newDescriptionOnSearch;
                }
            }
            newStates.Add(state);
        }

        allEvidenceStates[currentEvidenceID] = newStates;
        currentClueStates = newStates;
    }

    private void RefreshClueUI()
    {
        for (int i = 0; i < clueSlots.Count; i++)
        {
            var slot = clueSlots[i];
            if (slot == null || slot.clueButton == null) continue;

            int index = i;
            slot.clueButton.onClick.RemoveAllListeners();
            slot.clueButton.onClick.AddListener(() => OnClueClicked(index));

            if (slot.successImage != null)
            {
                bool isSolved = i < currentClueStates.Count ? currentClueStates[i].isSolved : false;
                slot.successImage.SetActive(isSolved);
            }
        }
    }

    private void OnClueClicked(int index)
    {
        if (index >= currentClueStates.Count || currentClueStates[index].isSolved) return;

        if (selectedClueIndex != -1)
        {
            CancelSelection();
        }

        selectedClueIndex = index;
        lineFixedStartPos = Input.mousePosition;

        activeLine = Instantiate(linePrefab, lineContainer);
        activeLine.gameObject.SetActive(true);
        activeLine.color = highlightColor;
    }

    private void HandleLineDrawing()
    {
        if (selectedClueIndex == -1 || activeLine == null) return;

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
        if (!string.IsNullOrEmpty(currentClue.targetLinkID) && linkID == currentClue.targetLinkID)
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
        if (line == null) yield break;

        currentClueStates[index].isSolved = true;

        if (index < clueSlots.Count && clueSlots[index].successImage != null)
        {
            clueSlots[index].successImage.SetActive(true);
        }

        line.color = successColor;
        yield return new WaitForSecondsRealtime(0.5f);

        foreach (Transform child in lineContainer)
        {
            Destroy(child.gameObject);
        }

        yield return null;
        CheckAllComplete();
    }

    private void CheckAllComplete()
    {
        if (currentClueStates.All(s => s.isSolved))
        {
            string finalDesc = string.Join("\n", currentClueStates.Select(s => s.resultText));
            NoteManager.Instance.UpdateEvidenceDescDirectly(currentEvidenceID, finalDesc);

            Evidence ev = NoteManager.Instance.GetEvidenceComponent(currentEvidenceID);
            if (ev != null)
            {
                ev.description = finalDesc;
                ev.SetUpdateFlag();
            }

            NoteManager.Instance.MarkSearchAsCompleted(currentEvidenceID);
            NoteManager.Instance.UpdateEvidenceInfo(currentEvidenceID, finalDesc);
        }
    }

    // ✅ 修改：现在高度(Y轴)使用的是面板配置的 lineWidth
    private void UpdateLine(RectTransform lineRect, Vector2 startScreenPos, Vector2 endScreenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, startScreenPos, null, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, endScreenPos, null, out Vector2 localEnd);

        Vector2 dir = localEnd - localStart;
        float distance = dir.magnitude;

        lineRect.pivot = new Vector2(0, 0.5f);
        lineRect.anchoredPosition = localStart;
        lineRect.sizeDelta = new Vector2(distance, lineWidth); // 应用新线宽

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
    }

    public void CloseSearchPanel()
    {
        CancelSelection();
        hoveredLinkIndex = -1;
        searchPanel.SetActive(false);
        ToggleExternalScripts(true);

        currentSequenceIndex = -1;
        ResetCustomImages();

        NoteManager.Instance.OnSearchPanelClosed(currentEvidenceID);
    }

    private void ToggleExternalScripts(bool isEnabled)
    {
        foreach (var script in scriptsToDisable)
        {
            if (script != null)
            {
                // ✨ 新增：在关闭/开启组件时，精准打印是谁被操作了
                if (!isEnabled)
                {
                    Debug.LogWarning($"【线索追踪】正在禁用物体【{script.gameObject.name}】上的组件【{script.GetType().Name}】", script);
                }

                script.enabled = isEnabled;
            }
        }
    }
}