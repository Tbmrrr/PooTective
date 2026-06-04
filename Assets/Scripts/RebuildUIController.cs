using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
[RequireComponent(typeof(CanvasGroup))]
public class RebuildUIController : MonoBehaviour
{
    [Header("监控设置")]
    [Tooltip("需要完成的 Quiz (EvidenceReceiver) 列表")]
    public List<EvidenceReceiver> requiredQuizzes = new List<EvidenceReceiver>();

    [Header("显现动画设置")]
    [Header("结局流程")]
    public CanvasGroup endingPanel;

    public float panelFadeDuration = 2f;

    public float panelStayDuration = 2f;

    public string nextSceneName = "Opening";
    [Tooltip("UI 显现的速度（秒）")]
    public float fadeDuration = 1.0f;

    private CanvasGroup canvasGroup;
    private bool isShowing = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // 确保初始是全透明
        if (canvasGroup != null && !isShowing)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (endingPanel != null)
        {
            endingPanel.alpha = 0f;
            endingPanel.interactable = false;
            endingPanel.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // 如果已经在显示了，就不再检测
        if (isShowing) return;

        if (CheckAllQuizzesSolved())
        {
            ShowUI();
        }
    }

    private bool CheckAllQuizzesSolved()
    {
        if (requiredQuizzes.Count == 0) return false;

        foreach (var quiz in requiredQuizzes)
        {
            // 只要有一个没解决，就返回 false
            if (quiz == null || !quiz.isSolved)
            {
                return false;
            }
        }
        return true;
    }

    private void ShowUI()
    {
        isShowing = true;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;
        float startAlpha = canvasGroup.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Debug.Log($"<color=green>[UI系统]</color> {gameObject.name} 已因 Quiz 全部完成而显现。");

        // 新增
        StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        if (endingPanel == null)
        {
            Debug.LogError("Ending Panel 未指定！");
            yield break;
        }

        // ===== 渐显 Panel =====

        float timer = 0f;

        while (timer < panelFadeDuration)
        {
            timer += Time.deltaTime;

            endingPanel.alpha =
                Mathf.Lerp(0f, 1f, timer / panelFadeDuration);

            yield return null;
        }

        endingPanel.alpha = 1f;

        // ===== 停留 =====

        yield return new WaitForSeconds(panelStayDuration);

        // ===== 切场景 =====

        SceneManager.LoadScene(nextSceneName);
    }
}