using System;
using UnityEngine;

public static class Parameters
{
    public static float Mass = MathF.Pow(SmoothingLength, 3);
    public const float SmoothingLength = 40.0f;
    public const float CellSize = 40.0f;
    public const int Dimension = 30;
    public const float TimeStep = 0.05f;
    public const float Stiffness = 50f;
    public const float DesiredRestDensity = 1.4f;
    public const float InitialDensity = 1;
    public const float KinematicViscosity = 30f;
    public const float Gravity = 9.8f;
}