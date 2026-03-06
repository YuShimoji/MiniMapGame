using UnityEngine;

public class CameraController : MonoBehaviour
{
    private enum CameraState { Following, ManualOrbit, ManualPan }

    [Header("Focus Settings")]
    public Transform playerTarget;
    public Vector3 focusOffset = new Vector3(0, 1.0f, 0); // プレイヤーのどの位置を注視点とするかのオフセット

    [Header("Camera Control")]
    [Tooltip("パン操作を離してから自動追従に戻るまでの時間")]
    public float panReleaseReturnTime = 1.0f;

    [Header("Following Behavior")]
    [Tooltip("プレイヤーを画面のどの位置に維持するか（0.5が中央）")]
    public Vector2 idealScreenPosition = new Vector2(0.5f, 0.4f);
    [Tooltip("プレイヤーが画面内のこの範囲を超えたら、カメラが追従を開始")]
    public Vector2 screenMargin = new Vector2(0.1f, 0.1f);
    public float followSmoothTime = 0.5f;

    [Header("Orbit (Rotation & Pitch)")]
    public Vector2 pitchMinMax = new Vector2(10, 85); // 傾斜角(X軸回転)の最小・最大
    [Tooltip("マウスの移動量に対するカメラの回転速度")]
    public float rotationSpeed = 3f;
    [Tooltip("プレイヤー追従時の回転の滑らかさ（小さいほど速く追従）")]
    public float followRotationSmoothTime = 0.3f;

    [Header("Zoom (Distance)")]
    public float initialDistance = 15f; // ゲーム開始時の、注視点からの距離
    public float zoomSpeed = 10f;
    public Vector2 distanceMinMax = new Vector2(5, 50); // 注視点からの距離の最小・最大
    public float zoomSmoothTime = 0.2f;
    
    [Header("Pan")]
    [Tooltip("パン操作の速度。カメラからの距離に応じて自動調整されます")]
    public float panSpeed = 1f;

    private Camera mainCamera;
    private CameraState currentState;
    private float lastPanTime;

    private Vector3 focusPoint;
    private Vector3 focusPointVelocity;

    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity;
    
    private Vector2 targetOrbitAngles; // Pitch (x), Yaw (y)
    private Vector2 currentOrbitAngles;
    private Vector2 orbitVelocity;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null || mainCamera.orthographic)
        {
            Debug.LogError("CameraController requires a Perspective Camera.");
            enabled = false;
            return;
        }

        currentState = CameraState.Following;
        lastPanTime = -panReleaseReturnTime;

        targetDistance = currentDistance = initialDistance;
        
        // Start with a default angle behind the player
        if (playerTarget != null)
        {
            focusPoint = playerTarget.position + focusOffset;
            targetOrbitAngles = new Vector2(45f, playerTarget.eulerAngles.y);
        }
        else
        {
            focusPoint = Vector3.zero;
            targetOrbitAngles = new Vector2(45f, 0f);
        }
        currentOrbitAngles = targetOrbitAngles;
        
        ApplyCameraTransform();
    }

    void LateUpdate()
    {
        UpdateState();
        UpdateFocusPoint();
        UpdateCameraOrbitAndDistance();
        ApplyCameraTransform();
    }
    
    private void UpdateState()
    {
        bool orbitInput = Input.GetMouseButton(1);
        bool panInput = Input.GetMouseButton(2);
        bool zoomInput = Input.GetAxis("Mouse ScrollWheel") != 0;

        if (panInput)
        {
            currentState = CameraState.ManualPan;
            lastPanTime = Time.time;
        }
        else if (orbitInput)
        {
            currentState = CameraState.ManualOrbit;
        }
        else if (currentState == CameraState.ManualPan)
        {
            // パン操作後は一定時間待ってからFollowingに戻る
            if (Time.time - lastPanTime > panReleaseReturnTime)
            {
                currentState = CameraState.Following;
            }
        }
        else if (!zoomInput)
        {
            // ズーム以外の操作がない場合は即座にFollowingに戻る
            currentState = CameraState.Following;
        }
    }
    
    private void UpdateFocusPoint()
    {
        if (currentState == CameraState.ManualPan)
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            // パン操作はカメラの水平な向き基準で移動
            Quaternion yawRotation = Quaternion.Euler(0, currentOrbitAngles.y, 0);
            Vector3 panMovement = yawRotation * new Vector3(-mouseDelta.x, 0, -mouseDelta.y);
            // カメラが遠いほど速くパンするように調整
            focusPoint += panMovement * panSpeed * currentDistance * 0.1f;
        }
        else if (playerTarget != null)
        {
            Vector3 targetFocusPoint = playerTarget.position + focusOffset;
            if (currentState == CameraState.Following)
            {
                float dist = Vector3.Distance(focusPoint, targetFocusPoint);
                // プレイヤーから3ユニット以上離れたら、追従速度を4倍にする
                float currentSmoothTime = dist > 3f ? followSmoothTime / 4f : followSmoothTime;
                focusPoint = Vector3.SmoothDamp(focusPoint, targetFocusPoint, ref focusPointVelocity, currentSmoothTime);
            }
            else // ManualOrbit
            {
                // 手動回転中は、即座にプレイヤーを注視する
                focusPoint = targetFocusPoint;
            }
        }
    }

    private Vector2 GetScreenPosition(Vector3 worldPosition)
    {
        Vector3 screenPos = mainCamera.WorldToViewportPoint(worldPosition);
        return new Vector2(screenPos.x, screenPos.y);
    }

    private void UpdateCameraOrbitAndDistance()
    {
        // --- Zoom Input (applies to all states) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, distanceMinMax.x, distanceMinMax.y);
        }
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        // --- Rotation Logic ---
        if (currentState == CameraState.ManualOrbit)
        {
            // 手動回転はスムージングなしで即座に反映
            currentOrbitAngles.y += Input.GetAxis("Mouse X") * rotationSpeed;
            currentOrbitAngles.x -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentOrbitAngles.x = Mathf.Clamp(currentOrbitAngles.x, pitchMinMax.x, pitchMinMax.y);
            
            // 追従モードに戻った時に備え、目標角度を更新しておく
            targetOrbitAngles = currentOrbitAngles;
        }
        else if (currentState == CameraState.Following && playerTarget != null)
        {
            // 追従モード時は、プレイヤーの背後を目標角度とする
            targetOrbitAngles.y = playerTarget.eulerAngles.y;
            // 角度のスムージング（Pitchは現在の値を維持し、Yawのみ目標に追従）
            currentOrbitAngles = Vector2.SmoothDamp(currentOrbitAngles, new Vector2(currentOrbitAngles.x, targetOrbitAngles.y), ref orbitVelocity, followRotationSmoothTime);
        }
    }

    void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(currentOrbitAngles.x, currentOrbitAngles.y, 0);
        Vector3 position = focusPoint - (rotation * Vector3.forward * currentDistance);
        transform.position = position;
        transform.rotation = rotation;
    }
} 