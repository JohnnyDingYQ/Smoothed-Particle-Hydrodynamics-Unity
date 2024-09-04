using System;
using Unity.Mathematics;

// https://www.jacksondunstan.com/articles/5292
static class OutsideAssertIf
{
    public static void IsTrue(bool truth)
    {
#if UNITY_ASSERTIONS
        if (!truth)
            throw new Exception("Assertion failed");
#endif
    }
    public static void NotNaN(float3 num)
    {
#if UNITY_ASSERTIONS
        if (float.IsNaN(num.x) || float.IsNaN(num.y) || float.IsNaN(num.z))
            throw new Exception("Assertion failed");
#endif
    }
    public static void NotZero(float num)
    {
#if UNITY_ASSERTIONS
        if (num == 0)
            throw new Exception("Assertion failed");
#endif
    }
}