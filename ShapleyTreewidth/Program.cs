using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Numerics;

namespace ShapleyTreewidth
{
    class Program
    {
        static void Main(string[] args)
        {
            ///*
            Graph g = Graph.ParsePACE2016(new StreamReader("../../../../instances/911network.gr"));
            TreeDecomposition td = TreeDecomposition.Parse(new StreamReader("../../../../instances/911network.td"), g);//*/

            /*
            StreamReader sr = new StreamReader("../../../../instances/pace/instance070.gr");
            Graph g = Graph.ParsePACE2018(sr);
            TreeDecomposition td = TreeDecomposition.Parse(sr, g);//*/

            ShapleyAlgorithm algo = new ShapleyAlgorithm();
            Dictionary<Tuple<TDNode, TDNode>, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>> lookup = new Dictionary<Tuple<TDNode, TDNode>, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // We need to run the computation with different bags as root
            // Such that each vertex occurs in at least one such root bag
            // Greedily find a set cover
            Dictionary<Vertex, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>> results = new Dictionary<Vertex, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>>();
            PriorityQueue<TDNode> pq = new PriorityQueue<TDNode>();

            pq.Enqueue(td.Nodes.First((z) => z.Bag.Contains(g.Vertices[0])), 1);

            foreach (TDNode t in td.Nodes)
                pq.Enqueue(t, -t.Bag.Length);

            while (!pq.IsEmpty())
            {
                int count = (int)(-pq.PeekDist());
                TDNode cur = pq.Dequeue();
                int currentCount = cur.Bag.Where((v) => !results.ContainsKey(v)).Count();
                if (currentCount == 0) continue;
                if(currentCount != count)
                {
                    pq.Enqueue(cur, -currentCount);
                    continue;
                }

                Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> result = cur.Compute(algo, null, lookup);

                foreach (Vertex v in cur.Bag)
                    results[v] = result;

                Console.WriteLine(results.Count + " / " + g.Vertices.Length);
            }

            sw.Stop();

            BigInteger[][] IncludingVertexCount = new BigInteger[g.Vertices.Length][], ExcludingVertexCount = new BigInteger[g.Vertices.Length][];
            BigInteger[][] IncludingVertexValue = new BigInteger[g.Vertices.Length][], ExcludingVertexValue = new BigInteger[g.Vertices.Length][];

            int n = g.Vertices.Length;

            for (int i = 0; i < n; i++)
            {
                Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> table = results[g.Vertices[i]];

                BigInteger[] includingCount = new BigInteger[n + 1], excludingCount = new BigInteger[n + 1];
                BigInteger[] includingValue = new BigInteger[n + 1], excludingValue = new BigInteger[n + 1];

                for (int j = 0; j <= n; j++)
                {
                    includingCount[j] = new BigInteger(0);
                    excludingCount[j] = new BigInteger(0);

                    includingValue[j] = new BigInteger(0);
                    excludingValue[j] = new BigInteger(0);
                }

                foreach (var (subset, subsetTable) in table)
                {
                    foreach (var (size, sizeTable) in subsetTable)
                    {
                        foreach (var (partition, partitionValue) in sizeTable) {
                            if (subset == 0 || partition.CountComponents(subset) == 1)
                            {
                                if ((subset & (1 << g.Vertices[i].Color)) != 0)
                                {
                                    //Console.WriteLine("Size {0} including {1} total number {2}", size, i, partitionValue.TotalNumber);
                                    includingCount[size] += partitionValue.TotalNumber;
                                    includingValue[size] += partitionValue.TotalValue;
                                }
                                else
                                {
                                    //Console.WriteLine("Size {0} excluding {1} total number {2}", size, i, partitionValue.TotalNumber);
                                    excludingCount[size] += partitionValue.TotalNumber;
                                    excludingValue[size] += partitionValue.TotalValue;
                                }
                            }
                         }
                    }
                }

                BigInteger unweighted = 0;
                BigInteger weighted = 0;

                for(int s = 1; s < n; s++)
                {
                    // How much can be gained by adding i to a coalition of size s?
                    BigInteger fac1 = Factorial(s), fac2 = Factorial(n - s- 1);
                    unweighted += (includingCount[s + 1] - (s == 1 ? 0 : excludingCount[s])) * fac1 * fac2;
                    weighted += (includingValue[s + 1] - excludingValue[s]) * fac1 * fac2;
                }

                unweighted *= 10000000000000000000;
                weighted *= 10000000000000000000;
                BigInteger nFac = Factorial(n);

                unweighted /= nFac;
                weighted /= nFac;

                Console.WriteLine("Vertex {0} unweighted: {1}, weighted: {2}", i + 1, Math.Round(((double)unweighted) / 10000000000000000000.0, 9), Math.Round(((double)weighted) / 10000000000000000000.0, 9));
            }

            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }

        static BigInteger Factorial(int n)
        {
            if (n <= 1)
                return 1;
            return n * Factorial(n - 1);
        }
    }
}
