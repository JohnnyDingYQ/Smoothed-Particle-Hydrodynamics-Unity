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

    [Test]
    public void BasicFindNeighbor()
    {
        SPH.FindNeigbors();
        Particle p = Grid.GetCell(1, 1).First();
        List<Particle> expected = new();
        for (int i = 0; i <= 2; i++)
            for (int j = 0; j <= 2; j++)
            {
                if (i == 1 && j == 1)
                    continue;
                expected.Add(Grid.GetCell(i, j).First());
            }
        Assert.True(new HashSet<Particle>(expected).SetEquals(new HashSet<Particle>(p.Neighbors)));

        p = Grid.GetCell(0, 0).First();
        Assert.AreEqual(3, p.Neighbors.Count);
    }
}