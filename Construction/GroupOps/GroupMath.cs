using UnityEngine;

/// <summary>
/// Чистая математика для групповых операций.
/// </summary>
public static class GroupMath
{
    public static Vector2Int RotateVector(Vector2Int v, float angle)
    {
        int x = v.x, y = v.y;
        if (Mathf.Abs(angle - 90f) < 1f)  return new Vector2Int(-y,  x);
        if (Mathf.Abs(angle - 180f) < 1f) return new Vector2Int(-x, -y);
        if (Mathf.Abs(angle - 270f) < 1f) return new Vector2Int( y, -x);
        return v;
    }

    public static Vector2Int GetRotatedSize(Vector2Int size, float angle)
    {
        if (Mathf.Abs(angle - 90f) < 1f || Mathf.Abs(angle - 270f) < 1f)
            return new Vector2Int(size.y, size.x);
        return size;
    }
}