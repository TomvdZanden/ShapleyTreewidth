using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ShapleyTreewidth
{
    class Graph
    {
        public Vertex[] Vertices;
        public List<Edge> Edges;

        public Graph(int n)
        {
            Vertices = new Vertex[n];
            for (int i = 0; i < n; i++)
                Vertices[i] = new Vertex(i);
            Edges = new List<Edge>();
        }

        // Parses a graph in PACE 2016/2017 format (https://pacechallenge.wordpress.com/pace-2016/track-a-treewidth/)
        public static Graph ParsePACE2016(TextReader sr)
        {
            Graph G = null;

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                string[] cf = line.Split();
                if (cf[0] == "p")
                    G = new Graph(int.Parse(cf[2]));
                if (G != null)
                {
                    try
                    {
                        int weight = 0;
                        if(cf.Length >= 3) int.TryParse(cf[2], out weight);
                        G.AddEdge(int.Parse(cf[0]) - 1, int.Parse(cf[1]) - 1, weight);
                    }
                    catch { }
                }
            }

            return G;
        }

        // Parses a graph in PACE 2018 format (https://pacechallenge.wordpress.com/pace-2018/)
        public static Graph ParsePACE2018(TextReader sr)
        {
            Graph G = null;

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                string[] cf = line.Split();
                if (cf[0] == "Nodes")
                    G = new Graph(int.Parse(cf[1]));
                if (cf[0] == "E" && G != null)
                {
                    try
                    {
                        int weight = 0;
                        int.TryParse(cf[3], out weight);
                        G.AddEdge(int.Parse(cf[1]) - 1, int.Parse(cf[2]) - 1, weight);
                    }
                    catch { }
                }
            }

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                // Terminals don't matter
            }

            return G;
        }

        public void AddEdge(int a, int b, int w)
        {
            Vertices[a].Adj.Add(new Edge(Vertices[a], Vertices[b], w));
            Vertices[b].Adj.Add(new Edge(Vertices[b], Vertices[a], w));

            if (a < b)
                Edges.Add(new Edge(Vertices[a], Vertices[b], w));
            else
                Edges.Add(new Edge(Vertices[b], Vertices[a], w));
        }
    }
}
