using UnityEngine;

public static class ResolutionConfig
{
    // 💡 该特性确保游戏一启动就自动执行此方法，无需挂载到任何 GameObject 上
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeResolution()
    {
        // 强制设置分辨率为 1600 * 900，并且指定为窗口模式 (Windowed)
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);

        Debug.Log("--- [系统] 游戏窗口已强制固定为 1600 * 900 窗口模式 ---");
    }
}