using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Grid
{
    public static float CellSize { get; set; }
    public static int Dimension { get; set; }
    private static HashSet<Particle>[,] array2D;
    public static Particle[] Particles { get; set; }

    static Grid()
    {
        CellSize = Parameters.CellSize;
        Dimension = Parameters.Dimension;
    }

    public static void InitParticles()
    {
        array2D = new HashSet<Particle>[Dimension, Dimension];
        Particles = new Particle[Dimension * Dimension];
        float3 offset = Parameters.CellSize / 2 * new float3(1, 1, 0);
        int count = 0;
        for (int i = 0; i < Dimension; i++)
            for (int j = 0; j < Dimension; j++)
            {
                float3 pos = new float3(i, j, 0) * CellSize + offset;
                Particle p = new(pos, i, j);
                array2D[i, j] = new() { p };
                Particles[count++] = p;

                // if (i == 16 && j == 3)
                // if (i == 19 && j == 11)
                //     p.IsTagged = true;
            }
    }

    public static HashSet<Particle> GetCell(int x, int y)
    {
        return array2D[x, y];
    }
}