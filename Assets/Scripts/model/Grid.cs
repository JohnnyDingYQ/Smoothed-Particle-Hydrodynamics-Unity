using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Grid
{
    public static float CellSize { get; set; }
    public static int Dimension { get; set; }
    public static int WallThickness { get; set; }
    private static HashSet<Particle>[,] array2D;
    public static Particle[] Particles { get; set; }

    static Grid()
    {
        CellSize = Constants.CellSize;
        Dimension = Constants.Dimension;
        WallThickness = Constants.WallThickness;
    }

    public static void InitParticles()
    {
        array2D = new HashSet<Particle>[Dimension, Dimension];
        Particles = new Particle[Dimension * Dimension];
        float3 offset = new(20.0f, 0f, 20.0f);
        int count = 0;
        for (int i = 0; i < Dimension; i++)
            for (int j = 0; j < Dimension; j++)
            {
                float3 pos = new float3(i, j, 0) * CellSize + offset;
                Int3 coord = new(i, j, 0);
                Particle p = new(pos, IsWall(i, j) ? Type.Solid : Type.Fluid, coord);
                array2D[i, j] = new() { p };
                Particles[count++] = p;
            }


        static bool IsWall(int x, int y)
        {
            int w = WallThickness;
            int _w = Dimension - WallThickness;
            return x < w || x >= _w || y < w;
        }
    }

    public static Int3 GetCoordFromIndex(int count)
    {
        int x, y, z;
        y = count / (Dimension * Dimension);
        z = count % (Dimension * Dimension) / Dimension;
        x = count % (Dimension * Dimension) % Dimension;
        return new Int3(x, y, z);
    }

    public static HashSet<Particle> GetCell(int x, int y)
    {
        return array2D[x, y];
    }
}