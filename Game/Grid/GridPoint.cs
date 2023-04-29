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
            return Direction.Left; 
#endif
        }

        /// <summary>
        /// distance on 1 axis directly to neighbour 
        /// </summary>
        public static float DistanceToNeighbour(GridPoint a, GridPoint b) => DistanceToNeighbour(a, b, GetConnectionType(a, b));

        /// <summary>
        /// flight distance 
        /// </summary>
        public static float EuclidianDistanceTo(GridPoint a, GridPoint b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        /// <summary>
        /// manhatten distance 
        /// </summary>
        public static float ManhattenDistanceto(GridPoint a, GridPoint b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); 

        /// <summary>
        /// distance to directly connected point 
        /// </summary>
        public static float DistanceToNeighbour(GridPoint a, GridPoint b, Direction connectionType)
        {
            switch(connectionType)
            {
                default: 
                case Direction.Left: 
                case Direction.Right: return MathF.Abs(b.X - a.X);
                case Direction.Top:
                case Direction.Bottom: return MathF.Abs(b.Y - a.Y); 
            }
        }
    }
}
