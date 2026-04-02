using System.Collections.Generic;
using UnityEngine;

public class SpatialHash
{
    readonly Dictionary<long, List<TopBase>> cells = new(64);
    readonly List<TopBase> queryBuffer = new(16);

    static long Key(float x, float y)
    {
        int cx = Mathf.FloorToInt(x / GameConfig.CellSize);
        int cy = Mathf.FloorToInt(y / GameConfig.CellSize);
        return ((long)cx << 32) | (uint)cy;
    }

    public void Clear() => cells.Clear();

    public void Insert(TopBase top)
    {
        long k = Key(top.X, top.Y);
        if (!cells.TryGetValue(k, out var list))
            cells[k] = list = new List<TopBase>(4);
        list.Add(top);
    }

    public List<TopBase> Query(TopBase top)
    {
        queryBuffer.Clear();
        int cx = Mathf.FloorToInt(top.X / GameConfig.CellSize);
        int cy = Mathf.FloorToInt(top.Y / GameConfig.CellSize);
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            long k = ((long)(cx + dx) << 32) | (uint)(cy + dy);
            if (cells.TryGetValue(k, out var list))
                for (int i = 0; i < list.Count; i++)
                    queryBuffer.Add(list[i]);
        }
        return queryBuffer;
    }
}
