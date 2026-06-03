using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct SpeakerImageMapping
{
    public string speakerName;
    public Sprite dialogueBoxSprite;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI引用")]
    public GameObject dialogueBox;
    public Text nameText;
    public Text dialogueText;
    public GameObject continueIcon;

    [Tooltip("需要更换图片的对话框 Image 组件")]
    public Image dialogueBoxImage;

    [Header("对话框外观配置")]
    public List<SpeakerImageMapping> speakerConfigs;
    public Sprite defaultBoxSprite;

    [Header("打字机设置")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences = new Queue<string>();
    private string currentContent;
    private string lastSpeakerName = "";

    private Dictionary<string, Sprite> speakerDict = new Dictionary<string, Sprite>();

    public bool isTyping { get; private set; }
    private bool cancelTyping = false;
    public bool isDialogueActive = false;

    // ✅ 从 NPCInteractable 改为 MonoBehaviour，任何脚本都可以作为回调对象
    private MonoBehaviour activeCaller;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dialogueBox.SetActive(false);
        if (continueIcon != null) continueIcon.SetActive(false);

        speakerDict.Clear();
        foreach (var config in speakerConfigs)
        {
            if (!string.IsNullOrEmpty(config.speakerName) && !speakerDict.ContainsKey(config.speakerName))
            {
                speakerDict.Add(config.speakerName, config.dialogueBoxSprite);
            }
        }
    }

    void Update()
    {
        // 💡 修改：使用 || (或运算符) 同时监听 E 键和 鼠标左键 (KeyCode.Mouse0)
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Mouse0)))
        {
            if (isTyping)
            {
                // 如果正在打字，点击后立即停止打字，显示完整文本
                cancelTyping = true;
            }
            else
            {
                // 如果字已经打完了，点击后进入下一句
                DisplayNextSentence();
            }
        }
    }

    // ✅ 参数类型从 NPCInteractable 改为 MonoBehaviour
    public void StartDialogue(string[] lines, MonoBehaviour caller)
    {
        activeCaller = caller;
        isDialogueActive = true;
        dialogueBox.SetActive(true);
        lastSpeakerName = "";

        sentences.Clear();
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line)) sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (continueIcon != null) continueIcon.SetActive(false);

        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string rawLine = sentences.Dequeue();
        ParseLine(rawLine);

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentContent));
    }

    private void ParseLine(string rawLine)
    {
        int colonIndex = rawLine.IndexOf('：');
        if (colonIndex == -1) colonIndex = rawLine.IndexOf(':');

        if (colonIndex != -1)
        {
            lastSpeakerName = rawLine.Substring(0, colonIndex).Trim();
            currentContent = rawLine.Substring(colonIndex + 1).Trim();
            UpdateDialogueBoxVisual(lastSpeakerName);
        }
        else
        {
            currentContent = rawLine.Trim();
        }

        if (nameText != null) nameText.text = lastSpeakerName;
    }

    private void UpdateDialogueBoxVisual(string speakerName)
    {
        if (dialogueBoxImage == null) return;

        if (speakerDict.ContainsKey(speakerName))
        {
            dialogueBoxImage.sprite = speakerDict[speakerName];
        }
        else if (defaultBoxSprite != null)
        {
            dialogueBoxImage.sprite = defaultBoxSprite;
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        isTyping = true;
        cancelTyping = false;

        foreach (char letter in sentence.ToCharArray())
        {
            if (cancelTyping)
            {
                dialogueText.text = sentence;
                break;
            }
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        cancelTyping = false;
        if (continueIcon != null) continueIcon.SetActive(true);
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialogueBox.SetActive(false);
        if (continueIcon != null) continueIcon.SetActive(false);

        Debug.Log($"EndDialogue called | activeCaller={activeCaller}");

        if (activeCaller != null)
        {
            activeCaller.SendMessage("OnDialogueComplete", SendMessageOptions.DontRequireReceiver);
            activeCaller = null;
        }
    }
}