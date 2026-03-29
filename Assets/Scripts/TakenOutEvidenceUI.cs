using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TakenOutEvidenceUI : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    public static TakenOutEvidenceUI Instance { get; private set; }

    [Header("组件引用")]
    public Image iconImage;
    public GameObject closeButton;
    public Text nameLabel;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [Header("移动设置")]
    public float flySpeed = 8f;
    private Vector2 savedFixedPos;

    [Tooltip("camera")]
    public Camera interactionCamera;

    [HideInInspector] public string currentEvidenceID;
    private bool isDragging = false;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        savedFixedPos = rectTransform.anchoredPosition;
        gameObject.SetActive(false);
    }

    // ✅ 新增判断逻辑：当前是否可以开始拖拽？
    private bool CanStartDragging()
    {
        // 1. 必须在重建模式
        bool isRebuild = (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive);
        // 2. 笔记本面板必须是关闭状态 (假设 NoteManager 的面板叫 notePanel)
        bool isNoteClosed = (NoteManager.Instance != null && !NoteManager.Instance.notePanel.activeSelf);

        return isRebuild && isNoteClosed;
    }

    public void TakeOut(NoteManager.EvidenceData data)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        currentEvidenceID = data.evidenceID;
        iconImage.sprite = data.icon;
        if (nameLabel != null) nameLabel.text = data.name;

        HideDecorations();

        // 记录鼠标点击位置并转换坐标
        rectTransform.position = Input.mousePosition;
        Vector2 startAnchoredPos = rectTransform.anchoredPosition;

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FlyToTarget(startAnchoredPos));
        }
    }

    // --- 拖拽处理 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // ✅ 拦截检查：如果不满足条件，直接退出
        if (!CanStartDragging())
        {
            Debug.Log("【系统】当前笔记本未关闭或不在重建模式，禁止拖拽证物。");
            return;
        }

        isDragging = true;
        HideDecorations();
        canvasGroup.blocksRaycasts = false;
        Debug.Log("开始拖拽证物: " + currentEvidenceID);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ✅ 持续检查：防止中途状态改变（虽然极少发生）
        if (!isDragging) return;

        rectTransform.position = eventData.position;
    }

    // 记得在顶部引入
    // using UnityEngine;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        bool isMatchSuccessful = false;

        // ✅ 核心修正：优先使用手动指定的摄像机，如果没有指定，则尝试寻找主摄像机
        Camera camToUse = interactionCamera != null ? interactionCamera : Camera.main;

        if (camToUse == null)
        {
            Debug.LogError("FATAL: 既没有指定 interactionCamera，场景中也找不到 MainCamera！无法执行射线检测。");
        }
        else
        {
            // 使用正确的摄像机发射射线
            Ray ray = camToUse.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                Debug.Log("射线击中了: " + hit.collider.name); // 打印击中物体，方便排查数据

                EvidenceReceiver receiver = hit.collider.GetComponent<EvidenceReceiver>();
                if (receiver != null)
                {
                    if (receiver.TryMatchEvidence(currentEvidenceID))
                    {
                        isMatchSuccessful = true;
                    }
                }
            }
        }

        // 处理结果 (逻辑同前...)
        if (isMatchSuccessful)
        {
            gameObject.SetActive(false);
            if (NoteManager.Instance != null) NoteManager.Instance.OnTakenOutItemClosed();
        }
        else
        {
            StopAllCoroutines();
            if (rectTransform != null) StartCoroutine(FlyToTarget(rectTransform.anchoredPosition));
        }
    }

    // --- 装饰元素控制 ---
    private void HideDecorations()
    {
        if (closeButton != null) closeButton.SetActive(false);
        if (nameLabel != null) nameLabel.gameObject.SetActive(false);
    }

    private void ShowDecorations()
    {
        if (closeButton != null) closeButton.SetActive(true);
        if (nameLabel != null) nameLabel.gameObject.SetActive(true);
    }

    // --- 叉叉点击事件 ---
    public void OnClickClose()
    {
        Debug.Log("【Debug】收回证物: " + currentEvidenceID);

        currentEvidenceID = null;
        gameObject.SetActive(false);

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnTakenOutItemClosed();
        }
    }

    // --- 悬浮检测 (保持开启，即使不能拖拽也可以看名字) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging)
        {
            ShowDecorations();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDecorations();
    }

    IEnumerator FlyToTarget(Vector2 startPos)
    {
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime * flySpeed;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, savedFixedPos, t);
            yield return null;
        }
        rectTransform.anchoredPosition = savedFixedPos;
    }
}