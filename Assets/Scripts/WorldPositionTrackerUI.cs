using UnityEngine;

public class WorldPositionTrackerUI : MonoBehaviour
{
    [Header("Tracking Target")]
    public Transform targetTransform; // このUIが追従するワールド空間のTransform

    [Header("Position Offset")]
    public Vector3 worldOffset = new Vector3(0, 1.5f, 0); // ターゲットの頭上など、少しずらしたい場合の位置オフセット

    private RectTransform myRectTransform;
    private Camera mainCamera;

    void Start()
    {
        myRectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (targetTransform == null)
        {
            Debug.LogWarning("WorldPositionTrackerUI: targetTransform is not assigned. Disabling component.", this.gameObject);
            this.enabled = false;
            return;
        }
    }

    // カメラの移動が完了した後にUIの位置を更新するため、LateUpdateを使用
    void LateUpdate()
    {
        if (!this.enabled || targetTransform == null)
        {
            return;
        }

        // ターゲットのワールド座標にオフセットを加算
        Vector3 targetWorldPosition = targetTransform.position + worldOffset;

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetWorldPosition);

        // UI要素の位置を更新
        myRectTransform.position = screenPosition;
    }
} 