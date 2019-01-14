using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapleyTreewidth
{
    public class Vertex
    {
        public int Color = -1; // Gives a (tw+1)-coloring of the graph
        public List<Edge> Adj = new List<Edge>();
        public int Id;
        public int Weight = 1;

        public Vertex(int Id)
        {
            this.Id = Id;
            Adj = new List<Edge>();
        }

        public override bool Equals(object obj)
        {
            Vertex oth = (Vertex)obj;
            return oth.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
