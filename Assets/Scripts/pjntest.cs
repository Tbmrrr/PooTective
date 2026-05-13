using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class pjntest : MonoBehaviour
{
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0.01f, 6f)] private float outlineThickness = 1f;

    private Material outlineMaterial;
    private Camera targetCamera;

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        EnsureMaterial();
    }

    private void OnDisable()
    {
        if (outlineMaterial != null)
        {
            DestroyImmediate(outlineMaterial);
            outlineMaterial = null;
        }
    }

    private void EnsureMaterial()
    {
        if (outlineShader == null)
        {
            return;
        }

        if (outlineMaterial == null || outlineMaterial.shader != outlineShader)
        {
            if (outlineMaterial != null)
            {
                DestroyImmediate(outlineMaterial);
            }

            outlineMaterial = new Material(outlineShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        targetCamera = GetComponent<Camera>();
        targetCamera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        EnsureMaterial();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (outlineShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        EnsureMaterial();

        if (outlineMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineThickness", outlineThickness);
        Graphics.Blit(source, destination, outlineMaterial);
    }
}
