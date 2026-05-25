using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class pjnnewtest : MonoBehaviour
{
    [SerializeField] private Shader postProcessShader;

    private Material postProcessMaterial;

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void OnDisable()
    {
        if (postProcessMaterial != null)
        {
            DestroyImmediate(postProcessMaterial);
            postProcessMaterial = null;
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureMaterial();
    }

    private void EnsureMaterial()
    {
        if (postProcessShader == null)
        {
            return;
        }

        if (postProcessMaterial == null || postProcessMaterial.shader != postProcessShader)
        {
            if (postProcessMaterial != null)
            {
                DestroyImmediate(postProcessMaterial);
            }

            postProcessMaterial = new Material(postProcessShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (postProcessShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        EnsureMaterial();

        if (postProcessMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, postProcessMaterial);
    }
}
