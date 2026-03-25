using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))] // 确保物体上有相机
public class DogVisionEffect : MonoBehaviour
{
    public Material effectMaterial;
    [HideInInspector] public bool isRendering = false;

    private Camera myCamera;

    void OnEnable()
    {
        myCamera = GetComponent<Camera>();
        // --- 核心改动：告诉相机生成深度和法线纹理 ---
        if (myCamera != null)
        {
            myCamera.depthTextureMode |= DepthTextureMode.DepthNormals;
        }
    }

    void OnDisable()
    {
        // 关闭时还原模式
        if (myCamera != null)
        {
            myCamera.depthTextureMode &= ~DepthTextureMode.DepthNormals;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (isRendering && effectMaterial != null)
        {
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}