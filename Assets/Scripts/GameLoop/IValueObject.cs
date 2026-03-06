namespace MiniMapGame.GameLoop
{
    /// <summary>Placed at dead-end nodes. Collectible value item.</summary>
    public interface IValueObject
    {
        string ObjectId { get; }
        int Value { get; }
        void OnCollect();
    }
}
