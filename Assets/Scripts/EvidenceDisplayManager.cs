using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ✅ 新增：证物自定义展示参数容器
[System.Serializable]
public class EvidenceDisplaySettings
{
    public float forwardDistance;
    public float upwardOffset;
    public float sideOffset;
    public float flyDuration;
    public float displayScaleFactor;
}

public class EvidenceDisplayManager : MonoBehaviour
{
    public static EvidenceDisplayManager Instance { get; private set; }

    [Header("摄像机设置")]
    public Camera targetCamera;

    [Header("展示位置设置（全局默认值）")]
    public float forwardDistance = 1.2f;
    public float upwardOffset = 0.1f;
    public float sideOffset = 0f;

    [Header("动画参数（全局默认值）")]
    public float flyDuration = 0.6f;
    public float displayScaleFactor = 2.0f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("层级设置")]
    public string displayLayer = "Default";

    // ✅ 新增：当前实际使用的参数（运行时决定用全局还是自定义）
    private float currentForwardDistance;
    private float currentUpwardOffset;
    private float currentSideOffset;
    private float currentFlyDuration;
    private float currentDisplayScaleFactor;

    private GameObject currentDisplayObject;
    private Coroutine displayCoroutine;
    private Coroutine followCoroutine;
    private List<GameObject> pages = new List<GameObject>();
    private int currentPageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) Debug.LogError("[Debug] 找不到主摄像机！请在 Inspector 检查 Target Camera 赋值。");
    }

    // ✅ 修改：增加可选参数 customSettings，默认为 null（即使用全局参数）
    public void ShowEvidence(GameObject evidencePrefab, Transform startTransform, EvidenceDisplaySettings customSettings = null)
    {
        Debug.Log($"<color=cyan>[Debug] 1. ShowEvidence 被调用。Prefab: {(evidencePrefab != null ? evidencePrefab.name : "空!")}</color>");

        // ✅ 新增：在展示开始时决定使用哪套参数
        if (customSettings != null)
        {
            currentForwardDistance = customSettings.forwardDistance;
            currentUpwardOffset = customSettings.upwardOffset;
            currentSideOffset = customSettings.sideOffset;
            currentFlyDuration = customSettings.flyDuration;
            currentDisplayScaleFactor = customSettings.displayScaleFactor;
            Debug.Log("[Debug] 使用证物自定义展示参数。");
        }
        else
        {
            currentForwardDistance = forwardDistance;
            currentUpwardOffset = upwardOffset;
            currentSideOffset = sideOffset;
            currentFlyDuration = flyDuration;
            currentDisplayScaleFactor = displayScaleFactor;
            Debug.Log("[Debug] 使用全局默认展示参数。");
        }

        if (currentDisplayObject != null) HideEvidence();

        if (evidencePrefab == null)
        {
            Debug.LogError("[Debug] 错误：你传进来的 Prefab 是空的！请检查 Evidence 物体上的插槽。");
            return;
        }

        // 实例化
        currentDisplayObject = Instantiate(evidencePrefab);
        Debug.Log($"<color=cyan>[Debug] 2. 实例生成成功: {currentDisplayObject.name}</color>");

        // 初始化页面
        InitPages();

        // 初始位置
        Vector3 startPos = startTransform.position;
        currentDisplayObject.transform.position = startPos;
        currentDisplayObject.transform.rotation = Quaternion.identity;

        // 设置层级
        int layer = LayerMask.NameToLayer(displayLayer);
        if (layer == -1)
        {
            Debug.LogWarning($"[Debug] 警告：找不到名为 '{displayLayer}' 的层级，将使用 Default。");
            layer = 0;
        }
        SetLayerRecursively(currentDisplayObject, layer);

        // 物理处理
        DisablePhysicsForInteraction(currentDisplayObject);

        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        displayCoroutine = StartCoroutine(FlyToDisplayPosition(startPos));
    }

    private void InitPages()
    {
        pages.Clear();
        currentPageIndex = 0;
        Debug.Log($"[Debug] 3. 开始扫描子物体...");

        foreach (Transform child in currentDisplayObject.transform)
        {
            pages.Add(child.gameObject);
            child.gameObject.SetActive(false);
            Debug.Log($"[Debug] - 发现页面: {child.name}");
        }

        if (pages.Count == 0)
        {
            Debug.LogWarning("[Debug] 没发现子物体。将父物体本身设为显示目标。");
            pages.Add(currentDisplayObject);
        }

        if (pages.Count > 0)
        {
            pages[0].SetActive(true);
            Debug.Log($"<color=green>[Debug] 4. 初始化完成。总页数: {pages.Count}，当前显示: {pages[0].name}</color>");
        }
    }

    private IEnumerator FlyToDisplayPosition(Vector3 startPos)
    {
        Debug.Log("[Debug] 5. 飞行动画开始执行...");

        float elapsed = 0f;
        Vector3 startScale = currentDisplayObject.transform.localScale;

        if (currentDisplayScaleFactor <= 0)
            Debug.LogWarning("[Debug] 警告：displayScaleFactor 为 0，物体会不可见！");

        // ✅ 修改：while 条件改用 currentFlyDuration
        while (elapsed < currentFlyDuration)
        {
            elapsed += Time.deltaTime;

            // ✅ 修改：t 的计算改用 currentFlyDuration
            float t = flyCurve.Evaluate(elapsed / currentFlyDuration);

            Vector3 targetPos = GetDisplayPosition();
            currentDisplayObject.transform.position = Vector3.Lerp(startPos, targetPos, t);

            // 旋转适配
            float camY = targetCamera.transform.eulerAngles.y;
            currentDisplayObject.transform.rotation =
                Quaternion.Euler(0, camY + 180f, 0) * Quaternion.Euler(0f, -90f, 20f);

            // ✅ 修改：缩放改用 currentDisplayScaleFactor
            currentDisplayObject.transform.localScale =
                Vector3.Lerp(startScale, startScale * currentDisplayScaleFactor, t);

            yield return null;
        }

        Debug.Log($"<color=green>[Debug] 6. 飞行结束。当前坐标: {currentDisplayObject.transform.position}</color>");
        followCoroutine = StartCoroutine(FollowCamera());
    }

    private IEnumerator FollowCamera()
    {
        while (currentDisplayObject != null && targetCamera != null)
        {
            currentDisplayObject.transform.position = Vector3.Lerp(
                currentDisplayObject.transform.position,
                GetDisplayPosition(),
                Time.deltaTime * 5f
            );

            float camY = targetCamera.transform.eulerAngles.y;
            currentDisplayObject.transform.rotation =
                Quaternion.Euler(0, camY + 180f, 0) * Quaternion.Euler(0f, -90f, 20f);

            if (Input.GetMouseButtonDown(0)) CheckForPageTurn();

            yield return null;
        }
    }

    private void CheckForPageTurn()
    {
        // 1. 如果只有一页，直接不处理
        if (pages.Count <= 1) return;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 2. 只要射线撞到了"任何"东西（说明你点在证物上了）
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log($"<color=yellow>[Debug] 强制翻页触发！撞击物体: {hit.transform.name}</color>");
            TurnToNextPage();
        }
    }

    private void TurnToNextPage()
    {
        pages[currentPageIndex].SetActive(false);
        currentPageIndex = (currentPageIndex + 1) % pages.Count;
        pages[currentPageIndex].SetActive(true);
        Debug.Log($"<color=yellow>[Debug] 翻页成功！当前第 {currentPageIndex + 1} 页</color>");
    }

    public void HideEvidence()
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        if (followCoroutine != null) StopCoroutine(followCoroutine);
        if (currentDisplayObject != null) Destroy(currentDisplayObject);
        pages.Clear();
        Debug.Log("[Debug] 证物已隐藏并销毁。");
    }

    // ✅ 修改：改用 current 系列变量
    private Vector3 GetDisplayPosition()
    {
        return targetCamera.transform.position +
               targetCamera.transform.forward * currentForwardDistance +
               targetCamera.transform.up * currentUpwardOffset +
               targetCamera.transform.right * currentSideOffset;
    }

    private void DisablePhysicsForInteraction(GameObject obj)
    {
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var col in obj.GetComponentsInChildren<Collider>()) col.enabled = true;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
    }
}
