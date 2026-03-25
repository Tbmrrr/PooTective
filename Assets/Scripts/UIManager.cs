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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
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

        // 如果是第三人称视角，建议在这里重新锁定鼠标
        // Cursor.lockState = CursorLockMode.Locked; 
        // Cursor.visible = false;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        Debug.Log("场景重置成功！");
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