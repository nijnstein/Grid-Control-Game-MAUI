namespace NSS
{
    public struct Size2D
    {
        public int X;
        public int Y; 
        public Size2D (int x, int y)
        {
            X = x;
            Y = y; 
        }
        public static Size2D operator +(Size2D a, Size2D b) => new Size2D(a.X + b.X, a.Y + b.Y);
        public static Size2D operator -(Size2D a, Size2D b) => new Size2D(a.X - b.X, a.Y - b.Y);
        public static Size2D operator *(Size2D a, Size2D b) => new Size2D(a.X * b.X, a.Y * b.Y);
        public static Size2D operator /(Size2D a, Size2D b) => new Size2D(a.X / b.X, a.Y / b.Y);
        public static Size2D operator *(Size2D a, int b) => new Size2D(a.X * b, a.Y * b);
        public static Size2D operator /(Size2D a, int b) => new Size2D(a.X / b, a.Y / b);
        public static Size2D operator +(Size2D a, int b) => new Size2D(a.X + b, a.Y + b);
        public static Size2D operator -(Size2D a, int b) => new Size2D(a.X - b, a.Y - b);
        public static Size2D operator -(Size2D a) => new Size2D(-a.X, -a.Y);

        public readonly static Size2D Zero = new Size2D(0, 0);
        public readonly static Size2D Ones = new Size2D(1, 1);
    }
}
