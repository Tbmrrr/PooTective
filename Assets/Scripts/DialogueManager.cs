using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI引用")]
    public GameObject dialogueBox;
    public Text nameText;
    public Text dialogueText;
    public GameObject continueIcon; // <--- 新增：拖入你的“继续”小图标

    [Header("打字机设置")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences = new Queue<string>();
    private string currentContent;
    private string lastSpeakerName = "";

    private bool isTyping = false;
    private bool cancelTyping = false;
    public bool isDialogueActive = false;

    private NPCInteractable activeNPC;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dialogueBox.SetActive(false);
        if (continueIcon != null) continueIcon.SetActive(false); // 初始隐藏图标
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                cancelTyping = true;
            }
            else
            {
                DisplayNextSentence();
            }
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
        // 只要准备显示下一句，就先隐藏图标
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
        }
        else
        {
            currentContent = rawLine.Trim();
        }

        nameText.text = lastSpeakerName;
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

        // --- 核心改动：文字显示完了，把图标变出来 ---
        if (continueIcon != null) continueIcon.SetActive(true);
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialogueBox.SetActive(false);
        if (continueIcon != null) continueIcon.SetActive(false); // 对话结束彻底隐藏

        if (activeNPC != null)
        {
            activeNPC.OnDialogueComplete();
            activeNPC = null;
        }
    }
}