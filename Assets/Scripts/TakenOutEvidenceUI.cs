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

    [Tooltip("用于射线检测的摄像机，通常是 MainCamera")]
    public Camera interactionCamera;

    [HideInInspector] public string currentEvidenceID;
    private bool isDragging = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 如果你希望这个 UI 在切换场景时不消失，可以加这句（可选）
            // DontDestroyOnLoad(gameObject); 
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 记录初始的固定悬浮位置（通常在屏幕边缘）
        savedFixedPos = rectTransform.anchoredPosition;
        gameObject.SetActive(false);
    }

    // 判断逻辑：当前是否可以开始拖拽
    private bool CanStartDragging()
    {
        // 1. 必须在重建模式
        bool isRebuild = (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive);
        // 2. 笔记本面板必须是关闭状态
        bool isNoteClosed = (NoteManager.Instance != null && !NoteManager.Instance.notePanel.activeSelf);

        return isRebuild && isNoteClosed;
    }

    public void TakeOut(NoteManager.EvidenceData data)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        currentEvidenceID = data.evidenceID; // 这里现在可能是证物 ID，也可能是员工 ID
        iconImage.sprite = data.icon;
        if (nameLabel != null) nameLabel.text = data.name;

        HideDecorations();

        // 记录鼠标点击位置并转换坐标，产生一个从点击位置“飞入”侧边栏的效果
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
        if (!CanStartDragging())
        {
            Debug.Log("【系统】当前笔记本未关闭或不在重建模式，禁止拖拽。");
            return;
        }

        isDragging = true;
        HideDecorations();
        canvasGroup.blocksRaycasts = false; // 拖拽时关闭自身射线阻挡，以便射线穿透到场景物体
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        bool isMatchSuccessful = false;

        // 优先使用手动指定的摄像机，否则使用主相机
        Camera camToUse = interactionCamera != null ? interactionCamera : Camera.main;

        if (camToUse != null)
        {
            Ray ray = camToUse.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                Debug.Log("射线击中了: " + hit.collider.name);

                // 获取接收器组件
                EvidenceReceiver receiver = hit.collider.GetComponent<EvidenceReceiver>();
                if (receiver != null)
                {
                    // ✅ 调用更新后的 TryMatch 接口
                    if (receiver.TryMatch(currentEvidenceID))
                    {
                        isMatchSuccessful = true;
                    }
                }
            }
        }

        // 处理结果
        if (isMatchSuccessful)
        {
            // 匹配成功由 EvidenceReceiver 逻辑触发 Close() 或在这里处理
            Close();
        }
        else
        {
            // 匹配失败，飞回原来的侧边位置
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

    // --- 公共关闭接口 ---
    public void Close()
    {
        Debug.Log("【系统】收回/消耗取出项: " + currentEvidenceID);
        currentEvidenceID = null;
        gameObject.SetActive(false);

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnTakenOutItemClosed();
        }
    }

    // 绑定给 UI 上自带的叉叉按钮
    public void OnClickClose()
    {
        Close();
    }

    // --- 悬浮检测 ---
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
            // ✅ 必须使用 unscaledDeltaTime，否则背包打开时（时间停止）动画会卡死
            t += Time.unscaledDeltaTime * flySpeed;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, savedFixedPos, t);
            yield return null;
        }
        rectTransform.anchoredPosition = savedFixedPos;
        Debug.Log("证物飞入侧边栏完成");
    }
}