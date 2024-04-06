using System;
using Unity.Mathematics;
using UnityEngine;

public static class SPH
{
    public static void Step()
    {
        FindNeigbors();
        ComputePressure();
        ComputeForces();
    }

    public static void FindNeigbors()
    {
        int x;
        int y;
        foreach (Particle p in Grid.Particles)
        {
            p.Neighbors.Clear();
            x = p.X;
            y = p.Y;
            for (int i = x - 1; i <= x + 1; i++)
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i == x && j == y)
                        continue;
                    if (IsValidCoord(i, j))
                        foreach (Particle particle in Grid.GetCell(i, j))
                            p.Neighbors.Add(particle);
                }
        }
    }

    public static void ComputePressure()
    {
        foreach (Particle i in Grid.Particles)
        {
            ComputeDensity(i);
            ComputePressure(i);
        }

        void ComputeDensity(Particle i)
        {
            float sum = 0;
            foreach (Particle j in i.Neighbors)
            {
                sum += j.Mass / j.Density * math.dot(i.Velocity - j.Velocity, KernelGradient(i.Position, j.Position));
            }
            float deltaDensity = i.Density * sum;
            i.Density = deltaDensity * 0.02f;
        }


        static void ComputePressure(Particle i)
        {
            i.Pressure = Constants.Stiffness * (MathF.Pow(i.Density / Constants.DesiredRestDensity, 7) - 1);
        }
    }

    static void ComputeForces()
    {
        foreach (Particle i in Grid.Particles)
        {
            float3 preasure = Pressure(i);
            float3 viscosity = Viscosity(i);
        }

        float3 Pressure(Particle i)
        {
            float3 sum = 0;
            foreach (Particle j in i.Neighbors)
            {
                sum += j.Mass * (1 / i.Density + 1 / j.Density) * KernelGradient(i.Position, j.Position);
            }
            float3 densityGradient = i.Density * sum;
            return -i.Mass / i.Density * densityGradient;
        }

        float3 Viscosity(Particle i)
        {
            float3 sum = 0;
            foreach (Particle j in i.Neighbors)
            {
                float3 xij = i.Position - j.Position;
                sum += j.Mass / j.Density * (i.Velocity - j.Velocity) * xij * KernelGradient(i.Position, j.Position)
                    / (math.dot(xij, xij) + 0.01f * MathF.Pow(Constants.SmoothingLength, 2));
            }
            float3 doubleGradientVelocity = 2*sum;
            return i.Mass * Constants.KinematicViscosity * doubleGradientVelocity;
        }
    }

    static float Kernel(float3 i, float3 j)
    {
        float q = MathF.Sqrt(MathF.Pow(i.x - j.x, 2) + MathF.Pow(i.y - j.y, 2)) / Constants.SmoothingLength;
        float fq = (float)3 / 2 * MathF.PI;
        if (q >= 2)
            fq = 0;
        else if (q < 2 && q >= 1)
            fq *= MathF.Pow(2 - q, 3) / 6;
        else if (q < 1)
            fq *= (float)2 / 3 - MathF.Pow(q, 2) + 0.5f * MathF.Pow(q, 3);
        return fq / MathF.Pow(Constants.SmoothingLength, 2);
    }

    static float3 KernelGradient(float3 i, float3 j)
    {
        float h = 0.001f;
        float h2 = h * 2;
        float3 gradient = new(0, 0, 0);
        float3 offset = new(1, 0, 0);
        gradient.x = (Kernel(i - h2 * offset, j) - 8 * Kernel(i - h * offset, j) + 8 * Kernel(i + h * offset, j) - Kernel(i + h2 * offset, j)) / (h2 * 6);
        offset = new(0, 1, 0);
        gradient.y = (Kernel(i - h2 * offset, j) - 8 * Kernel(i - h * offset, j) + 8 * Kernel(i + h * offset, j) - Kernel(i + h2 * offset, j)) / (h2 * 6);
        return gradient;
    }

    public static bool IsValidCoord(int x, int y)
    {
        int d = Grid.Dimension;
        return x >= 0 && y >= 0 && x < d && y < d;
    }
}