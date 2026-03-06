using UnityEngine;
using UnityEngine.AI; // NavMeshAgent を使うために必要
using TMPro; // TextMeshProUGUI を使用するために必要

public class PlayerMovement : MonoBehaviour
{
    // mainCamera は Start() で Camera.main から取得する想定なので、CameraController からの直接参照は不要
    // private Camera mainCamera;
    private NavMeshAgent agent;

    [Header("Movement Speed Settings")]
    public float minSpeedAtMinZoomSize = 2f;  // カメラが最もズームインした時の速度
    public float maxSpeedAtMaxZoomSize = 8f;  // カメラが最もズームアウトした時の速度

    // カメラのズーム範囲の最小値と最大値（CameraControllerと値を合わせるか、ここで独立して設定）
    // CameraControllerの値を参照するのが理想だが、簡単のためPlayerMovementでも定義
    // もしCameraControllerのインスタンスを確実に取得できるなら、そちらを参照する方が良い
    [Header("Reference Zoom Levels (match CameraController)")]
    public float referenceMinZoomSize = 2f; 
    public float referenceMaxZoomSize = 10f;

    [Header("Interaction UI")] // Header属性を追加してインスペクターで見やすく
    public TextMeshProUGUI interactionMessageTextElement; // Inspectorから設定するUIテキスト

    private Collider currentInteractionPointCollider = null; // 現在接触しているインタラクションポイントのCollider

    void Start()
    {
        // mainCamera = Camera.main; // Camera.main はコストが高いので、可能なら一度だけ取得
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("PlayerMovement: NavMeshAgent component not found on this GameObject.");
            enabled = false;
            return;
        }
        if (Camera.main == null)
        {
            Debug.LogError("PlayerMovement: Main Camera not found in the scene.");
            enabled = false;
            return;
        }
        // interactionMessageTextElement のnullチェックと初期化
        if (interactionMessageTextElement == null)
        {
            Debug.LogWarning("PlayerMovement: InteractionMessageTextElement is not assigned in the Inspector. Messages will not be shown.");
        }
        else
        {
            interactionMessageTextElement.gameObject.SetActive(false); // 初期状態では非表示
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        // エージェントとカメラが存在する場合のみ処理
        if (agent == null || Camera.main == null) return;

        // 現在のカメラのOrthographic Sizeに基づいて速度を調整
        float currentOrthographicSize = Camera.main.orthographicSize;
        
        // ズーム割合を計算 (0: minZoomSize時, 1: maxZoomSize時)
        // Mathf.InverseLerp は、値が範囲内のどこにあるかを 0-1 の割合で返す
        float zoomRatio = Mathf.InverseLerp(referenceMinZoomSize, referenceMaxZoomSize, currentOrthographicSize);
        zoomRatio = Mathf.Clamp01(zoomRatio); // 念のため0-1の範囲に収める

        // 線形補間で速度を決定
        // Lerp(a, b, t) は t=0 のとき a, t=1 のとき b を返す
        // orthographicSize が小さい（ズームイン）ほど zoomRatio は 0 に近くなり、minSpeed になる
        // orthographicSize が大きい（ズームアウト）ほど zoomRatio は 1 に近くなり、maxSpeed になる
        agent.speed = Mathf.Lerp(minSpeedAtMinZoomSize, maxSpeedAtMaxZoomSize, zoomRatio);

        // --- 既存のクリック移動処理 ---
        if (Input.GetMouseButtonDown(0))
        {
            // Debug.Log("Mouse button down detected!"); // デバッグ用
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            int groundLayerMask = LayerMask.GetMask("Ground");
            // Debug.Log("GroundLayerMask value: " + groundLayerMask); // デバッグ用

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask))
            {
                // Debug.Log("Raycast hit ground at: " + hit.point); // デバッグ用
                if (agent.isOnNavMesh) // agentがnullでないことは上で確認済み
                {
                    // Debug.Log("Setting destination to: " + hit.point); // デバッグ用
                    agent.SetDestination(hit.point);
                }
                // else
                // {
                //     Debug.LogWarning("Agent is not on NavMesh, cannot set destination."); // デバッグ用
                // }
            }
            // else
            // {
            //     Debug.LogWarning("Raycast did not hit anything on the Ground layer."); // デバッグ用
            // }
        }

        // --- プレイヤーの向きを移動方向に合わせる ---
        if (agent.velocity.sqrMagnitude > 0.01f) // わずかでも動いていれば
        {
            Vector3 direction = agent.velocity.normalized;
            if (direction != Vector3.zero) 
            {
                // Y軸回転を計算 (atan2を使い、ワールドのZ軸を前方、X軸を右とする標準的な向きから角度を計算)
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                
                // プレイヤーのX回転は90度で固定し、Y軸周りのみtargetAngleで回転させる
                transform.rotation = Quaternion.Euler(90f, targetAngle, 0f);
            }
        }
    }

    // インタラクションポイントとの接触を検知するメソッド
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("InteractionPointTag")) 
        {
            // 同じポイントに既に接触している場合は何もしない (ズーム時の再トリガー対策にもなりうる)
            if (currentInteractionPointCollider == other) return;

            InteractionPointController pointController = other.gameObject.GetComponent<InteractionPointController>();
            if (pointController != null)
            {
                if (interactionMessageTextElement != null)
                {
                    interactionMessageTextElement.text = pointController.GetInteractionMessage();
                    interactionMessageTextElement.gameObject.SetActive(true);
                    currentInteractionPointCollider = other; // 現在のポイントを記憶
                }
                else
                {
                    Debug.LogWarning("PlayerMovement: InteractionMessageTextElement is not assigned. Cannot display message: " + pointController.GetInteractionMessage());
                }
            }
            else
            {
                Debug.LogWarning("InteractionPointTag を持つオブジェクトに InteractionPointController がアタッチされていません: " + other.gameObject.name);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // 記憶しているインタラクションポイントから離れた場合のみメッセージを消す
        if (other.gameObject.CompareTag("InteractionPointTag") && other == currentInteractionPointCollider)
        {
            if (interactionMessageTextElement != null)
            {
                interactionMessageTextElement.gameObject.SetActive(false);
                currentInteractionPointCollider = null; // 記憶をクリア
            }
        }
    }
} 