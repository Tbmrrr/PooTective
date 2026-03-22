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

    [Header("打字机设置")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences = new Queue<string>();
    private string currentContent;
    private string lastSpeakerName = "";

    private bool isTyping = false;
    private bool cancelTyping = false;
    public bool isDialogueActive = false;

    private NPCInteractable activeNPC; // 记录当前正在对话的NPC

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        dialogueBox.SetActive(false);
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

    // 增加了参数接收当前的NPC
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
        // 修复问题2：如果队列空了，直接结束
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
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialogueBox.SetActive(false);

        // 告诉NPC：对话彻底结束了，你可以把自己“禁用”了
        if (activeNPC != null)
        {
            activeNPC.OnDialogueComplete();
            activeNPC = null;
        }
    }
}