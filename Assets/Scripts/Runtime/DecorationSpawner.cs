using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;
using MiniMapGame.Player;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Instantiates decoration objects from MapDecoration data.
    /// Supports LOD-based visibility toggling via camera distance.
    /// </summary>
    public class DecorationSpawner : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public CameraController cameraController;

        [Header("Road Decoration Colors")]
        public Color streetLightColor = new(0.9f, 0.85f, 0.5f);
        public Color treeCanopyColor = new(0.12f, 0.28f, 0.10f);
        public Color treeTrunkColor = new(0.35f, 0.22f, 0.12f);
        public Color benchColor = new(0.4f, 0.3f, 0.2f);
        public Color bollardColor = new(0.45f, 0.45f, 0.5f);

        [Header("Terrain Decoration Colors")]
        public Color rockColor = new(0.55f, 0.52f, 0.48f);
        public Color boulderColor = new(0.50f, 0.48f, 0.44f);
        public Color grassColor = new(0.25f, 0.45f, 0.15f);
        public Color wildflowerPetalColor = new(0.85f, 0.65f, 0.30f);
        public Color wildflowerStemColor = new(0.20f, 0.38f, 0.12f);
        public Color shrubColor = new(0.18f, 0.35f, 0.12f);
        public Color shrubLightColor = new(0.22f, 0.42f, 0.16f);
        public Color fenceColor = new(0.50f, 0.38f, 0.22f);
        public Color stumpColor = new(0.40f, 0.28f, 0.15f);
        public Color signPostColor = new(0.60f, 0.55f, 0.45f);
        public Color signFaceColor = new(0.80f, 0.75f, 0.60f);

        [Header("LOD Thresholds")]
        public float lodMediumDistance = 30f;
        public float lodCloseDistance = 12f;

        private readonly Dictionary<int, List<GameObject>> _lodGroups = new()
        {
            [0] = new(),
            [1] = new(),
            [2] = new()
        };

        private int _currentLOD = -1;
        private static readonly MaterialPropertyBlock _propBlock = new();

        public void Spawn(MapData data)
        {
            Clear();
            if (data.decorations == null) return;

            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            foreach (var dec in data.decorations)
            {
                float elev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    elev = mapManager.CurrentElevationMap.Sample(dec.position);

                var worldPos = MapGenUtils.ToWorldPosition(dec.position, elev, preset);
                var go = CreateDecoration(dec, worldPos);

                if (go != null)
                {
                    int lod = Mathf.Clamp(dec.lodLevel, 0, 2);
                    _lodGroups[lod].Add(go);
                }
            }

            _currentLOD = -1; // Force update
        }

        public void Clear()
        {
            foreach (var kvp in _lodGroups)
            {
                foreach (var obj in kvp.Value)
                    if (obj != null) Destroy(obj);
                kvp.Value.Clear();
            }
            _currentLOD = -1;
        }

        void LateUpdate()
        {
            if (cameraController == null) return;

            float dist = cameraController.CurrentDistance;
            int newLOD;
            if (dist < lodCloseDistance) newLOD = 2;      // Show all
            else if (dist < lodMediumDistance) newLOD = 1; // LOD 0+1 only
            else newLOD = 0;                               // LOD 0 only

            if (newLOD != _currentLOD)
            {
                _currentLOD = newLOD;
                UpdateLODVisibility();
            }
        }

        private void UpdateLODVisibility()
        {
            SetGroupActive(0, true);
            SetGroupActive(1, _currentLOD >= 1);
            SetGroupActive(2, _currentLOD >= 2);
        }

        private void SetGroupActive(int level, bool active)
        {
            if (!_lodGroups.TryGetValue(level, out var list)) return;
            foreach (var obj in list)
                if (obj != null) obj.SetActive(active);
        }

        // ─── Decoration factory ─────────────────────────────────────────

        private GameObject CreateDecoration(MapDecoration dec, Vector3 worldPos)
        {
            return dec.type switch
            {
                DecorationType.StreetLight => CreateStreetLight(worldPos, dec),
                DecorationType.Tree        => CreateTree(worldPos, dec),
                DecorationType.Bench       => CreateBench(worldPos, dec),
                DecorationType.Bollard     => CreateBollard(worldPos, dec),
                DecorationType.Rock        => CreateRock(worldPos, dec),
                DecorationType.Boulder     => CreateBoulder(worldPos, dec),
                DecorationType.GrassClump  => CreateGrassClump(worldPos, dec),
                DecorationType.Wildflower  => CreateWildflower(worldPos, dec),
                DecorationType.Shrub       => CreateShrub(worldPos, dec),
                DecorationType.Fence       => CreateFence(worldPos, dec),
                DecorationType.Stump       => CreateStump(worldPos, dec),
                DecorationType.SignPost    => CreateSignPost(worldPos, dec),
                _ => null
            };
        }

        // ─── Existing types ─────────────────────────────────────────────

        private GameObject CreateStreetLight(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_Light");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform);
            pole.transform.localPosition = new Vector3(0, 2f, 0);
            pole.transform.localScale = new Vector3(0.15f, 2f, 0.15f);
            RemoveCollider(pole);
            ApplyColor(pole, bollardColor);

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.transform.SetParent(root.transform);
            lamp.transform.localPosition = new Vector3(0, 4.2f, 0);
            lamp.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
            RemoveCollider(lamp);
            ApplyColor(lamp, streetLightColor);

            return root;
        }

        private GameObject CreateTree(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_Tree");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            float s = dec.scale;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0, s * 0.6f, 0);
            trunk.transform.localScale = new Vector3(0.2f * s, s * 0.6f, 0.2f * s);
            RemoveCollider(trunk);
            ApplyColor(trunk, treeTrunkColor);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(root.transform);
            canopy.transform.localPosition = new Vector3(0, s * 1.5f, 0);
            canopy.transform.localScale = Vector3.one * s * 1.2f;
            RemoveCollider(canopy);
            ApplyColor(canopy, treeCanopyColor);

            return root;
        }

        private GameObject CreateBench(Vector3 pos, MapDecoration dec)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Deco_Bench";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * 0.25f;
            go.transform.rotation = Quaternion.Euler(0, dec.angle * Mathf.Rad2Deg, 0);
            go.transform.localScale = new Vector3(2f, 0.5f, 0.6f);
            RemoveCollider(go);
            ApplyColor(go, benchColor);

            return go;
        }

        private GameObject CreateBollard(Vector3 pos, MapDecoration dec)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Deco_Bollard";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * 0.4f;
            go.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
            RemoveCollider(go);
            ApplyColor(go, bollardColor);

            return go;
        }

        // ─── New terrain-aware types ────────────────────────────────────

        private GameObject CreateRock(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Deco_Rock";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * s * 0.2f;
            go.transform.rotation = Quaternion.Euler(
                dec.angle * Mathf.Rad2Deg * 0.3f, dec.angle * Mathf.Rad2Deg, 15f);
            go.transform.localScale = new Vector3(s * 1.2f, s * 0.6f, s * 0.9f);
            RemoveCollider(go);
            ApplyColor(go, rockColor);

            return go;
        }

        private GameObject CreateBoulder(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var root = new GameObject("Deco_Boulder");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            // Main mass
            var main = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            main.transform.SetParent(root.transform);
            main.transform.localPosition = new Vector3(0, s * 0.4f, 0);
            main.transform.localScale = new Vector3(s * 1.3f, s * 0.8f, s * 1.1f);
            main.transform.localRotation = Quaternion.Euler(10f, dec.angle * Mathf.Rad2Deg, 5f);
            RemoveCollider(main);
            ApplyColor(main, boulderColor);

            // Secondary chunk
            var chunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chunk.transform.SetParent(root.transform);
            chunk.transform.localPosition = new Vector3(s * 0.5f, s * 0.2f, s * 0.3f);
            chunk.transform.localScale = new Vector3(s * 0.7f, s * 0.5f, s * 0.6f);
            chunk.transform.localRotation = Quaternion.Euler(20f, dec.angle * 45f, -10f);
            RemoveCollider(chunk);
            ApplyColor(chunk, rockColor);

            return root;
        }

        private GameObject CreateGrassClump(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var root = new GameObject("Deco_Grass");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            for (int i = 0; i < 3; i++)
            {
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tuft.transform.SetParent(root.transform);
                float ox = (i - 1) * s * 0.6f;
                float oz = (i % 2 == 0 ? 0.3f : -0.2f) * s;
                tuft.transform.localPosition = new Vector3(ox, s * 0.1f, oz);
                tuft.transform.localScale = new Vector3(s * 0.8f, s * 0.25f, s * 0.7f);
                RemoveCollider(tuft);
                ApplyColor(tuft, grassColor);
            }

            return root;
        }

        private GameObject CreateWildflower(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var root = new GameObject("Deco_Wildflower");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            // Stem
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(root.transform);
            stem.transform.localPosition = new Vector3(0, s * 0.4f, 0);
            stem.transform.localScale = new Vector3(0.05f * s, s * 0.4f, 0.05f * s);
            RemoveCollider(stem);
            ApplyColor(stem, wildflowerStemColor);

            // Flower heads
            for (int i = 0; i < 3; i++)
            {
                var petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.transform.SetParent(root.transform);
                float angle = i * Mathf.PI * 2f / 3f + dec.angle;
                float r = s * 0.15f;
                petal.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * r, s * 0.85f + i * 0.05f, Mathf.Sin(angle) * r);
                petal.transform.localScale = Vector3.one * s * 0.2f;
                RemoveCollider(petal);
                ApplyColor(petal, wildflowerPetalColor);
            }

            return root;
        }

        private GameObject CreateShrub(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var root = new GameObject("Deco_Shrub");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            // Lower mass
            var lower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lower.transform.SetParent(root.transform);
            lower.transform.localPosition = new Vector3(0, s * 0.4f, 0);
            lower.transform.localScale = new Vector3(s * 1.4f, s * 0.7f, s * 1.2f);
            RemoveCollider(lower);
            ApplyColor(lower, shrubColor);

            // Upper mass (lighter green)
            var upper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            upper.transform.SetParent(root.transform);
            upper.transform.localPosition = new Vector3(s * 0.1f, s * 0.8f, -s * 0.05f);
            upper.transform.localScale = new Vector3(s * 1.0f, s * 0.5f, s * 0.9f);
            RemoveCollider(upper);
            ApplyColor(upper, shrubLightColor);

            return root;
        }

        private GameObject CreateFence(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_Fence");
            root.transform.SetParent(transform);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0, dec.angle * Mathf.Rad2Deg, 0);

            // Two posts
            for (int i = -1; i <= 1; i += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.SetParent(root.transform);
                post.transform.localPosition = new Vector3(i * 1.2f, 0.6f, 0);
                post.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
                RemoveCollider(post);
                ApplyColor(post, fenceColor);
            }

            // Rail
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.transform.SetParent(root.transform);
            rail.transform.localPosition = new Vector3(0, 0.9f, 0);
            rail.transform.localScale = new Vector3(2.6f, 0.08f, 0.08f);
            RemoveCollider(rail);
            ApplyColor(rail, fenceColor);

            return root;
        }

        private GameObject CreateStump(Vector3 pos, MapDecoration dec)
        {
            float s = dec.scale;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Deco_Stump";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * s * 0.25f;
            go.transform.localScale = new Vector3(s * 0.6f, s * 0.25f, s * 0.6f);
            RemoveCollider(go);
            ApplyColor(go, stumpColor);

            return go;
        }

        private GameObject CreateSignPost(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_SignPost");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform);
            pole.transform.localPosition = new Vector3(0, 1.5f, 0);
            pole.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
            RemoveCollider(pole);
            ApplyColor(pole, signPostColor);

            // Sign face
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.transform.SetParent(root.transform);
            sign.transform.localPosition = new Vector3(0, 3.2f, 0);
            sign.transform.rotation = Quaternion.Euler(0, dec.angle * Mathf.Rad2Deg, 0);
            sign.transform.localScale = new Vector3(1.2f, 0.8f, 0.08f);
            RemoveCollider(sign);
            ApplyColor(sign, signFaceColor);

            return root;
        }

        // ─── Utilities ──────────────────────────────────────────────────

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            _propBlock.SetColor("_BaseColor", color);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
