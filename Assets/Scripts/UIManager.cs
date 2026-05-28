using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 面板引用")]
    public GameObject settingsPanel;

    [Header("状态")]
    private bool isSettingsOpen = false;

    // 💡 【新增】公开的只读属性，供其他脚本（如视角脚本）判断面板是否打开
    public bool IsSettingsOpen => isSettingsOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;
        settingsPanel.SetActive(isSettingsOpen);

        if (isSettingsOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;

        // 💡 【建议取消注释】关闭设置时，把鼠标重新锁回游戏中央并隐藏
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;

        // ✅ 新增：在重启场景前，清空所有的交互和质询记录
        if (InteractionHistoryManager.Instance != null)
        {
            InteractionHistoryManager.Instance.ClearHistory();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        Debug.Log("场景与交互历史重置成功！");
    }

    // --- 新增：退出游戏功能 ---
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");

        // 如果是在 Unity 编辑器里运行，则停止运行
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // 如果是打包后的程序，则关闭程序
        Application.Quit();
#endif
    }
}