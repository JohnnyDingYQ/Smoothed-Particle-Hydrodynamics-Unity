using System;
using Unity.Mathematics;
using UnityEngine;

public static class SPH
{
    static int iteration = 0;
    public static void Step()
    {
        FindNeigbors();
        ComputePressure();
        ComputeForces();
        ApplyMotion();
        UpdateGrid();
        iteration ++;
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
                    if (IsValidCoord(i, j))
                        foreach (Particle particle in Grid.GetCell(i, j))
                        {
                            if (particle == p)
                                continue;
                            p.Neighbors.Add(particle);
                        }

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
                sum += j.Mass * Kernel(i.Position, j.Position);
            }

            i.Density = sum;
        }


        static void ComputePressure(Particle i)
        {
            i.Pressure = Parameters.Stiffness * (MathF.Pow(i.Density / Parameters.DesiredRestDensity, 7) - 1);
        }
    }

    static void ComputeForces()
    {
        foreach (Particle i in Grid.Particles)
        {
            float3 pressure = Pressure(i);
            float3 viscosity = Viscosity(i);
            float3 gravity = i.Mass * Parameters.Gravity * new float3(0, -1, 0);
            i.Force = pressure + viscosity + gravity;

            // if (i.IsTagged)
            //     Debug.Log("Pressure: " + math.length(pressure) / Parameters.Mass +
            //     " viscosity: " + math.length(viscosity) / Parameters.Mass +
            //     " gravity: " + math.length(gravity) / Parameters.Mass);
        }

        float3 Pressure(Particle i)
        {
            float3 sum = 0;
            float test = 0;
            foreach (Particle j in i.Neighbors)
            {
                // if (j.Pressure / MathF.Pow(j.Density, 2) > 100000000000 && i.IsTagged)
                // {
                //     Debug.Log(j.InitialX + " "  + j.InitialY);
                // }
                    
                test += j.Pressure / MathF.Pow(j.Density, 2);
                sum += j.Mass * (i.Pressure / MathF.Pow(i.Density, 2) + j.Pressure / MathF.Pow(j.Density, 2))
                    * KernelGradient(i.Position, j.Position);
            }
            // if (i.IsTagged)
            //     Debug.Log(test);
            float3 pressureGradient = i.Density * sum;
            return -i.Mass / i.Density * pressureGradient;
        }

        float3 Viscosity(Particle i)
        {
            float3 sum = 0;
            foreach (Particle j in i.Neighbors)
            {
                float3 xij = i.Position - j.Position;
                sum += j.Mass / j.Density * (i.Velocity - j.Velocity) * xij * KernelGradient(i.Position, j.Position)
                    / (math.dot(xij, xij) + 0.01f * MathF.Pow(Parameters.SmoothingLength, 2));
            }
            float3 doubleGradientVelocity = 2 * sum;
            return i.Mass * Parameters.KinematicViscosity * doubleGradientVelocity;
        }
    }

    static void ApplyMotion()
    {
        foreach (Particle i in Grid.Particles)
        {
            i.Velocity += Parameters.TimeStep * i.Force / i.Mass;
            float3 next = i.Position + Parameters.TimeStep * i.Velocity;
            if (next.x < 0 || next.x >= Grid.Dimension * Grid.CellSize || next.y < 0 || next.y >= Grid.Dimension * Grid.CellSize)
            {
                i.Velocity *= -0.5f;
                i.Position += i.Velocity * Parameters.TimeStep;
            }
            else
                i.Position = next;
            if (float.IsNaN(i.Position.x) || float.IsNaN(i.Position.y))
            {
                float d = Grid.CellSize * Grid.Dimension;
                i.Position = new float3(d / 2, d * 2 / 3, 0);
                i.Velocity = 0;
            }
        }
    }

    static void UpdateGrid()
    {
        foreach (Particle i in Grid.Particles)
        {
            int newX = (int)(i.Position.x / Parameters.CellSize);
            int newY = (int)(i.Position.y / Parameters.CellSize);
            if (i.X == newX && i.Y == newY)
                continue;
            Grid.GetCell(i.X, i.Y).Remove(i);
            Grid.GetCell(newX, newY).Add(i);
            i.X = newX;
            i.Y = newY;
        }
    }

    static float Kernel(float3 i, float3 j)
    {
        float q = MathF.Sqrt(MathF.Pow(i.x - j.x, 2) + MathF.Pow(i.y - j.y, 2)) / Parameters.SmoothingLength;
        float fq = (float)3 / 2 / MathF.PI;
        if (q >= 2)
            fq = 0;
        else if (q < 2 && q >= 1)
            fq *= MathF.Pow(2 - q, 3) / 6;
        else if (q < 1)
            fq *= (float)2 / 3 - MathF.Pow(q, 2) + 0.5f * MathF.Pow(q, 2);
        return fq / MathF.Pow(Parameters.SmoothingLength, 3);
    }

    static float3 KernelGradient(float3 i, float3 j)
    {
        float h = 0.1f;
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