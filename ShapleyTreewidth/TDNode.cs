using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ShapleyTreewidth
{
    class TDNode
    {
        public TreeDecomposition ParentDecomposition;
        public Vertex[] Bag; // Subset of at most (tw+1) vertices of ParentDecomposition.ParentGraph
        public List<TDNode> Adj = new List<TDNode>(); // Adjacent TDNodes
        public Vertex[] VertexByColor; // Invariant: for all v\in Bag, VertexByColor[v.Color]=v, if VertexByColor[...] is null there is no such vertex in Bag

        public HashSet<Edge> availableEdges = new HashSet<Edge>();
        public List<Edge> introduceEdges = new List<Edge>();

        public TDNode(int n, TreeDecomposition ParentDecomposition, int w)
        {
            this.ParentDecomposition = ParentDecomposition;
            Bag = new Vertex[n];
            VertexByColor = new Vertex[w];
        }

        public T Compute<T>(IDPAlgorithm<T> algorithm, TDNode parent, Dictionary<Tuple<TDNode, TDNode>, T> lookup)
        {
            T result;
            if (lookup.TryGetValue(Tuple.Create(this, parent), out result))
                return result;

            IEnumerable<TDNode> children = Adj.Where((n) => n != parent);

            if (!children.Any())
                result = algorithm.Introduce(this, Bag, algorithm.Leaf(ParentDecomposition.Width));
            else
            {
                TDNode first = children.First();

                result = AdjustBag(algorithm, first, first.Compute(algorithm, this, lookup));

                foreach (TDNode next in children.Skip(1))
                    result = algorithm.Join(this, result, AdjustBag(algorithm, next, next.Compute(algorithm, this, lookup)));
            }

            // Only cache if the result might be reused, prevents caching in all nodes of a long chain
            if(parent == null || parent.Adj.Count() > 2)
                lookup[Tuple.Create(this, parent)] = result;

            return result;
        }

        public T AdjustBag<T>(IDPAlgorithm<T> algorithm, TDNode child, T table)
        {
            Vertex[] toForget = child.Bag.Where((v) => !this.BagContains(v)).ToArray();
            Vertex[] toIntroduce = Bag.Where((v) => !child.BagContains(v)).ToArray();

            if (toForget.Length > 0)
                table = algorithm.Forget(this, toForget, table);

            if (toIntroduce.Length > 0)
                table = algorithm.Introduce(this, toIntroduce, table);

            return table;
        }

        public void ColorVertices(TDNode parent)
        {
            for (int i = 0; i < Bag.Length; i++)
            {
                if (Bag[i].Color >= 0)
                    VertexByColor[Bag[i].Color] = Bag[i];
            }

            int firstColor = 0;
            for (int i = 0; i < Bag.Length; i++)
            {
                if (Bag[i].Color < 0)
                {
                    while (VertexByColor[firstColor] != null)
                        firstColor++;
                    VertexByColor[firstColor] = Bag[i];
                    Bag[i].Color = firstColor;
                }
            }

            foreach (TDNode nb in Adj)
                if (nb != parent)
                    nb.ColorVertices(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BagContains(Vertex v)
        {
            return VertexByColor[v.Color] == v;
        }
    }
}
