using UnityEngine;
using UnityEngine.UI;

public class ImageTracker : MonoBehaviour
{
    private Image myImage;
    private bool lastState;

    void Start()
    {
        myImage = GetComponent<Image>();
        if (myImage != null)
        {
            lastState = myImage.enabled;
        }
    }

    void Update()
    {
        if (myImage == null) return;

        // 只要发现 Image 的勾选状态被改变了
        if (myImage.enabled != lastState)
        {
            lastState = myImage.enabled;

            // 如果是被关掉了，立马打印是谁干的
            if (!lastState)
            {
                Debug.LogError($"【抓到贼了！】{gameObject.name} 的 Image 组件被关闭了！关闭时的调用栈如下：", this);
            }
        }
    }
}