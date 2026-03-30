using UnityEngine;
using TMPro; // 必须引用此命名空间

public class TextWavyEffect : MonoBehaviour
{
    private TMP_Text textComponent;

    [Header("波动参数设置")]
    [Tooltip("波动的速度")]
    public float speedMultiplier = 2.0f;

    [Tooltip("波形的密集程度（每个字之间的相位差）")]
    public float angleMultiplier = 0.5f;

    [Tooltip("波动的幅度（跳动的高度）")]
    public float waveHeight = 0.5f;

    void Awake()
    {
        // 获取 TextMeshPro 组件（不论是 3D 版本还是 UI 版本都通用）
        textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        // 1. 必须强制更新网格，否则无法实时获取最新的顶点坐标
        textComponent.ForceMeshUpdate();

        // 2. 获取当前的文字信息快照
        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        // 3. 遍历每一个字符
        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // 如果字符不可见（比如空格、换行符），则跳过，避免报错
            if (!charInfo.isVisible) continue;

            // 获取该字符对应的材质索引和顶点索引（一个字由4个顶点组成）
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // 获取该字符原始的顶点坐标数组
            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

            // 计算该字符的 Y 轴偏移量
            // 使用 Mathf.Sin 产生正弦波。 i * angleMultiplier 是让每个字错开跳动的核心
            float offset = Mathf.Sin(Time.time * speedMultiplier + i * angleMultiplier) * waveHeight;

            // 将偏移应用到该字符的全部 4 个顶点上
            // 顶点顺序通常是：0(左下), 1(左上), 2(右上), 3(右下)
            sourceVertices[vertexIndex + 0].y += offset;
            sourceVertices[vertexIndex + 1].y += offset;
            sourceVertices[vertexIndex + 2].y += offset;
            sourceVertices[vertexIndex + 3].y += offset;
        }

        // 4. 将修改后的顶点数据应用回网格
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            textComponent.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}