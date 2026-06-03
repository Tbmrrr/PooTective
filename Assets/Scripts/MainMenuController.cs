using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("游戏关卡场景的名称")]
    public string gameSceneName = "GameScene";

    [Header("Loading UI 引用")]
    public GameObject loadingPanel;    // Loading 面板
    public Text progressText;          // 💡 只保留文本组件

    private bool isLoadingFinished = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // 🟢 点击“开始游戏”
    public void ClickStartGame()
    {
        StartCoroutine(LoadSceneAsync());
        StartCoroutine(AnimateLoadingText()); // 💡 同时启动文本动画协程
    }

    // 🔄 异步加载场景协程
    IEnumerator LoadSceneAsync()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        isLoadingFinished = false;

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        operation.allowSceneActivation = false; // 依然先拉住刹车

        // 1. 等待底层数据加载到 90%
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 2. 此时已经加载完，让 Loading... 动画停止
        isLoadingFinished = true;
        if (progressText != null) progressText.text = "加载完毕，正在唤醒场景...";

        // 💡 核心改动：不再检测 Input.anyKeyDown，而是强制让画面在 Loading 界面停留 0.5 秒
        // 这 0.5 秒可以让后台安稳地处理一部分垃圾回收（GC）
        yield return new WaitForSeconds(0.5f);

        // 3. 放开刹车，允许切换场景
        operation.allowSceneActivation = true;

        // 4. 关键：等待新场景完全激活、Awake和Start全部跑完
        while (!operation.isDone)
        {
            yield return null;
        }

        // 5. 彻底成功进入新场景，关闭面板
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // ✍️ 让 "Loading..." 动起来的辅助协程
    IEnumerator AnimateLoadingText()
    {
        while (!isLoadingFinished)
        {
            if (progressText != null) progressText.text = "Loading";
            yield return new WaitForSeconds(0.4f);
            if (isLoadingFinished) break;

            if (progressText != null) progressText.text = "Loading.";
            yield return new WaitForSeconds(0.4f);
            if (isLoadingFinished) break;

            if (progressText != null) progressText.text = "Loading..";
            yield return new WaitForSeconds(0.4f);
            if (isLoadingFinished) break;

            if (progressText != null) progressText.text = "Loading...";
            yield return new WaitForSeconds(0.4f);
        }
    }

    // 🔴 点击“退出游戏”
    public void ClickQuitGame()
    {
        Debug.Log("玩家点击了退出游戏！");
        Application.Quit();
    }
}