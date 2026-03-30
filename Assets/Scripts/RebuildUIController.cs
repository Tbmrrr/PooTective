using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class RebuildUIController : MonoBehaviour
{
    [Header("监控设置")]
    [Tooltip("需要完成的 Quiz (EvidenceReceiver) 列表")]
    public List<EvidenceReceiver> requiredQuizzes = new List<EvidenceReceiver>();

    [Header("显现动画设置")]
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
        // 显现后允许交互（如果它是按钮的话）
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Debug.Log($"<color=green>[UI系统]</color> {gameObject.name} 已因 Quiz 全部完成而显现。");
    }
}