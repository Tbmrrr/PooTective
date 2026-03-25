using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RebuildFPSController : MonoBehaviour
{
    [Header("组件引用")]
    public Transform playerCamera; // 拖入该角色下的 Camera 子物体
    private CharacterController controller;

    [Header("移动设置")]
    public float moveSpeed = 6.0f;
    public float rotationSpeed = 10.0f;

    [Header("相机旋转设置")]
    public float mouseSensitivity = 3.0f;

    // 内部记录变量
    private float yaw;
    private float fixedPitch; // 保持你主角脚本中的固定俯仰角特性

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 自动获取子物体相机
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>().transform;

        // 记录启动时的初始角度（复刻主角逻辑）
        yaw = transform.eulerAngles.y;
        fixedPitch = playerCamera.eulerAngles.x;
    }

    void Update()
    {
        HandleCamera();
        HandleMovement();
    }

    void HandleCamera()
    {
        // 复刻主角逻辑：只有按下右键才允许左右转
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 第一人称逻辑：旋转由 yaw 决定
        playerCamera.rotation = Quaternion.Euler(fixedPitch, yaw, 0);

        // 角色物体的旋转也同步更新，确保移动方向正确
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        Vector3 moveDir = Vector3.zero;

        if (inputDir.magnitude >= 0.1f)
        {
            // 复刻主角逻辑：移动方向参考当前的 yaw
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + yaw;
            moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }

        // --- 已移除重力逻辑 ---
        // 直接根据输入的方向和速度进行位移
        Vector3 finalMove = moveDir * moveSpeed;

        controller.Move(finalMove * Time.deltaTime);
    }
}