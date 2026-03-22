using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 场景管理必备

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 面板引用")]
    public GameObject settingsPanel; // 拖入 Settings_Panel

    [Header("状态")]
    private bool isSettingsOpen = false;

    private void Awake()
    {
        // 单例模式，方便全局调用
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 确保启动时设置面板是关闭的
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        // 监听 Esc 键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    // 切换设置面板显示/隐藏
    public void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;
        settingsPanel.SetActive(isSettingsOpen);

        // 优化手感：打开设置时暂停游戏，关闭时恢复
        if (isSettingsOpen)
        {
            Time.timeScale = 0f; // 时间静止
            Cursor.lockState = CursorLockMode.None; // 释放鼠标
            Cursor.visible = true;
        }
        else
        {
            ResumeGame();
        }
    }

    // 恢复游戏函数
    public void ResumeGame()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f; // 恢复正常时间流速
        
        // 如果你的游戏是第一人称/第三人称旋转，这里可能需要重新锁定鼠标
        // Cursor.lockState = CursorLockMode.Locked; 
    }

    // --- 重启场景功能 ---
    public void RestartScene()
    {
        // 极其重要：在重启前必须把时间流速恢复为 1，否则新场景也是静止的
        Time.timeScale = 1f;
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        
        Debug.Log("场景重置成功！");
    }
}