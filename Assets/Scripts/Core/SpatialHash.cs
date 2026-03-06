using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Generic 2D spatial hash for overlap detection.
    /// Port of JSX SpatialHash class. Uses AABB approximation with rotation-aware bounds.
    /// </summary>
    public class SpatialHash<T> where T : ISpatialBounds
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<T>> _cells = new();

        public SpatialHash(float cellSize = 40f)
        {
            _cellSize = cellSize;
        }

        /// <summary>
        /// Compute AABB that encloses the rotated rectangle, with 3-unit padding.
        /// Port of JSX _bounds().
        /// </summary>
        public Rect GetBounds(T item)
        {
            float cos = Mathf.Abs(Mathf.Cos(item.Angle));
            float sin = Mathf.Abs(Mathf.Sin(item.Angle));
            float hw = (item.Width * cos + item.Height * sin) / 2f + 3f;
            float hh = (item.Width * sin + item.Height * cos) / 2f + 3f;
            return new Rect(item.Position.x - hw, item.Position.y - hh, hw * 2f, hh * 2f);
        }

        public void Insert(T item)
        {
            var bd = GetBounds(item);
            int x0 = Mathf.FloorToInt(bd.x / _cellSize);
            int x1 = Mathf.FloorToInt((bd.x + bd.width) / _cellSize);
            int y0 = Mathf.FloorToInt(bd.y / _cellSize);
            int y1 = Mathf.FloorToInt((bd.y + bd.height) / _cellSize);

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    long key = PackKey(x, y);
                    if (!_cells.TryGetValue(key, out var list))
                    {
                        list = new List<T>();
                        _cells[key] = list;
                    }
                    list.Add(item);
                }
            }
        }

        public bool Overlaps(T item)
        {
            var bd = GetBounds(item);
            int x0 = Mathf.FloorToInt(bd.x / _cellSize);
            int x1 = Mathf.FloorToInt((bd.x + bd.width) / _cellSize);
            int y0 = Mathf.FloorToInt(bd.y / _cellSize);
            int y1 = Mathf.FloorToInt((bd.y + bd.height) / _cellSize);

            var seen = new HashSet<T>();
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    long key = PackKey(x, y);
                    if (!_cells.TryGetValue(key, out var list)) continue;
                    foreach (var other in list)
                    {
                        if (!seen.Add(other)) continue;
                        var ob = GetBounds(other);
                        if (!(bd.x + bd.width < ob.x || ob.x + ob.width < bd.x ||
                              bd.y + bd.height < ob.y || ob.y + ob.height < bd.y))
                            return true;
                    }
                }
            }
            return false;
        }

        public void Clear()
        {
            _cells.Clear();
        }

        private static long PackKey(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }
    }
}
