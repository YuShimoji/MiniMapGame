using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(WorldPositionTrackerUI))]
public class LabelController : MonoBehaviour
{
    [Header("Display Range (Zoom Level)")]
    [Tooltip("このラベルが表示され始めるZoomレベル (これより大きいと非表示)")]
    public float displayRangeMax = 10f;
    [Tooltip("このラベルが完全に消えるZoomレベル (これより小さいと非表示)")]
    public float displayRangeMin = 2f;

    [Header("Full Clarity Range (Fade)")]
    [Tooltip("このZoomレベルより小さいと完全に不透明 (アルファ=1)")]
    public float fullClarityRangeMax = 8f;
    [Tooltip("このZoomレベルより大きいと完全に不透明 (アルファ=1)")]
    public float fullClarityRangeMin = 4f;

    [Header("Font Size Range")]
    [Tooltip("Zoomレベルが最小(displayRangeMin)の時のフォントサイズ")]
    public float minFontSize = 12f;
    [Tooltip("Zoomレベルが最大(displayRangeMax)の時のフォントサイズ")]
    public float maxFontSize = 24f;

    private TextMeshProUGUI myText;
    private Camera mainCamera;

    void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
        mainCamera = Camera.main;

        // 設定値のバリデーション
        if (displayRangeMin >= displayRangeMax || fullClarityRangeMin >= fullClarityRangeMax)
        {
            Debug.LogWarning("LabelController: Range settings are invalid. Min value must be less than Max value.", this.gameObject);
            enabled = false;
        }
    }

    void Update()
    {
        float currentZoom = mainCamera.orthographicSize;

        // 1. 表示範囲外なら非表示にして終了
        if (currentZoom < displayRangeMin || currentZoom > displayRangeMax)
        {
            myText.enabled = false;
            return;
        }

        myText.enabled = true;

        // 2. 透明度(アルファ)を計算
        float alpha = 1f;
        if (currentZoom > fullClarityRangeMax) // フェードアウト (遠ざかる方向)
        {
            alpha = Mathf.InverseLerp(displayRangeMax, fullClarityRangeMax, currentZoom);
        }
        else if (currentZoom < fullClarityRangeMin) // フェードアウト (近づく方向)
        {
            alpha = Mathf.InverseLerp(displayRangeMin, fullClarityRangeMin, currentZoom);
        }
        myText.color = new Color(myText.color.r, myText.color.g, myText.color.b, Mathf.Clamp01(alpha));


        // 3. フォントサイズを計算
        // 現在のZoomレベルが表示範囲全体のどの割合にあるかを計算
        float sizeRatio = Mathf.InverseLerp(displayRangeMin, displayRangeMax, currentZoom);
        float fontSize = Mathf.Lerp(minFontSize, maxFontSize, sizeRatio);
        myText.fontSize = fontSize;
    }
} 