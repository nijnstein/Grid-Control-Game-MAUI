using NSS.GameObjects;

namespace Grid.GameObjects
{
    public class GridSegment : GameObject
    {
        public Grid Grid;
        public int A;
        public int B;

        public Color Color = Colors.Crimson;
        public int StrokeSize = 3;

        public GridPoint PointA => Grid.Points[A];
        public GridPoint PointB => Grid.Points[B];

        public GridSegment(GridGame parent, int a, int b) : base(parent)
        {
            Grid = parent.Grid; 
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color;
            canvas.StrokeSize = StrokeSize;

            PointF a = PointToView(PointA.AsPointF);
            PointF b = PointToView(PointB.AsPointF);

            canvas.DrawLine(a, b); 
        }
    }
}
