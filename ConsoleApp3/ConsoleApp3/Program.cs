using System;
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

    public static Dictionary<int, int> CompressSparseVector(int[] vector)
    {
        var denseVector = new Dictionary<int, int>();

        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] != 0)
            {
                denseVector[i] = vector[i];
            }
        }
        return denseVector;
    }
    public static Dictionary<int, int> CompressSparseVectorMultithreaded2(int[] sparseVector)
    {
        var compressedSparseVector = new Dictionary<int, int>();
        int elements = sparseVector.Length;

        int tasksCount = Environment.ProcessorCount;

        Task[] tasks = new Task[tasksCount];
        object lockObj = new object();
        int elementsPerTask = elements / tasksCount;

        List<Dictionary<int, int>> localDictionaries = new List<Dictionary<int, int>>(tasksCount);

        for (int i = 0; i < tasksCount; i++)
        {
            localDictionaries.Add(new Dictionary<int, int>());
            int start = i * elementsPerTask;
            int end = (i == tasksCount - 1) ? elements : start + elementsPerTask;

            tasks[i] = Task.Factory.StartNew(state =>
            {
                var localDict = (Dictionary<int, int>)state;
                for (int j = start; j < end; j++)
                {
                    if (sparseVector[j] != 0)
                    {
                        localDict[j] = sparseVector[j];
                    }
                }
            }, localDictionaries[i]);
        }
        Task.WhenAll(tasks).Wait();
        foreach (var localDict in localDictionaries)
        {
            lock (lockObj)
            {
                foreach (var kvp in localDict)
                {
                    compressedSparseVector[kvp.Key] = kvp.Value;
                }
            }
        }
        return compressedSparseVector;
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

            MeasureExecutionTime(() => CompressSparseVectorMultithreaded2(denseVector), "Multi-threaded Compression");
     
            Console.WriteLine();
        }
     
        
    }
}
