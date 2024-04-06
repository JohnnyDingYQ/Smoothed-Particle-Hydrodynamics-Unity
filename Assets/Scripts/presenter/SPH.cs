using UnityEngine;

public static class SPH
{
    public static void FindNeigbors()
    {
        int x = 1;
        int y = 1;
        int z = 1;
        foreach (Particle p in Grid.Particles)
        {
            p.Neighbors.Clear();
            
            for (int i = x - 1; i <= x + 1; i++)
                for (int j = y - 1; j <= y + 1; j++)
                    for (int k = z - 1; k <= z + 1; k++)
                    {
                        if (i == x && j == y && k == z)
                            continue;
                        if (IsValidCoord(i, j, k))
                            foreach(Particle particle in Grid.GetCell(x, y))
                                p.Neighbors.Add(particle);
                    }
        }
    }

    public static bool IsValidCoord(int x, int y, int z)
    {
        int d = Grid.Dimension;
        return x >= 0 && y >= 0 && z >= 0 && x < d && y < d && z < d;
    }
}