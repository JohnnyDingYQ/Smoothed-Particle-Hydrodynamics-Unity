using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

public class SPHTest
{
    [SetUp]
    public void SetUp()
    {
        Grid.InitParticles();
    }

    [Test, Performance]
    public void FindNeigborsPerformance()
    {
        Measure.Method(() => { SPH.FindNeigbors(); })
            .WarmupCount(5)
            .MeasurementCount(4)
            .IterationsPerMeasurement(4)
            .GC()
            .Run();
    }



    // [Test]
    // public void BasicFindNeighbor()
    // {
    //     SPH.FindNeigbors();
    //     Int3 c = new(1, 1, 1);
    //     Particle p = Grid.GetCell(c).First();
    //     HashSet<Particle> expected = new();
    //     for (int i = -1; i <= 1; i++)
    //         for (int j = -1; j <= 1; j++)
    //             for (int k = -1; k <= 1; k++)
    //             {
    //                 if (i == 0 && j == 0 && k == 0)
    //                     continue;
    //                 expected.Add(Grid.GetCell(c + new Int3(i, j, k)).First());
    //             }
    //     Assert.True(p.Neighbors.SetEquals(expected));

    //     p = Grid.GetCell(new Int3(0, 0, 0)).First();
    //     Assert.AreEqual(7, p.Neighbors.Count);
    // }
}