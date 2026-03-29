using UnityEngine;
using System.Collections;

public class EvidenceDisplayManager : MonoBehaviour
{
    public static EvidenceDisplayManager Instance { get; private set; }

    [Header("摄像机设置")]
    public Camera targetCamera;

    [Header("展示位置设置")]
    public float forwardDistance = 2f;
    public float upwardOffset = 0.5f;
    public float sideOffset = 0f;

    [Header("动画参数")]
    public float flyDuration = 0.8f;
    public float displayScaleFactor = 2.5f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("弧线参数")]
    public float arcHeightFactor = 0.2f;

    [Header("层级设置")]
    public string displayLayer = "UI";

    private GameObject currentDisplayObject;
    private Coroutine displayCoroutine;
    private Coroutine followCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void ShowEvidence(GameObject evidencePrefab, Transform startTransform, Quaternion targetModelLocalRotation)
    {
        if (currentDisplayObject != null) HideEvidence();
        if (evidencePrefab == null || startTransform == null) return;

        Vector3 startPos = startTransform.position;
        currentDisplayObject = Instantiate(evidencePrefab);

        // 初始化：先给它一个干净的旋转
        currentDisplayObject.transform.rotation = Quaternion.identity;

        AlignVisualCenter(currentDisplayObject, startPos);

        Vector3 startScale = currentDisplayObject.transform.localScale;

        SetLayerRecursively(currentDisplayObject, LayerMask.NameToLayer(displayLayer));
        DisablePhysics(currentDisplayObject);

        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        
        // 注意：这里我们依然带着 lockedLocalRot，但在协程里会用统一逻辑处理它
        displayCoroutine = StartCoroutine(FlyToDisplayPosition(startPos, targetModelLocalRotation, startScale));
    }

    private void AlignVisualCenter(GameObject obj, Vector3 targetPos)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            obj.transform.position = targetPos;
            return;
        }
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        Vector3 offset = bounds.center - obj.transform.position;
        obj.transform.position = targetPos - offset;
    }

    private IEnumerator FlyToDisplayPosition(Vector3 startPos, Quaternion lockedLocalRot, Vector3 startScale)
    {
        if (currentDisplayObject == null || targetCamera == null) yield break;

        Vector3 targetScale = startScale * displayScaleFactor;
        float elapsed = 0f;
        float distance = Vector3.Distance(startPos, GetDisplayPosition());
        float arcHeight = distance * arcHeightFactor;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = flyCurve.Evaluate(elapsed / flyDuration);

            Vector3 targetPos = GetDisplayPosition();
            currentDisplayObject.transform.position = GetArcPosition(startPos, targetPos, t, arcHeight);

            // ✅ 核心修正逻辑：
            // 我们不再相信 lockedLocalRot 能包治百病，我们直接让它面朝相机方向
            // 然后根据你说的“正对着”的需求，做一个统一的 Y 轴旋转
            float camY = targetCamera.transform.eulerAngles.y;
            
            // 重点：我们让模型先看向相机水平方向，再叠加一个 180 度（让正面对着玩家）
            // 如果你的模型是侧着的，这里我们就根据你报纸的经验，统一加上 Y 轴的偏移
            Quaternion baseRot = Quaternion.Euler(0, camY + 180f, 0); 
            
            // 如果 A 和 B 在场景里角度不同但面朝向一样，说明它们的 Mesh 轴向本身就有偏角
            // 这里我们强行应用你调好的那个 LocalRotation 的 Y 轴偏值
            currentDisplayObject.transform.rotation = baseRot * Quaternion.Euler(0, lockedLocalRot.eulerAngles.y, 0);

            currentDisplayObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        followCoroutine = StartCoroutine(FollowCamera(lockedLocalRot));
    }

    private IEnumerator FollowCamera(Quaternion lockedLocalRot)
    {
        while (currentDisplayObject != null && targetCamera != null)
        {
            currentDisplayObject.transform.position = Vector3.Lerp(
                currentDisplayObject.transform.position,
                GetDisplayPosition(),
                Time.deltaTime * 5f
            );

            float camY = targetCamera.transform.eulerAngles.y;
            // 保持跟飞行结束时一致的逻辑
            currentDisplayObject.transform.rotation = Quaternion.Euler(0, camY + 180f, 0) * Quaternion.Euler(0, lockedLocalRot.eulerAngles.y, 0);

            yield return null;
        }
    }

    // --- 其余函数（GetArcPosition, HideEvidence, GetDisplayPosition, DisablePhysics, SetLayerRecursively）保持不变 ---
    private Vector3 GetArcPosition(Vector3 start, Vector3 end, float t, float height)
    {
        Vector3 linear = Vector3.Lerp(start, end, t);
        float arc = Mathf.Sin(t * Mathf.PI) * height;
        return linear + Vector3.up * arc;
    }

    public void HideEvidence()
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        if (followCoroutine != null) StopCoroutine(followCoroutine);
        if (currentDisplayObject != null) Destroy(currentDisplayObject);
    }

    private Vector3 GetDisplayPosition()
    {
        return targetCamera.transform.position +
               targetCamera.transform.forward * forwardDistance +
               targetCamera.transform.up * upwardOffset +
               targetCamera.transform.right * sideOffset;
    }

    private void DisablePhysics(GameObject obj)
    {
        foreach (var col in obj.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
    }
}