using NUnit.Framework;

public class Int3Test
{
    [Test]
    public void Addition()
    {
        Int3 r = new Int3(1, 2, 3) + new Int3(4, 5, 6);
        Assert.AreEqual(5, r.x);
        Assert.AreEqual(7, r.y);
        Assert.AreEqual(9, r.z);
    }
}