namespace NSS
{
    public enum Direction : byte
    {
        // in ccw order
        Top = 0,
        Up = Top, 
        
        Left = 1, 
        
        Bottom = 2, 
        Down = Bottom, 

        Right = 3,

        None = 255
    }

    public static class DirectionExtensions
    {
        public static bool IsLeft(this Direction dir) => dir == Direction.Left;
        public static bool IsUp(this Direction dir) => dir == Direction.Up;
        public static bool IsTop(this Direction dir) => dir == Direction.Top;
        public static bool IsRight(this Direction dir) => dir == Direction.Right;
        public static bool IsBottom(this Direction dir) => dir == Direction.Bottom;
        public static bool IsDown(this Direction dir) => dir == Direction.Down;
        public static bool IsVertical(this Direction dir) => dir == Direction.Up | dir == Direction.Down;
        public static bool IsHorizontal(this Direction dir) => dir == Direction.Left | dir == Direction.Right;
        public static bool AnyDir(this Direction dir) => dir != Direction.None;
        
        public static bool IsOppositeOf(this Direction dir, Direction other)
        {
            if( (dir.IsVertical() & other.IsVertical())
                | 
                (dir.IsHorizontal() & other.IsHorizontal()))
            {
                return dir != other; 
            }
            return false; 
        }

        public static Direction Opposite(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                case Direction.Top: return Direction.Bottom;
                case Direction.Bottom: return Direction.Top;
                default:
                case Direction.None: return Direction.None;
            }
        }

        public static PointF ToPointF(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Left: return new PointF(-1, 0);
                case Direction.Right: return new PointF(1, 0);
                case Direction.Top: return new PointF(0, -1);
                case Direction.Bottom: return new PointF(0, 1);
                case Direction.None:
                default: return new PointF(0, 0);                                 
            }
        }
    }
}
