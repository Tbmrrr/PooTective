using UnityEngine;
using System.Collections;

public class EvidenceDisplayManager : MonoBehaviour
{
    public static EvidenceDisplayManager Instance { get; private set; }

    [Header("摄像机设置")]
    public Camera targetCamera;

    [Header("展示位置设置")]
    public float forwardDistance = 5f;
    public float upwardOffset = 1.5f;
    public float sideOffset = 0f;

    [Header("动画参数")]
    public float flyDuration = 0.8f;
    public float displayScaleFactor = 2.5f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("弧线参数")]
    [Tooltip("弧线高度系数（根据距离自动计算）")]
    public float arcHeightFactor = 0.2f; // ⭐ 推荐 0.15 ~ 0.3

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

    public void ShowEvidence(GameObject evidencePrefab, Transform startTransform)
    {
        if (currentDisplayObject != null) HideEvidence();
        if (evidencePrefab == null || startTransform == null) return;

        Vector3 startPos = startTransform.position;
        Quaternion startRot = startTransform.rotation;

        Debug.Log($"[证物展示] 起点位置: {startPos}");

        currentDisplayObject = Instantiate(evidencePrefab);

        // ⭐ 对齐视觉中心（解决模型偏移问题）
        AlignVisualCenter(currentDisplayObject, startPos);

        currentDisplayObject.transform.rotation = startRot;

        Vector3 startScale = currentDisplayObject.transform.lossyScale;

        SetLayerRecursively(currentDisplayObject, LayerMask.NameToLayer(displayLayer));
        DisablePhysics(currentDisplayObject);

        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        displayCoroutine = StartCoroutine(FlyToDisplayPosition(startPos, startRot, startScale));
    }

    /// <summary>
    /// ⭐ 让模型“视觉中心”对齐起点
    /// </summary>
    private void AlignVisualCenter(GameObject obj, Vector3 targetPos)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            obj.transform.position = targetPos;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        Vector3 offset = bounds.center - obj.transform.position;
        obj.transform.position = targetPos - offset;
    }

    /// <summary>
    /// ⭐ 带弧线的飞行动画
    /// </summary>
    private IEnumerator FlyToDisplayPosition(Vector3 startPos, Quaternion startRot, Vector3 startScale)
    {
        if (currentDisplayObject == null || targetCamera == null) yield break;

        Vector3 targetScale = startScale * displayScaleFactor;
        float elapsed = 0f;

        // ⭐ 根据距离自动计算弧线高度
        float distance = Vector3.Distance(startPos, GetDisplayPosition());
        float arcHeight = distance * arcHeightFactor;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = flyCurve.Evaluate(elapsed / flyDuration);

            Vector3 targetPos = GetDisplayPosition();
            Quaternion targetRot = GetDisplayRotation();

            // ⭐ 使用弧线位置
            currentDisplayObject.transform.position =
                GetArcPosition(startPos, targetPos, t, arcHeight);

            currentDisplayObject.transform.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            currentDisplayObject.transform.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        followCoroutine = StartCoroutine(FollowCamera());
        StartCoroutine(IdleRotation());
    }

    /// <summary>
    /// ⭐ 弧线轨迹核心算法
    /// </summary>
    private Vector3 GetArcPosition(Vector3 start, Vector3 end, float t, float height)
    {
        Vector3 linear = Vector3.Lerp(start, end, t);

        // 正弦弧线（中间最高）
        float arc = Mathf.Sin(t * Mathf.PI) * height;

        return linear + Vector3.up * arc;
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
            yield return null;
        }
    }

    private IEnumerator IdleRotation()
    {
        while (currentDisplayObject != null)
        {
            currentDisplayObject.transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
            yield return null;
        }
    }

    public void HideEvidence()
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        if (followCoroutine != null) StopCoroutine(followCoroutine);

        if (currentDisplayObject != null)
            Destroy(currentDisplayObject);
    }

    private Vector3 GetDisplayPosition()
    {
        return targetCamera.transform.position +
               targetCamera.transform.forward * forwardDistance +
               targetCamera.transform.up * upwardOffset +
               targetCamera.transform.right * sideOffset;
    }

    private Quaternion GetDisplayRotation()
    {
        Vector3 dir = targetCamera.transform.position - GetDisplayPosition();
        return dir != Vector3.zero ? Quaternion.LookRotation(-dir) : targetCamera.transform.rotation;
    }

    private void DisablePhysics(GameObject obj)
    {
        foreach (var col in obj.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}