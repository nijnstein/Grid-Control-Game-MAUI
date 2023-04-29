using Microsoft.Maui.Animations;
using NSS;
using NSS.GameObjects;
using System.ComponentModel.DataAnnotations;
using Font = Microsoft.Maui.Graphics.Font;

namespace Grid.GameObjects
{

    public class GridSurface : GameObject
    {
        public Grid Grid; 
        public int[] Points;
        public bool CCW;

        // surface as percentage 0...1 of total area
        public float Surface;

        // the score given when creating this surface 
        public float Score; 

        public float Offset = -4f;
        public Color FillColor = Colors.Gray;
        public Color StrokeColor = Colors.Gray;
        public float StrokeSize = 1f;

        public int HighlightCountdown = 20;
        public Color HighlightColor = Colors.Yellow;
        public float HighlightStrokeSize = 3f;

        public PointF Centroid; 

        private PathF _path;
        private IPattern _pattern;
        private Microsoft.Maui.Graphics.IImage _image;
        private int _patternSize; 

        public bool IsHighlighted => HighlightCountdown > 0;

        
        public GridSurface(GridGame parent, int[] points, bool ccw, [Range(0f, 1f)] float area, float score) : base(parent)
        {
#if DEBUG
            if (points == null || points.Length < 3 || points[0] != points[points.Length - 1])
            {
                throw new Exception("invalid polygon, must be valid and closed");
            }
#endif 

            Grid = parent.Grid;
            Surface = area;
            Score = score; 
            Points = points;
            CCW = ccw;

            int idx = Random.Shared.Next(PrimaryColors.Length);

            FillColor = PrimaryColors[idx];
            StrokeColor = PrimaryColors[idx]; 
            HighlightColor = _highlightColors[idx];

            PointF[] p = points.Select(x => Grid.Points[x].AsPointF).ToArray();
            RectF rc = MathEx.CalculateBoundingRectangle(p);

            this.Position = rc.Center;
            this.Extent = new PointF(rc.Width / 2, rc.Height / 2);
            this.Centroid = MathEx.CalculateCentroid(p);
            this._patternSize = (int)MathF.Min(20, (10f + 150 * area));

            // pop poly into view 
            new ValueAnimation(this, (float step) =>
            {
                Offset = -4 - (1f - step) * 20;
                _path = null;

            }, 1.5f, StepFunction.Linear, true);

            // seperate animation of color 
            new ValueAnimation(this, (float step) =>
            {
                FillColor = StrokeColor = PrimaryColors[idx].Lerp(SecondaryColors[idx], step);
                _pattern = null;
            }, 1.5f + Random.Shared.NextSingle() * 2.5f, StepFunction.Linear2Ways, false, true);
        }

        static Color PrimaryColor = Colors.Crimson;
        static Color[] PrimaryColors = new Color[]
        {
            PrimaryColor,
            PrimaryColor.Lerp(Colors.White, 0.4f),
            PrimaryColor.Lerp(Colors.Black, 0.2f),
            PrimaryColor.Lerp(Colors.White, 0.2f), 
            PrimaryColor.Lerp(Colors.Black, 0.1f), 
            PrimaryColor.Lerp(Colors.Red, 0.1f), 
            PrimaryColor.Lerp(Colors.Black, 0.25f)
        };

        static Color SecondaryColor = Colors.Red;
        static Color[] SecondaryColors = new Color[]
        {
            SecondaryColor,
            SecondaryColor.Lerp(Colors.White, 0.4f),
            SecondaryColor.Lerp(Colors.Black, 0.2f), 
            SecondaryColor.Lerp(Colors.White, 0.2f),
            SecondaryColor.Lerp(Colors.Black, 0.1f),
            SecondaryColor.Lerp(Colors.Red, 0.1f), 
            SecondaryColor.Lerp(Colors.Black, 0.25f)
        };

        static Color[] _highlightColors = PrimaryColors.Select(x => new Color(MathF.Sqrt(x.Red), MathF.Sqrt(x.Green), MathF.Sqrt(x.Blue), x.Alpha)).ToArray();

        PathF ToPathF(float offset, bool noise = false)
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
                _path = ToPathF(Offset, false);
            }

            if (_pattern == null || IsHighlighted)
            {
                Color color = IsHighlighted ? HighlightColor : FillColor;
                using (PictureCanvas picture = new PictureCanvas(0, 0, _patternSize, _patternSize))
                {
                    picture.StrokeColor = color;
                    picture.DrawLine(0, 0, _patternSize, _patternSize);
                    picture.DrawLine(0, _patternSize, _patternSize, 0);
                    _pattern = new PicturePattern(picture.Picture, _patternSize, _patternSize);
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

        public void InsertPoint(int pointIndex, int atIndex)
        {
            if(this.Points == null)
            {
                this.Points = new int[] { pointIndex }; 
            }

            Span<int> n = new int[Points.Length + 1];
            Span<int> s = Points.AsSpan(); 

            if (atIndex > 0)
            {
                s.Slice(0, atIndex).CopyTo(n);
                n[atIndex] = pointIndex;
                s.Slice(atIndex).CopyTo(n.Slice(atIndex + 1));
            }
            else
            {
                n[0] = pointIndex;
                s.CopyTo(n.Slice(1)); 
            }

            Points = n.ToArray();
        }
    }
}
