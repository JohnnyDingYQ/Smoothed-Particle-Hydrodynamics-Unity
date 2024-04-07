using System;
using UnityEngine;

public static class Parameters
{
    public static float Mass = MathF.Pow(SmoothingLength, 3);
    public const float SmoothingLength = 40.0f;
    public const float CellSize = SmoothingLength;
    public const int Dimension = 30;
    public const int WallThickness = 3;
    public const float TimeStep = 0.005f;
    public const float Stiffness = 2f;
    public const float DesiredRestDensity = 3f;
    public const float InitialDensity = 1;
    public const float KinematicViscosity = 0.1f;
    public const float Gravity = 9.8f;
}