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
