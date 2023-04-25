using Microsoft.Maui.Animations;
using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{

    public class GridSurface : GameObject
    {
        public Grid Grid; 
        public int[] Points; 
        public bool CCW;
        public float Surface;

        public float Offset = -4f;
        public Color FillColor = Colors.Gray;

        public Color StrokeColor = Colors.Gray;
        public float StrokeSize = 1f;

        public int HighlightCountdown = 20;
        public Color HighlightColor = Colors.Yellow;
        public float HighlightStrokeSize = 3f; 

        private PathF _path;
        private IPattern _pattern;
        private Microsoft.Maui.Graphics.IImage _image; 

        public bool IsHighlighted => HighlightCountdown > 0;

        public GridSurface(GridGame parent, int[] points, bool ccw, float area) : base(parent)
        {
            Grid = parent.Grid;
            Surface = area;
            Points = points;
            CCW = ccw;

            int idx = Random.Shared.Next(_colors.Length);

            FillColor = _colors[idx];
            StrokeColor = _colors[idx]; 
            HighlightColor = _highlightColors[idx];
         }

        static Color _baseColor = Colors.Crimson;
        static Color[] _colors = new Color[]
        {
            _baseColor, _baseColor.Lerp(Colors.White, 0.4f), _baseColor.Lerp(Colors.Black, 0.2f), _baseColor.Lerp(Colors.White, 0.2f), _baseColor.Lerp(Colors.Black, 0.1f), _baseColor.Lerp(Colors.Red, 0.1f), _baseColor.Lerp(Colors.Black, 0.25f)
        };
        static Color[] _highlightColors = _colors.Select(x => new Color(MathF.Sqrt(x.Red), MathF.Sqrt(x.Green), MathF.Sqrt(x.Blue), x.Alpha)).ToArray();

        PathF ToPathF(float offset)
        {
            // the scaling from grid to the internal canvas (which is later transformed to viewspace) 
            float gx = (1f / (float)Grid.SizeX) * GridGame.CanvasWidth;
            float gy = (1f / (float)Grid.SizeY) * GridGame.CanvasHeight;

            PointF[] points = new PointF[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                GridPoint gp = Grid.Points[Points[i]];
                points[i] = Game.PointToView(new PointF(gp.X * gx, gp.Y * gy));
            }

            if (offset != 0)
            {
                points = MathEx.OffsetPolygon(points, CCW ? offset : -offset, 1);
            }

            PathF path = new PathF(points[0]);
            for (int i = 1; i < Points.Length; i++)
            {
                path.LineTo(points[i]);
            }

            return path;
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            //
            // TODO:  should invalidate path if the game is resized 
            //
            if (_path == null)
            {
                _path = ToPathF(Offset);
            }

            if (_pattern == null || IsHighlighted)
            {
                Color color = IsHighlighted ? HighlightColor : FillColor;
                using (PictureCanvas picture = new PictureCanvas(0, 0, 10, 10))
                {
                    picture.StrokeColor = color;
                    picture.DrawLine(0, 0, 10, 10);
                    picture.DrawLine(0, 10, 10, 0);
                    _pattern = new PicturePattern(picture.Picture, 10, 10);
                }
            }

            canvas.SetFillPattern(_pattern);
            canvas.FillPath(_path, WindingMode.NonZero);

            if (StrokeSize > 0)
            {
                if (IsHighlighted)
                {
                    canvas.StrokeColor = HighlightCountdown % 2 == 0 ? StrokeColor : HighlightColor;
                    canvas.StrokeSize = 3;
                }
                else
                {
                    canvas.StrokeColor = StrokeColor;
                    canvas.StrokeSize = 1;
                }
                canvas.StrokeDashPattern = null;
                canvas.DrawPath(_path);
            }

            if (IsHighlighted)
            {
                HighlightCountdown--;
                _pattern = null;
            }
        }
    }
}
