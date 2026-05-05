using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// --- 新增：名字与图片的映射结构体 ---
[System.Serializable]
public struct SpeakerImageMapping
{
    public string speakerName;   // 对应 TXT 里冒号前的名字
    public Sprite dialogueBoxSprite; // 对应的对话框底图
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI引用")]
    public GameObject dialogueBox;
    // 保留引用以防止其他脚本引用失败，但你可以不在场景里给它赋值，或者在代码里不操作它
    public Text nameText;
    public Text dialogueText;
    public GameObject continueIcon;

    // ✅ 新增：目标对话框 Image 组件
    [Tooltip("需要更换图片的对话框 Image 组件")]
    public Image dialogueBoxImage;

    [Header("对话框外观配置")]
    // ✅ 新增：在 Inspector 里配置名字和图片的对应关系
    public List<SpeakerImageMapping> speakerConfigs;
    public Sprite defaultBoxSprite; // 如果找不到名字对应的图片，使用的默认图

    [Header("打字机设置")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences = new Queue<string>();
    private string currentContent;
    private string lastSpeakerName = "";

    // 内部快速查询字典
    private Dictionary<string, Sprite> speakerDict = new Dictionary<string, Sprite>();

    public bool isTyping { get; private set; } // 建议保持属性访问
    private bool cancelTyping = false;
    public bool isDialogueActive = false;

    private NPCInteractable activeNPC;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dialogueBox.SetActive(false);
        if (continueIcon != null) continueIcon.SetActive(false);

        // ✅ 初始化字典以便快速查找
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
        if (isDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping) cancelTyping = true;
            else DisplayNextSentence();
        }
    }

    public void StartDialogue(string[] lines, NPCInteractable npc)
    {
        activeNPC = npc;
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

            // ✅ 核心改动：不再更新 nameText，而是更新对话框图片
            UpdateDialogueBoxVisual(lastSpeakerName);
        }
        else
        {
            currentContent = rawLine.Trim();
            // 如果没有名字，可以保持当前图片或切回默认
        }

        // 如果其他脚本访问了 nameText.text，为了兼容性我们依然可以赋值，
        // 如果你彻底不需要显示名字，可以直接在场景里把 NameText 所在的 GameObject 隐藏
        if (nameText != null) nameText.text = lastSpeakerName;
    }

    // ✅ 新增：更换图片的逻辑
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

        if (activeNPC != null)
        {
            activeNPC.OnDialogueComplete();
            activeNPC = null;
        }
    }
}