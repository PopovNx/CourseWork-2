namespace ImageCP;

public readonly unsafe struct PixelRef(byte* r, byte* g, byte* b)
{
    public void Deconstruct(out byte* r1, out byte* g1, out byte* b1)
    {
        r1 = r;
        g1 = g;
        b1 = b;
    }
}