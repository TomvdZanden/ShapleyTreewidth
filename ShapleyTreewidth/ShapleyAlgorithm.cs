using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ShapleyTreewidth
{
    class SolutionValue
    {
        // Gives the total number and total value (respectively) of good subsets of G with this characteristic
        public BigInteger TotalNumber, TotalValue;
    }

    class ShapleyAlgorithm : IDPAlgorithm<Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>>
    {
        // Key of first dictionary: subset of vertices (S intersected X_t)
        // Key of second dictionary: size of subset (|S|)
        // Key of third dictionary: PartialSolution (represents partition)
        // Value held: number of subsets/total value of good subsets for given characteristic

        public Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> Forget(TDNode bag, Vertex[] vertices, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> table)
        {
            Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> result = new Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>();

            int forgetMask = 0;
            foreach (Vertex v in vertices)
                forgetMask |= (1 << v.Color);
            forgetMask = ~forgetMask;

            foreach (var (subset, subsetTable) in table)
            {
                int newset = subset & forgetMask;
                Dictionary<int, Dictionary<PartialSolution, SolutionValue>> newSubsetTable;
                if (!result.TryGetValue(newset, out newSubsetTable))
                {
                    newSubsetTable = new Dictionary<int, Dictionary<PartialSolution, SolutionValue>>();
                    result.Add(newset, newSubsetTable);
                }

                foreach (var (size, sizeTable) in subsetTable)
                {
                    Dictionary<PartialSolution, SolutionValue> newSizeTable;
                    if (!newSubsetTable.TryGetValue(size, out newSizeTable))
                    {
                        newSizeTable = new Dictionary<PartialSolution, SolutionValue>();
                        newSubsetTable.Add(size, newSizeTable);
                    }

                    foreach (var (partition, value) in sizeTable)
                    {
                        int oldCount = partition.CountComponents(subset);
                        int newCount = partition.CountComponents(newset);
                        if (oldCount != newCount && !(oldCount <= 1 && newCount == 0)) continue;

                        PartialSolution newPartition = new PartialSolution(partition, newset, subset);

                        SolutionValue newValue;
                        if(!newSizeTable.TryGetValue(newPartition, out newValue))
                        {
                            newSizeTable.Add(newPartition, new SolutionValue()
                            {
                                TotalNumber = value.TotalNumber,
                                TotalValue = value.TotalValue
                            });
                        }
                        else
                        {
                            newValue.TotalNumber = newValue.TotalNumber + value.TotalNumber;
                            newValue.TotalValue = newValue.TotalValue + value.TotalValue;
                        }
                    }
                }
            }

            return result;
        }

        public Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> Introduce(TDNode bag, Vertex[] vertices, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> table)
        {
            List<Vertex> toAdd = new List<Vertex>(vertices.Length);
            List<Edge> introEdges = new List<Edge>();

            Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> result = new Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>();

            for (int i = 0; i < (1 << vertices.Length); i++)
            {
                for (int j = 0; j < vertices.Length; j++)
                    if ((i & (1 << j)) != 0)
                        toAdd.Add(vertices[j]);

                int addMask = 0;
                foreach (Vertex v in toAdd)
                    addMask |= (1 << v.Color);

                int vertexValue = 0;

                foreach (Vertex v in toAdd)
                {
                    vertexValue += v.Weight;
                    foreach (Edge e in v.Adj)
                        if (bag.Bag.Contains(e.To) && !introEdges.Contains(e))
                            introEdges.Add(e);
                }

                foreach (var (subset, subsetTable) in table)
                {
                    int newset = subset | addMask;
                    Dictionary<int, Dictionary<PartialSolution, SolutionValue>> newSubsetTable = new Dictionary<int, Dictionary<PartialSolution, SolutionValue>>();

                    foreach (var (size, sizeTable) in subsetTable)
                    {
                        Dictionary<PartialSolution, SolutionValue> newSizeTable = new Dictionary<PartialSolution, SolutionValue>();

                        // Not connected check
                        if (subset == 0 && addMask != 0 && size > 0) continue;

                        foreach (var (partition, value) in sizeTable)
                        {
                            PartialSolution newPartition = new PartialSolution(partition);
                            foreach (Edge e in introEdges)
                                if ((newset & (1 << e.To.Color)) != 0)
                                    newPartition.Union(e.From.Color, e.To.Color);

                            BigInteger newTotalValue = value.TotalValue;
                            newTotalValue += value.TotalNumber * vertexValue;

                            SolutionValue newValue;
                            if (newSizeTable.TryGetValue(newPartition, out newValue))
                            {
                                newValue.TotalNumber += value.TotalNumber;
                                newValue.TotalValue += newTotalValue;
                            }
                            else
                            {
                                newSizeTable.Add(newPartition, new SolutionValue()
                                {
                                    TotalNumber = value.TotalNumber,
                                    TotalValue = newTotalValue
                                });
                            }
                        }

                        newSubsetTable.Add(size + toAdd.Count, newSizeTable);
                    }

                    result.Add(newset, newSubsetTable);
                }

                toAdd.Clear();
                introEdges.Clear();
            }

            return result;
        }

        public Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> Join(TDNode bag, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> left, Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> right)
        {
            Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> result = new Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>();

            foreach (var (subset, subsetTableLeft) in left)
            {
                Dictionary<int, Dictionary<PartialSolution, SolutionValue>> subsetTableRight;
                if (!right.TryGetValue(subset, out subsetTableRight)) throw new Exception(); // This should never happen for this problem!
                Dictionary<int, Dictionary<PartialSolution, SolutionValue>> joinSubsetResult = new Dictionary<int, Dictionary<PartialSolution, SolutionValue>>();

                int adjustWeight = 0;
                foreach (Vertex v in bag.Bag)
                    if ((subset & (1 << v.Color)) != 0)
                        adjustWeight -= v.Weight;

                foreach (var (leftSize, leftSizeTable) in subsetTableLeft)
                    foreach (var (rightSize, rightSizeTable) in subsetTableRight)
                    {
                        int newSize = leftSize + rightSize - subset.BitCount();
                        Dictionary<PartialSolution, SolutionValue> newSizeTable;
                        if (!joinSubsetResult.TryGetValue(newSize, out newSizeTable))
                        {
                            newSizeTable = new Dictionary<PartialSolution, SolutionValue>();
                            joinSubsetResult.Add(newSize, newSizeTable);
                        }

                        // Not connected check
                        if (leftSize != 0 && rightSize != 0 && subset == 0) continue;

                        foreach(var (partitionLeft, valueLeft) in leftSizeTable)
                            foreach (var (partitionRight, valueRight) in rightSizeTable)
                            {
                                PartialSolution newPartition = new PartialSolution(partitionLeft);
                                for (int i = 0; i < newPartition.UnionFind.Length; i++)
                                    newPartition.Union(i, partitionRight.Find(i));

                                BigInteger totalNumber = valueLeft.TotalNumber * valueRight.TotalNumber;

                                SolutionValue newValue;
                                if (!newSizeTable.TryGetValue(newPartition, out newValue))
                                {
                                    newValue = new SolutionValue()
                                    {
                                        TotalNumber = totalNumber,
                                        TotalValue = valueLeft.TotalValue * valueRight.TotalNumber
                                    };
                                    newValue.TotalValue += valueRight.TotalValue * valueLeft.TotalNumber;
                                    newValue.TotalValue += totalNumber * adjustWeight;
                                    newSizeTable.Add(newPartition, newValue);
                                }
                                else
                                {
                                    newValue.TotalNumber += totalNumber;

                                    newValue.TotalValue += valueLeft.TotalValue * valueRight.TotalNumber;
                                    newValue.TotalValue += valueRight.TotalValue * valueLeft.TotalNumber;
                                    newValue.TotalValue += totalNumber * adjustWeight;
                                }
                            }
                    }

                result.Add(subset, joinSubsetResult);
            }

            return result;
        }

        public Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> Leaf(int width)
        {
            Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>> result = new Dictionary<int, Dictionary<int, Dictionary<PartialSolution, SolutionValue>>>
            {
                // Empty subset
                [0] = new Dictionary<int, Dictionary<PartialSolution, SolutionValue>>
                {
                    // Contains zero vertices
                    [0] = new Dictionary<PartialSolution, SolutionValue>()
                    {
                        [new PartialSolution(width)] = new SolutionValue() { TotalNumber = new BigInteger(1), TotalValue = new BigInteger(0) }
                    }
                }
            };

            return result;
        }
    }
}
