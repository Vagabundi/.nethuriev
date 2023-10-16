﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

class SparseVectors
{
    public static int[] GenerateRandomVector(int length, double density)
    {
        if (length <= 0 || density < 0.0 || density > 1.0)
        {
            throw new ArgumentException("Invalid input parameters");
        }

        Random rand = new Random();
        int[] vector = new int[length];

        for (int i = 0; i < length; i++)
        {
            if (rand.NextDouble() < density)
            {
                vector[i] = rand.Next(-100, 100);
            }
            else
            {
                vector[i] = 0;
            }
        }

        return vector;
    }

    public static ConcurrentDictionary<int, int> CompressSparseVector(int[] vector)
    {
        var sparseVector = new ConcurrentDictionary<int, int>();

        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] != 0)
            {
                sparseVector[i] = vector[i];
            }
        }

        return sparseVector;
    }

    public static Dictionary<int, int> CompressSparseVectorMultithreaded(int[] denseVector)
    {
        var sparseVector = new Dictionary<int, int>();

        int blockSize = 1000000;
        int numBlocks = (denseVector.Length + blockSize - 1) / blockSize;

        for (int blockIndex = 0; blockIndex < numBlocks; blockIndex++)
        {
            int start = blockIndex * blockSize;
            int end = Math.Min(start + blockSize, denseVector.Length);

            Parallel.For(start, end, i =>
            {
                if (denseVector[i] != 0)
                {
                    lock (sparseVector)
                    {
                        sparseVector[i] = denseVector[i];
                    }
                }
            });
        }

        return sparseVector;
    }


    static void MeasureExecutionTime(Action action, string operationName)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        action();
        stopwatch.Stop();
        Console.WriteLine($"{operationName}: Time taken: {stopwatch.ElapsedMilliseconds} ms");
    }

    static void Main(string[] args)
    {
        int[] vectorSizes = { 1000000, 5000000, 10000000, 50000000, 100000000};
        double density = 0.5; 

        foreach (int size in vectorSizes)
        {
            int[] denseVector = GenerateRandomVector(size, density);
            Console.WriteLine($"Vector size: {size}");
        
            MeasureExecutionTime(() => CompressSparseVector(denseVector), "Single-threaded Compression");

            MeasureExecutionTime(() => CompressSparseVectorMultithreaded(denseVector), "Multi-threaded Compression");
     
            Console.WriteLine();
        }
    }
}