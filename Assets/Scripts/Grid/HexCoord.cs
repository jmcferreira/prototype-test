using System;
using UnityEngine;

/// <summary>
/// Axial hex coordinate (q, r). Uses flat-top orientation.
/// Third cube coordinate s is derived: s = -q - r.
/// </summary>
[Serializable]
public struct HexCoord : IEquatable<HexCoord>
{
    public int q;
    public int r;

    public int S => -q - r;

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    /// <summary>
    /// Converts axial coordinate to world position (flat-top hex layout).
    /// Outer radius (center to vertex) is controlled by <paramref name="hexSize"/>.
    /// </summary>
    public Vector3 ToWorldPosition(float hexSize = 1f)
    {
        // Flat-top hex:
        //   x = size * (3/2 * q)
        //   y = size * (sqrt(3)/2 * q + sqrt(3) * r)
        float x = hexSize * (1.5f * q);
        float y = hexSize * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Manhattan distance on the hex grid (cube-based).
    /// </summary>
    public int DistanceTo(HexCoord other)
    {
        int dq = Mathf.Abs(q - other.q);
        int dr = Mathf.Abs(r - other.r);
        int ds = Mathf.Abs(S - other.S);
        return Mathf.Max(dq, Mathf.Max(dr, ds));
    }

    /// <summary>
    /// The six axial direction vectors (flat-top hex).
    /// </summary>
    public static readonly HexCoord[] Directions = new HexCoord[]
    {
        new HexCoord(+1,  0), // E
        new HexCoord(+1, -1), // NE
        new HexCoord( 0, -1), // NW
        new HexCoord(-1,  0), // W
        new HexCoord(-1, +1), // SW
        new HexCoord( 0, +1), // SE
    };

    public HexCoord Neighbor(int direction)
    {
        HexCoord d = Directions[direction % 6];
        return new HexCoord(q + d.q, r + d.r);
    }

    // Equality
    public bool Equals(HexCoord other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(q, r);
    public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
    public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);

    public static HexCoord operator +(HexCoord a, HexCoord b) =>
        new HexCoord(a.q + b.q, a.r + b.r);

    public static HexCoord operator -(HexCoord a, HexCoord b) =>
        new HexCoord(a.q - b.q, a.r - b.r);

    public override string ToString() => $"({q}, {r})";
}
