using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SearchPanelManager : MonoBehaviour
{
    public static SearchPanelManager Instance { get; private set; }

    [Header("搜索面板根节点")]
    public GameObject searchPanel;

    [Header("左侧：证物详情")]
    public Image leftDetailImage;
    public Text leftDetailName;
    public TMP_Text leftDetailDesc;

    [Header("右侧：搜索区域")]
    public Text searchResultText;
    public Image closeHintImage;
    public Text dropZonePlaceholderText;

    [Header("拖拽Ghost预制体")]
    public GameObject dragGhostPrefab;

    [Header("高亮颜色")]
    public Color highlightColor = new Color(1f, 0.85f, 0f);

    [Header("搜索框区域")]
    public RectTransform searchDropZone;

    // 内部状态
    private NoteManager.EvidenceData currentEvidenceData;
    private List<SearchableKeyword> currentKeywords;
    private string currentEvidenceID;
    private HashSet<string> searchedKeywordsThisSession = new HashSet<string>();

    // 拖拽状态
    private GameObject activeDragGhost;
    private string draggingKeyword;
    private Canvas rootCanvas;

    // 拖拽平滑判定
    private Vector3 lastMouseDownPos;
    private bool isDraggingThresholdPassed = false;

    // 状态控制
    private bool isSearchUsed = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (searchPanel != null)
        {
            rootCanvas = searchPanel.GetComponentInParent<Canvas>();
        }
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
    }

    void Start()
    {
        if (searchPanel != null) searchPanel.SetActive(false);
        if (searchResultText != null) searchResultText.text = "";
    }

    void Update()
    {
        if (!searchPanel.activeSelf) return;

        // 如果已经搜过了，就不再处理拖拽
        if (isSearchUsed) return;

        HandleKeywordDrag();
    }

    public void OpenSearchPanel(NoteManager.EvidenceData evidenceData, List<SearchableKeyword> keywords)
    {
        currentEvidenceData = evidenceData;
        currentKeywords = keywords;
        currentEvidenceID = evidenceData.evidenceID;
        searchedKeywordsThisSession.Clear();

        isSearchUsed = false;
        if (dropZonePlaceholderText != null)
        {
            dropZonePlaceholderText.text = "将关键词拖入此处";
            dropZonePlaceholderText.color = Color.white;
        }

        if (leftDetailImage != null) leftDetailImage.sprite = evidenceData.fullImage;
        if (leftDetailName != null) leftDetailName.text = evidenceData.name;

        RenderHighlightedDesc(evidenceData.desc);

        if (searchResultText != null) searchResultText.text = "";

        searchPanel.SetActive(true);
    }

    public void CloseSearchPanel()
    {
        if (searchPanel == null || !searchPanel.activeSelf) return;

        if (activeDragGhost != null)
        {
            Destroy(activeDragGhost);
            activeDragGhost = null;
        }
        draggingKeyword = null;

        // ✅ 原有的 append 拼凑逻辑已删除，因为我们改用了即时全量更新
        searchedKeywordsThisSession.Clear();
        searchPanel.SetActive(false);

        NoteManager.Instance.OnSearchPanelClosed(currentEvidenceID);
    }

    private void RenderHighlightedDesc(string desc)
    {
        if (currentKeywords == null || currentKeywords.Count == 0)
        {
            if (leftDetailDesc != null) leftDetailDesc.text = desc;
            return;
        }

        string colorHex = ColorUtility.ToHtmlStringRGB(highlightColor);
        string result = desc;

        foreach (var kw in currentKeywords)
        {
            if (string.IsNullOrEmpty(kw.keyword)) continue;
            string replacement = $"<link=\"{kw.keyword}\"><color=#{colorHex}>{kw.keyword}</color></link>";
            result = result.Replace(kw.keyword, replacement);
        }

        if (leftDetailDesc != null) leftDetailDesc.text = result;
    }

    private void HandleKeywordDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            string hitKeyword = GetKeywordUnderMouse();
            if (!string.IsNullOrEmpty(hitKeyword))
            {
                draggingKeyword = hitKeyword;
                lastMouseDownPos = Input.mousePosition;
                isDraggingThresholdPassed = false;
            }
        }

        if (Input.GetMouseButton(0) && !string.IsNullOrEmpty(draggingKeyword))
        {
            if (!isDraggingThresholdPassed)
            {
                if (Vector3.Distance(lastMouseDownPos, Input.mousePosition) > 10f)
                {
                    isDraggingThresholdPassed = true;
                    BeginDrag(draggingKeyword);
                }
            }

            if (activeDragGhost != null)
            {
                activeDragGhost.GetComponent<RectTransform>().position = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingThresholdPassed && activeDragGhost != null)
            {
                EndDrag();
            }
            draggingKeyword = null;
            isDraggingThresholdPassed = false;
        }
    }

    private string GetKeywordUnderMouse()
    {
        if (leftDetailDesc == null) return null;
        leftDetailDesc.ForceMeshUpdate();

        Canvas parentCanvas = leftDetailDesc.GetComponentInParent<Canvas>();
        Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? (parentCanvas.worldCamera ?? Camera.main) : null;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(leftDetailDesc.rectTransform, Input.mousePosition, cam, out localPoint))
            return null;

        TMP_TextInfo textInfo = leftDetailDesc.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            if (localPoint.x >= charInfo.bottomLeft.x && localPoint.x <= charInfo.topRight.x &&
                localPoint.y >= charInfo.bottomLeft.y && localPoint.y <= charInfo.topRight.y)
            {
                for (int j = 0; j < textInfo.linkCount; j++)
                {
                    TMP_LinkInfo linkInfo = textInfo.linkInfo[j];
                    if (i >= linkInfo.linkTextfirstCharacterIndex && i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength)
                        return linkInfo.GetLinkID();
                }
            }
        }
        return null;
    }

    private void BeginDrag(string keyword)
    {
        if (dragGhostPrefab == null || searchPanel == null) return;

        activeDragGhost = Instantiate(dragGhostPrefab, rootCanvas.transform);
        activeDragGhost.transform.SetAsLastSibling();

        TMP_Text ghostText = activeDragGhost.GetComponentInChildren<TMP_Text>();
        if (ghostText != null) ghostText.text = keyword;

        activeDragGhost.transform.localScale = Vector3.one;
        activeDragGhost.GetComponent<RectTransform>().position = Input.mousePosition;
        activeDragGhost.layer = LayerMask.NameToLayer("UI");
    }

    private void EndDrag()
    {
        if (activeDragGhost == null) return;

        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null :
                     (rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main);

        bool droppedInZone = false;
        if (searchDropZone != null)
        {
            droppedInZone = RectTransformUtility.RectangleContainsScreenPoint(searchDropZone, Input.mousePosition, cam);
        }

        Destroy(activeDragGhost);
        activeDragGhost = null;

        if (droppedInZone && !string.IsNullOrEmpty(draggingKeyword))
        {
            OnKeywordDropped(draggingKeyword);
        }
    }

    private void OnKeywordDropped(string keyword)
    {
        if (currentKeywords == null) return;
        foreach (var kw in currentKeywords)
        {
            if (kw.keyword == keyword)
            {
                if (searchResultText != null) searchResultText.text = kw.searchResult;

                if (dropZonePlaceholderText != null)
                {
                    dropZonePlaceholderText.text = keyword;
                    dropZonePlaceholderText.color = highlightColor;
                }

                // ✅ 核心业务逻辑修改点：全量更新 NoteManager 里的缓存描述
                // 在 SearchPanelManager.cs 的 OnKeywordDropped 方法中
                if (!string.IsNullOrEmpty(kw.newDescriptionOnSearch))
                {
                    // 1. 更新 NoteManager 内部的列表数据（这样背包刷新时文字就变了）
                    NoteManager.Instance.UpdateEvidenceDescDirectly(currentEvidenceID, kw.newDescriptionOnSearch);

                    // 2. 更新场景中 Evidence 脚本里的 description 变量
                    // ✅ 修复：通过 NoteManager 新写的接口获取组件，不受 SetActive(false) 影响
                    Evidence ev = NoteManager.Instance.GetEvidenceComponent(currentEvidenceID);
                    if (ev != null)
                    {
                        ev.description = kw.newDescriptionOnSearch;
                        ev.SetUpdateFlag(); // 开启【线索更新】红字
                        Debug.Log($"[Search] 已同步更新证物脚本数据: {currentEvidenceID}");
                    }
                }

                searchedKeywordsThisSession.Add(keyword);
                isSearchUsed = true;

                NoteManager.Instance.MarkSearchAsCompleted(currentEvidenceID);
                return;
            }
        }
    }
}