using UnityEngine;

namespace MiniMapGame.Data
{
    /// <summary>
    /// Metadata for a group of related hills forming a terrain feature.
    /// </summary>
    [System.Serializable]
    public struct HillCluster
    {
        public int id;
        public ClusterType type;
        public Vector2 center;
        public float orientationAngle;
        public SlopeProfile dominantProfile;
    }
}
