using System.Collections.Generic;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public class MapAnalysis
    {
        public List<int> deadEndIndices = new();
        public List<int> intersectionIndices = new();
        public List<int> plazaIndices = new();
        public List<int> chokeEdgeIndices = new();
    }
}
