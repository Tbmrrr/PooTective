using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class GoldenPathEffect : MonoBehaviour
{
    [Header("--- 路径平滑 ---")]
    public bool enableSmoothing = true;
    [Range(5, 30)] public int smoothPointsPerSegment = 15;

    [Header("--- 波动动画设置 ---")]
    public float baseHoverHeight = 0.3f;    // 基础悬浮高度
    public float floatAmplitude = 0.15f;   // 波动幅度
    public float floatSpeed = 3.0f;        // 波动频率（速度）
    [Range(0.01f, 1.0f)]
    public float waveDensity = 0.1f;       // 波动密度（值越大，波浪越密、扭动越剧烈）
    public float flowSpeed = 2.0f;         // 纹理流动速度

    [Header("--- 线条外观 (Built-in HDR) ---")]
    public Material lineMaterial;
    public float lineWidth = 0.25f;
    [ColorUsage(true, true)] public Color lineColor = new Color(2f, 1.5f, 0.5f);
    public float textureTiling = 1.0f;

    private LineRenderer lineRenderer;
    private Vector3[] smoothPoints;
    private Vector3[] animatedPoints;

    private int lastPointCount = -1;
    private bool isInitialized = false;
    private float scrollOffset;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineMaterial != null) lineRenderer.material = lineMaterial;

        // 删除了报错的 allowOcclusion 行
        // 确保 LineRenderer 的一些基本设置正确
        lineRenderer.useWorldSpace = true;
    }

    void OnEnable()
    {
        isInitialized = false;
        lastPointCount = -1;
    }

    void Update()
    {
        // 1. 检测路径点变化
        if (lineRenderer.positionCount >= 2 && lineRenderer.positionCount != lastPointCount)
        {
            RefreshCurve();
            isInitialized = true;
        }

        // 2. 执行波动和流动动画
        if (isInitialized && smoothPoints != null && smoothPoints.Length >= 2)
        {
            UpdateLineVisuals();
            UpdateWaveAnimation();
        }
    }

    public void RefreshCurve()
    {
        int rawCount = lineRenderer.positionCount;
        Vector3[] rawPoints = new Vector3[rawCount];
        lineRenderer.GetPositions(rawPoints);

        if (enableSmoothing && rawCount >= 2)
            smoothPoints = GenerateSmoothCurve(rawPoints, smoothPointsPerSegment);
        else
            smoothPoints = rawPoints;

        animatedPoints = new Vector3[smoothPoints.Length];

        lastPointCount = smoothPoints.Length;
        lineRenderer.positionCount = smoothPoints.Length;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    private void UpdateLineVisuals()
    {
        if (lineRenderer.material == null) return;

        scrollOffset += Time.deltaTime * flowSpeed;
        lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(-scrollOffset, 0));
        lineRenderer.material.SetTextureScale("_MainTex", new Vector2(textureTiling, 1));

        float pulse = 0.8f + Mathf.Sin(Time.time * 4f) * 0.2f;
        lineRenderer.startColor = lineColor * pulse;
        lineRenderer.endColor = lineColor * pulse;
    }

    private void UpdateWaveAnimation()
    {
        float time = Time.time * floatSpeed;

        for (int i = 0; i < smoothPoints.Length; i++)
        {
            // 【核心公式】：每个点根据自己的索引 i 获得不同的 Sin 相位
            // 这会产生类似海浪依次推移的效果
            float wave = Mathf.Sin(time + i * waveDensity);

            // 为了让线条两头（起点和终点）相对固定，不至于跳出起始位置
            // 我们可以在这里加一个权重（可选）
            float edgeWeight = 1.0f;
            if (i < 5) edgeWeight = i / 5f;
            else if (i > smoothPoints.Length - 6) edgeWeight = (smoothPoints.Length - 1 - i) / 5f;

            float yOffset = baseHoverHeight + (wave * floatAmplitude * edgeWeight);

            animatedPoints[i] = smoothPoints[i] + Vector3.up * yOffset;
        }

        lineRenderer.SetPositions(animatedPoints);
    }

    private Vector3[] GenerateSmoothCurve(Vector3[] raw, int segments)
    {
        if (raw.Length < 3) return raw;
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < raw.Length - 1; i++)
        {
            Vector3 p0 = (i == 0) ? raw[i] : raw[i - 1];
            Vector3 p1 = raw[i];
            Vector3 p2 = raw[i + 1];
            Vector3 p3 = (i == raw.Length - 2) ? raw[i + 1] : raw[i + 2];
            for (int j = 0; j < segments; j++)
            {
                float t = (float)j / segments;
                points.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        points.Add(raw[raw.Length - 1]);
        return points.ToArray();
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t; float t3 = t2 * t;
        return 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }
}