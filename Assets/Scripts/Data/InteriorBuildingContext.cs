using UnityEngine;

namespace MiniMapGame.Data
{
    /// <summary>
    /// Bridges exterior MapBuilding data to interior generation.
    /// Computed once per building by BuildingClassifier.
    /// </summary>
    [System.Serializable]
    public struct InteriorBuildingContext
    {
        // From MapBuilding
        public string buildingId;
        public float footprintWidth;
        public float footprintHeight;
        public float angle;
        public int tier;            // 0=arterial, 1=secondary, 2=tertiary
        public bool isLandmark;
        public int floors;
        public int shapeType;       // 0=box, 1=L-shape, 2=cylinder, 3=stepped

        // Classified by BuildingClassifier
        public BuildingCategory category;
        public ShopSubtype shopSubtype;

        // Environmental context
        public float elevation;
        public bool nearCoast;
        public bool nearRiver;
        public bool nearHill;
        public GeneratorType mapType;
    }
}
