using UnityEngine;

[ExecuteInEditMode]
public class DreamyCameraEffect : MonoBehaviour
{
    public Material effectMaterial;

    // 在相机渲染完成后应用效果
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null)
        {
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}