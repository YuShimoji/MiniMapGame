using UnityEngine;
using TMPro; // TextMeshPro を使用する場合に必要

public class InteractionPointController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [TextArea(3, 10)] // Inspectorで複数行入力できるようにする属性
    public string messageToShow = "ここにインタラクション時のメッセージを入力します。";

    // [Header("UI References")] // 対象フィールドがないためコメントアウト、または削除
    // このポイントがアクティブになった時にメッセージを表示する先のUIテキスト
    // Player側で一元管理するなら、これは不要になる可能性もある
    // public TextMeshProUGUI UIMessageTextElement; 

    // 必要に応じて、インタラクションの種類やその他のデータもここに追加できます
    // public enum InteractionType { Message, Item, Quest }
    // public InteractionType interactionType;
    // public int itemID; // interactionTypeがItemの場合のアイテムIDなど

    void Start()
    {
        // UI参照をここで探すか、Player側で設定するかは設計による
        // 今回はPlayer側でUIテキストを管理する前提で進めるため、ここでは何もしない
    }

    public string GetInteractionMessage()
    {
        return messageToShow;
    }

    // このスクリプトは、主にデータを保持する役割と、
    // Playerから呼び出されるメソッドを提供するのが目的です。
} 