using UnityEngine;

public static class SPH
{
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

    public static bool IsValidCoord(int x, int y)
    {
        int d = Grid.Dimension;
        return x >= 0 && y >= 0 && x < d && y < d;
    }
}