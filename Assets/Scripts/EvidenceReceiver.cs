using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

public class EvidenceReceiver : MonoBehaviour
{
    [Header("链式解谜设置")]
    public EvidenceReceiver dependOnReceiver;
    public bool activateOnStart = false;

    [Header("状态")]
    public bool isActivated = false;
    public bool isSolved = false;

    [Header("配置")]
    public string requiredID;
    public bool hideTakenOutItemOnSuccess = true;
    public bool disableObjectOnSuccess = false;

    [Header("组件关联")]
    public TextMeshPro questionText;
    public GameObject objectToEnableOnSuccess;

    // ✅ 新增：Collider 引用
    private Collider myCollider;

    [Header("渐变设置")]
    public float fadeDuration = 1.0f;

    [Header("成功反馈")]
    public UnityEvent onSuccess;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 获取自身的碰撞体
        myCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (questionText != null)
        {
            Color c = questionText.color;
            c.a = 0;
            questionText.color = c;
            questionText.gameObject.SetActive(false);
        }

        // ✅ 初始检查：如果没激活，直接关掉碰撞体
        if (!activateOnStart)
        {
            UpdatePhysicsState(false);
        }
        else
        {
            ActivateQuestion();
        }
    }

    public void ActivateQuestion()
    {
        if (dependOnReceiver != null && !dependOnReceiver.isSolved) return;
        if (isActivated || isSolved) return;

        isActivated = true;

        // ✅ 启用问题时，同时开启物理检测
        UpdatePhysicsState(true);

        Debug.Log($"<color=cyan>[任务系统]</color> {gameObject.name} 已激活并开启射线检测。");

        if (questionText != null)
        {
            questionText.gameObject.SetActive(true);
            StartFade(1);
        }
    }

    public bool TryMatch(string incomingID)
    {
        if (!isActivated || isSolved) return false;

        if (incomingID == requiredID)
        {
            isSolved = true;
            isActivated = false;

            // ✅ 匹配成功后，立即关闭物理检测，防止二次触发
            UpdatePhysicsState(false);

            if (questionText != null)
            {
                StartFade(0, () => {
                    questionText.gameObject.SetActive(false);
                    if (disableObjectOnSuccess) gameObject.SetActive(false);
                });
            }
            else if (disableObjectOnSuccess)
            {
                gameObject.SetActive(false);
            }

            if (objectToEnableOnSuccess != null)
                objectToEnableOnSuccess.SetActive(true);

            onSuccess?.Invoke();
            if (hideTakenOutItemOnSuccess && TakenOutEvidenceUI.Instance != null)
                TakenOutEvidenceUI.Instance.Close();

            NotifyPotentialFollowers();
            return true;
        }
        return false;
    }

    // ✅ 新增辅助方法：控制碰撞体状态
    private void UpdatePhysicsState(bool canDetect)
    {
        if (myCollider != null)
        {
            myCollider.enabled = canDetect;
        }
    }

    private void NotifyPotentialFollowers()
    {
        EvidenceReceiver[] allReceivers = FindObjectsByType<EvidenceReceiver>(FindObjectsSortMode.None);
        foreach (var receiver in allReceivers)
        {
            if (receiver.dependOnReceiver == this) receiver.ActivateQuestion();
        }
    }

    // 渐变逻辑保持不变...
    private void StartFade(float targetAlpha, System.Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
    {
        float startAlpha = questionText.color.a;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (questionText != null)
            {
                Color c = questionText.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
                questionText.color = c;
            }
            yield return null;
        }
        onComplete?.Invoke();
    }
}