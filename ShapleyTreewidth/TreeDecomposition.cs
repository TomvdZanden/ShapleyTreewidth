using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ShapleyTreewidth
{
    class TreeDecomposition
    {
        public List<TDNode> Nodes;
        public int Width;
        public Graph ParentGraph;

        public TreeDecomposition(int n, int w)
        {
            if (w > 32) throw new ArgumentException("Only graphs up to treewidth 31 (bag size 32) are supported.");

            Nodes = new List<TDNode>(n);
            for (int i = 0; i < n; i++) Nodes.Add(null);
            Width = w;
        }

        // Parses a tree decomposition for a graph g, reading the input from a specific streamreader sr
        // Format: https://pacechallenge.wordpress.com/pace-2016/track-a-treewidth/
        public static TreeDecomposition Parse(TextReader sr, Graph g)
        {
            TreeDecomposition td = null;

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                string[] cf = line.Split();
                if (cf[0] == "s")
                {
                    td = new TreeDecomposition(int.Parse(cf[2]), int.Parse(cf[3]));
                }
                else if (cf[0] == "b")
                {
                    TDNode newNode = new TDNode(cf.Length - 2, td, td.Width);
                    for (int i = 2; i < cf.Length; i++)
                        newNode.Bag[i - 2] = g.Vertices[int.Parse(cf[i]) - 1];
                    td.Nodes[int.Parse(cf[1]) - 1] = newNode;
                }
                else
                {
                    try
                    {
                        int a = int.Parse(cf[0]);
                        int b = int.Parse(cf[1]);
                        td.Nodes[a - 1].Adj.Add(td.Nodes[b - 1]);
                        td.Nodes[b - 1].Adj.Add(td.Nodes[a - 1]);
                    }
                    catch
                    { }
                }
            }

            td.ParentGraph = g;

            // Find a (tw+1)-coloring for g
            td.Nodes[0].ColorVertices(null);

            Console.WriteLine("Bags: {0} - Join Bags: {1} - Width: {2} - Vertices: {3}", td.Nodes.Count, td.Nodes.Where((n) => n.Adj.Count > 2).Count(), td.Width, g.Vertices.Length);

            return td;
        }
    }
}
