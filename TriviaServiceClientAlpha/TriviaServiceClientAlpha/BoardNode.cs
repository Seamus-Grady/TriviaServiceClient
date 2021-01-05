using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriviaServiceClientAlpha
{
    public class BoardNode
    {
        public BoardNode(int type)
        {
            Type = type;
        }
        public BoardNode left { get; set; }
        public BoardNode right { get; set; }
        public BoardNode straight { get; set; }
        public BoardNode backwards { get; set; }
        public int Category { get; set; }
        public int position { get; set; }
        public int Type { get; set; }

        public int myType()
        {
            return Type;
        }
    }
    public class CenterNode : BoardNode
    {
        public CenterNode() : base (1) {}
        public BoardNode YellowPath { get; set; }
        public BoardNode GreenPath { get; set; }
        public BoardNode PinkPath { get; set; }
        public BoardNode OrangePath { get; set; }
        public BoardNode PurplePath { get; set; }
        public BoardNode BluePath { get; set; }
    }
}
