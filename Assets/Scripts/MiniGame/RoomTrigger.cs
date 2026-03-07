using UnityEngine;
using MiniMapGame.Interior;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Attached to room floors. Detects player entry and starts the corresponding mini-game.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoomTrigger : MonoBehaviour
    {
        [HideInInspector] public MiniGameManager miniGameManager;
        [HideInInspector] public MiniGameType gameType;
        [HideInInspector] public int roomIndex;
        [HideInInspector] public string buildingId;
        [HideInInspector] public int seed;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (miniGameManager == null || miniGameManager.IsPlaying) return;
            if (!other.CompareTag("Player")) return;

            miniGameManager.StartMiniGame(new MiniGameContext
            {
                type = gameType,
                roomIndex = roomIndex,
                seed = seed,
                buildingId = buildingId
            });
        }

        /// <summary>
        /// Maps RoomType to MiniGameType. Returns null for rooms without mini-games.
        /// </summary>
        public static MiniGameType? GetGameType(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => MiniGameType.TimingCombat,
                RoomType.Treasure => MiniGameType.MemoryMatch,
                RoomType.Alcove => MiniGameType.TrapDodge,
                _ => null
            };
        }
    }
}
