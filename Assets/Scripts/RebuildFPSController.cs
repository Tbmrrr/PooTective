using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RebuildFPSController : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("如果是单物体结构，这里可以拖入自己，或者留空")]
    public Transform playerCamera;
    private CharacterController controller;

    [Header("移动设置")]
    public float moveSpeed = 6.0f;

    [Header("相机旋转设置")]
    public float mouseSensitivity = 3.0f;
    [Tooltip("最大仰角/俯角")]
    public float pitchLimit = 80f;

    [Header("交互设置 (第一人称射线)")]
    public float interactDistance = 5.0f;
    [Tooltip("在 Inspector 中勾选 Evidence 和 Door 所在的层")]
    public LayerMask interactableLayer;

    // 内部旋转累加器
    private float yaw;      // 水平
    private float pitch;    // 纵向
    private bool isDialogueLock = false;

    // 记录当前指向的对象
    private Evidence currentEvidence;
    private DoorInteractable currentDoor;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 单物体结构下，直接引用自身
        if (playerCamera == null) playerCamera = transform;

        // 初始化角度，防止启动时视角瞬间跳回 0
        Vector3 currentRot = transform.eulerAngles;
        yaw = currentRot.y;
        // 处理初始 Pitch（如果是单物体，通常初始是 0）
        pitch = (currentRot.x > 180) ? currentRot.x - 360 : currentRot.x;
    }

    void Update()
    {
        // 1. 状态锁判断
        bool isNoteOpen = NoteManager.Instance != null && NoteManager.Instance.notePanel.activeSelf;
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;

        // 如果处于对话或看笔记状态，解锁鼠标并停止视角操作
        if (isDialogueActive || isNoteOpen)
        {
            isDialogueLock = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ClearAllPrompts();
            return;
        }

        isDialogueLock = false;

        // 2. 核心逻辑调用
        HandleCamera();
        HandleMovement();
        HandleRaycastDetection();

        // 3. 交互按键
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecuteInteraction();
        }
    }

    void HandleCamera()
    {
        if (isDialogueLock) return;

        // ✅ 直接锁定鼠标并隐藏，不再需要判断 Input.GetMouseButton(1)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 累加鼠标输入
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 限制纵向角度，防止“翻脖子”
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        // 单物体结构的核心：一次性应用欧拉角
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void HandleMovement()
    {
        if (isDialogueLock) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // 无论头（pitch）抬多高，移动只参考水平面（yaw）的方向
            Vector3 moveDir = Quaternion.Euler(0, yaw, 0) * inputDir;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }
    }

    void HandleRaycastDetection()
    {
        // 从屏幕中心发射射线
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 调试用黄线
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow);

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 优先检测证物
            Evidence evidence = hitObj.GetComponentInParent<Evidence>();
            if (evidence != null)
            {
                if (currentEvidence != evidence) ClearAllPrompts();
                currentEvidence = evidence;
                currentEvidence.ShowPrompt(true);
                return;
            }

            // 其次检测门
            DoorInteractable door = hitObj.GetComponentInParent<DoorInteractable>();
            if (door != null)
            {
                if (currentDoor != door) ClearAllPrompts();
                currentDoor = door;
                currentDoor.ShowPrompt(true);
                return;
            }
        }

        ClearAllPrompts();
    }

    void ExecuteInteraction()
    {
        if (currentEvidence != null) currentEvidence.OnInteract();
        else if (currentDoor != null) currentDoor.OnInteract();
    }

    void ClearAllPrompts()
    {
        if (currentEvidence != null) currentEvidence.ShowPrompt(false);
        if (currentDoor != null) currentDoor.ShowPrompt(false);
        currentEvidence = null;
        currentDoor = null;
    }
}