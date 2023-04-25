using NSS;

namespace Grid.GameObjects
{
    public struct GridPoint
    {
        public int Index; 
        public int X;
        public int Y; 

        public GridPoint(int index, int x, int y)
        {
            Index = index; 
            X = x;
            Y = y; 
        }

        public PointF AsPointF => new PointF(X, Y);

        public static Direction GetConnectionType(GridPoint p1, GridPoint p2)
        {
            if(p1.X == p2.X)
            {
                if (p1.Y > p2.Y) return Direction.Top; // p2 is to top of p1 
                return Direction.Bottom; 
            }
            else
            if(p1.X < p2.X)
            {
                return Direction.Right;
            }
            else
            {
                return Direction.Left; 
            }

#if DEBUG
            throw new Exception("points may not be equal");
#else
            return GridConnectionType.Left; 
#endif
        }

        public static float Distance(GridPoint a, GridPoint b) => Distance(a, b, GetConnectionType(a, b)); 
        

        /// <summary>
        /// distance to directly connected point 
        /// </summary>
        public static float Distance(GridPoint a, GridPoint b, Direction connectionType)
        {
            switch(connectionType)
            {
                default: 
                case Direction.Left: return a.X - b.X;
                case Direction.Right: return b.X - a.X;
                case Direction.Top: return a.Y - b.Y;
                case Direction.Bottom: return b.Y - a.Y; 
            }
        }
    }
}
