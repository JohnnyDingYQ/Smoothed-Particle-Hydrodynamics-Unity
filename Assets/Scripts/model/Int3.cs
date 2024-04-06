using System;

public struct Int3 : IEquatable<Int3>
{
    public int x;
    public int y;
    public int z;
    public Int3(int a, int b, int c)
    {
        x = a;
        y = b;
        z = c;

    }

    public override int GetHashCode() {
        return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    }
    public override bool Equals(object obj) {
        return obj is Int3 c && Equals(c);
    }

    public bool Equals(Int3 other)
    {
        return x == other.x && y == other.y && z == other.z;
    }
    
    public static Int3 operator+ (Int3 l, Int3 r)
    {
        return new Int3(l.x + r.x, l.y + r.y, l.z + r.z);
    }

    public static Int3 operator- (Int3 l, Int3 r)
    {
        return new Int3(l.x - r.x, l.y - r.y, l.z - r.z);
    }

    public override string ToString()
    {
        return String.Format("({0}, {1}, {2})", x, y, z);
    }
}