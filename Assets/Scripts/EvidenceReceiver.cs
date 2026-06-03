using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // 💡 引入 UI 命名空间以使用 Image
using System.Collections;

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
    // 🔄 已替换：将 TextMeshPro 替换为 Image 组件
    public Image questionImage;
    public GameObject objectToEnableOnSuccess;

    // ✅ Collider 引用
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
        // 🔄 已替换：初始化时将图片透明度设为 0 并隐藏
        if (questionImage != null)
        {
            Color c = questionImage.color;
            c.a = 0;
            questionImage.color = c;
            questionImage.gameObject.SetActive(false);
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

        // 🔄 已替换：显示并渐显图片
        if (questionImage != null)
        {
            questionImage.gameObject.SetActive(true);
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

            // 🔄 已替换：解谜成功后，渐隐并关闭图片
            if (questionImage != null)
            {
                StartFade(0, () => {
                    questionImage.gameObject.SetActive(false);
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

    // ✅ 控制碰撞体状态
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

    // 渐变逻辑调配
    private void StartFade(float targetAlpha, System.Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    // 🔄 已替换：对 Image 的颜色 Alpha 通道进行 Lerp 渐变
    IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
    {
        float startAlpha = questionImage.color.a;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (questionImage != null)
            {
                Color c = questionImage.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
                questionImage.color = c;
            }
            yield return null;
        }
        onComplete?.Invoke();
    }
}