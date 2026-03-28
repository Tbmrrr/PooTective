using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // 缓存主摄像机的 Transform，提高性能
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // 使用 LateUpdate 确保在摄像机移动后更新 UI 位置
    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        // 让 UI 的正前方与摄像机的正前方保持一致
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                         mainCameraTransform.rotation * Vector3.up);
    }
}