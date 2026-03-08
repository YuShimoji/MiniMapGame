using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Shared utilities for floor plan generation: BSP subdivision, adjacency detection,
    /// door placement, footprint calculation.
    /// </summary>
    public static class FloorPlanUtils
    {
        /// <summary>
        /// Binary Space Partition of a rectangle into sub-rooms.
        /// Returns leaf rectangles that satisfy size constraints.
        /// </summary>
        public static List<Rect> Subdivide(
            Rect area, SeededRng rng, float minSize, float maxSize,
            int maxDepth, float irregularity = 0f)
        {
            var result = new List<Rect>();
            SubdivideRecursive(area, rng, minSize, maxSize, maxDepth, 0, irregularity, result);
            return result;
        }

        private static void SubdivideRecursive(
            Rect area, SeededRng rng, float minSize, float maxSize,
            int maxDepth, int depth, float irregularity, List<Rect> result)
        {
            bool tooWide = area.width > maxSize;
            bool tooTall = area.height > maxSize;

            // Stop splitting if both dimensions are within range or max depth reached
            if (!tooWide && !tooTall)
            {
                result.Add(area);
                return;
            }
            if (depth >= maxDepth)
            {
                result.Add(area);
                return;
            }

            // Choose split direction: prefer splitting the longer axis
            bool splitHorizontal;
            if (tooWide && !tooTall) splitHorizontal = false;
            else if (tooTall && !tooWide) splitHorizontal = true;
            else splitHorizontal = rng.Next() > 0.5f;

            if (splitHorizontal)
            {
                // Split top/bottom
                float splitMin = Mathf.Max(minSize, area.height * 0.3f);
                float splitMax = Mathf.Min(area.height - minSize, area.height * 0.7f);
                if (splitMin >= splitMax)
                {
                    result.Add(area);
                    return;
                }

                float splitY = splitMin + rng.Next() * (splitMax - splitMin);

                // Apply irregularity offset
                if (irregularity > 0f)
                    splitY += (rng.Next() - 0.5f) * irregularity * minSize;
                splitY = Mathf.Clamp(splitY, minSize, area.height - minSize);

                var top = new Rect(area.x, area.y, area.width, splitY);
                var bottom = new Rect(area.x, area.y + splitY, area.width, area.height - splitY);

                SubdivideRecursive(top, rng, minSize, maxSize, maxDepth, depth + 1, irregularity, result);
                SubdivideRecursive(bottom, rng, minSize, maxSize, maxDepth, depth + 1, irregularity, result);
            }
            else
            {
                // Split left/right
                float splitMin = Mathf.Max(minSize, area.width * 0.3f);
                float splitMax = Mathf.Min(area.width - minSize, area.width * 0.7f);
                if (splitMin >= splitMax)
                {
                    result.Add(area);
                    return;
                }

                float splitX = splitMin + rng.Next() * (splitMax - splitMin);

                if (irregularity > 0f)
                    splitX += (rng.Next() - 0.5f) * irregularity * minSize;
                splitX = Mathf.Clamp(splitX, minSize, area.width - minSize);

                var left = new Rect(area.x, area.y, splitX, area.height);
                var right = new Rect(area.x + splitX, area.y, area.width - splitX, area.height);

                SubdivideRecursive(left, rng, minSize, maxSize, maxDepth, depth + 1, irregularity, result);
                SubdivideRecursive(right, rng, minSize, maxSize, maxDepth, depth + 1, irregularity, result);
            }
        }

        /// <summary>
        /// Find pairs of rooms that share a wall edge (adjacency).
        /// Two rooms are adjacent if they share an edge segment of at least doorWidth.
        /// </summary>
        public static List<(int a, int b)> FindAdjacentPairs(
            List<InteriorRoom> rooms, float doorWidth, float tolerance = 0.5f)
        {
            var pairs = new List<(int, int)>();

            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (AreAdjacent(rooms[i], rooms[j], doorWidth, tolerance))
                        pairs.Add((i, j));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Check if two axis-aligned rooms share a wall edge of at least minOverlap length.
        /// </summary>
        public static bool AreAdjacent(InteriorRoom a, InteriorRoom b, float minOverlap, float tolerance = 0.5f)
        {
            float aLeft = a.position.x - a.size.x * 0.5f;
            float aRight = a.position.x + a.size.x * 0.5f;
            float aBottom = a.position.y - a.size.y * 0.5f;
            float aTop = a.position.y + a.size.y * 0.5f;

            float bLeft = b.position.x - b.size.x * 0.5f;
            float bRight = b.position.x + b.size.x * 0.5f;
            float bBottom = b.position.y - b.size.y * 0.5f;
            float bTop = b.position.y + b.size.y * 0.5f;

            // Check horizontal adjacency (sharing vertical edge)
            float xOverlap = Mathf.Min(aTop, bTop) - Mathf.Max(aBottom, bBottom);
            bool hAdj = (Mathf.Abs(aRight - bLeft) < tolerance || Mathf.Abs(bRight - aLeft) < tolerance)
                        && xOverlap >= minOverlap;

            // Check vertical adjacency (sharing horizontal edge)
            float yOverlap = Mathf.Min(aRight, bRight) - Mathf.Max(aLeft, bLeft);
            bool vAdj = (Mathf.Abs(aTop - bBottom) < tolerance || Mathf.Abs(bTop - aBottom) < tolerance)
                        && yOverlap >= minOverlap;

            return hAdj || vAdj;
        }

        /// <summary>
        /// Place doors between adjacent room pairs.
        /// Returns the door at the midpoint of the shared edge.
        /// </summary>
        public static List<InteriorDoor> PlaceDoors(
            List<InteriorRoom> rooms, List<(int a, int b)> adjacentPairs,
            SeededRng rng, float doorWidth, float tolerance = 0.5f)
        {
            var doors = new List<InteriorDoor>();

            foreach (var (a, b) in adjacentPairs)
            {
                var ra = rooms[a];
                var rb = rooms[b];
                var doorPos = FindSharedEdgeMidpoint(ra, rb, tolerance);

                doors.Add(new InteriorDoor
                {
                    roomA = a,
                    roomB = b,
                    position = doorPos,
                    width = doorWidth,
                    isHidden = false,
                    isLocked = false
                });
            }

            return doors;
        }

        /// <summary>
        /// Find the midpoint of the shared edge between two adjacent rooms.
        /// </summary>
        public static Vector2 FindSharedEdgeMidpoint(InteriorRoom a, InteriorRoom b, float tolerance = 0.5f)
        {
            float aLeft = a.position.x - a.size.x * 0.5f;
            float aRight = a.position.x + a.size.x * 0.5f;
            float aBottom = a.position.y - a.size.y * 0.5f;
            float aTop = a.position.y + a.size.y * 0.5f;

            float bLeft = b.position.x - b.size.x * 0.5f;
            float bRight = b.position.x + b.size.x * 0.5f;
            float bBottom = b.position.y - b.size.y * 0.5f;
            float bTop = b.position.y + b.size.y * 0.5f;

            // Right edge of A touches left edge of B
            if (Mathf.Abs(aRight - bLeft) < tolerance)
            {
                float overlapMin = Mathf.Max(aBottom, bBottom);
                float overlapMax = Mathf.Min(aTop, bTop);
                return new Vector2(aRight, (overlapMin + overlapMax) * 0.5f);
            }
            // Left edge of A touches right edge of B
            if (Mathf.Abs(aLeft - bRight) < tolerance)
            {
                float overlapMin = Mathf.Max(aBottom, bBottom);
                float overlapMax = Mathf.Min(aTop, bTop);
                return new Vector2(aLeft, (overlapMin + overlapMax) * 0.5f);
            }
            // Top edge of A touches bottom edge of B
            if (Mathf.Abs(aTop - bBottom) < tolerance)
            {
                float overlapMin = Mathf.Max(aLeft, bLeft);
                float overlapMax = Mathf.Min(aRight, bRight);
                return new Vector2((overlapMin + overlapMax) * 0.5f, aTop);
            }
            // Bottom edge of A touches top edge of B
            if (Mathf.Abs(aBottom - bTop) < tolerance)
            {
                float overlapMin = Mathf.Max(aLeft, bLeft);
                float overlapMax = Mathf.Min(aRight, bRight);
                return new Vector2((overlapMin + overlapMax) * 0.5f, aBottom);
            }

            // Fallback: midpoint between centers
            return (a.position + b.position) * 0.5f;
        }

        /// <summary>
        /// Calculate the usable footprint rectangle from building context.
        /// Origin is at (0,0), room positions are relative to footprint center.
        /// </summary>
        public static Rect CalculateFootprint(InteriorBuildingContext context)
        {
            // For L-shape and stepped buildings, use the main block dimensions
            float w = context.footprintWidth;
            float h = context.footprintHeight;

            switch (context.shapeType)
            {
                case 1: // L-shape: main block is 100% width × 60% height
                    h *= 0.6f;
                    break;
                case 2: // Cylinder: inscribed rectangle
                    float radius = Mathf.Min(w, h) * 0.5f;
                    w = radius * 1.41f; // sqrt(2)
                    h = radius * 1.41f;
                    break;
                case 3: // Stepped: use base dimensions
                    break;
                default: // Box: full dimensions
                    break;
            }

            return new Rect(-w * 0.5f, -h * 0.5f, w, h);
        }

        /// <summary>
        /// Convert BSP Rects to InteriorRooms with default Normal type.
        /// </summary>
        public static List<InteriorRoom> RectsToRooms(List<Rect> rects, int startId = 0)
        {
            var rooms = new List<InteriorRoom>();
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                rooms.Add(new InteriorRoom
                {
                    id = startId + i,
                    type = InteriorRoomType.LivingRoom, // Default, will be reassigned
                    position = r.center,
                    size = r.size,
                    rotation = 0f,
                    discoverySlotCount = 0,
                    isSecret = false
                });
            }
            return rooms;
        }

        /// <summary>
        /// Ensure all rooms are reachable by adding corridors for disconnected components.
        /// Returns corridors connecting disconnected room groups.
        /// </summary>
        public static List<InteriorCorridor> EnsureConnectivity(
            List<InteriorRoom> rooms, List<(int a, int b)> adjacentPairs,
            float corridorWidth)
        {
            var corridors = new List<InteriorCorridor>();
            if (rooms.Count <= 1) return corridors;

            // Union-Find for connectivity
            int[] parent = new int[rooms.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            foreach (var (a, b) in adjacentPairs)
            {
                Union(parent, a, b);
            }

            // Find disconnected components and connect them
            for (int i = 1; i < rooms.Count; i++)
            {
                if (Find(parent, i) != Find(parent, 0))
                {
                    // Find closest room in component 0 to room i
                    int closest = 0;
                    float minDist = float.MaxValue;
                    for (int j = 0; j < rooms.Count; j++)
                    {
                        if (Find(parent, j) == Find(parent, 0))
                        {
                            float dist = Vector2.Distance(rooms[i].position, rooms[j].position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closest = j;
                            }
                        }
                    }

                    corridors.Add(new InteriorCorridor
                    {
                        roomA = closest,
                        roomB = i,
                        width = corridorWidth,
                        waypoints = new[] { rooms[closest].position, rooms[i].position }
                    });

                    Union(parent, closest, i);
                }
            }

            return corridors;
        }

        /// <summary>
        /// Insert dead space by shrinking rooms and creating WallVoid rooms in the gaps.
        /// </summary>
        public static void InsertDeadSpace(
            List<InteriorRoom> rooms, SeededRng rng, float ratio, float wallVoidProb)
        {
            if (ratio <= 0f) return;

            int count = rooms.Count;
            for (int i = 0; i < count; i++)
            {
                if (rng.Next() > wallVoidProb) continue;

                var room = rooms[i];
                // Don't shrink entrance or very small rooms
                if (room.type == InteriorRoomType.Entrance) continue;
                if (room.size.x < 4f || room.size.y < 4f) continue;

                // Shrink one side and create a WallVoid
                float shrinkAmount = Mathf.Min(room.size.x * ratio, room.size.y * ratio, 2f);
                if (shrinkAmount < 1f) continue;

                bool shrinkX = rng.Next() > 0.5f;

                var voidRoom = new InteriorRoom
                {
                    id = rooms.Count,
                    type = InteriorRoomType.WallVoid,
                    rotation = 0f,
                    discoverySlotCount = 0,
                    isSecret = false
                };

                if (shrinkX)
                {
                    float side = rng.Next() > 0.5f ? 1f : -1f;
                    voidRoom.size = new Vector2(shrinkAmount, room.size.y * 0.5f);
                    voidRoom.position = new Vector2(
                        room.position.x + side * (room.size.x * 0.5f + shrinkAmount * 0.5f),
                        room.position.y + (rng.Next() - 0.5f) * room.size.y * 0.3f);
                }
                else
                {
                    float side = rng.Next() > 0.5f ? 1f : -1f;
                    voidRoom.size = new Vector2(room.size.x * 0.5f, shrinkAmount);
                    voidRoom.position = new Vector2(
                        room.position.x + (rng.Next() - 0.5f) * room.size.x * 0.3f,
                        room.position.y + side * (room.size.y * 0.5f + shrinkAmount * 0.5f));
                }

                rooms.Add(voidRoom);
            }
        }

        private static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra != rb) parent[ra] = rb;
        }
    }
}
