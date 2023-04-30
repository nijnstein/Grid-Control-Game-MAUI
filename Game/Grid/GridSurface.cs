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

        public int HighlightCountdown = 0;
        public Color HighlightColor = Colors.Yellow;
        public float HighlightStrokeSize = 3f;
        public Color GradientColor = Colors.White; 

        public PointF Centroid; 

        private PathF _path;
        private IPattern _pattern;
        private Microsoft.Maui.Graphics.IImage _image;
        private int _patternSize; 

        public bool IsHighlighted => HighlightCountdown > 0;

        public bool PatternFill = false;
        public bool GradientFill = true;

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

        static Color PrimaryColor = Color.FromRgba(34, 34, 34, 255);
        static Color[] PrimaryColors = new Color[]
        {
            PrimaryColor,
            PrimaryColor.Lerp(Colors.White, 0.4f),
            PrimaryColor.Lerp(Colors.Black, 0.2f),
            PrimaryColor.Lerp(Colors.White, 0.2f), 
            PrimaryColor.Lerp(Colors.Black, 0.1f), 
            PrimaryColor.Lerp(Colors.Black, 0.25f)
        };

        static Color SecondaryColor = Color.FromRgba(54, 54, 54, 255);
        static Color[] SecondaryColors = new Color[]
        {
            SecondaryColor,
            SecondaryColor.Lerp(Colors.White, 0.4f),
            SecondaryColor.Lerp(Colors.Black, 0.2f), 
            SecondaryColor.Lerp(Colors.White, 0.2f),
            SecondaryColor.Lerp(Colors.Black, 0.1f),
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
                _path = ToPathF(Offset - 1, false);
            }

            if (PatternFill)
            {
                if (_pattern == null || IsHighlighted)
                {
                    LinearGradientBrush brush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1)
                    };
                    brush.GradientStops.Add(new GradientStop(FillColor, 0));
                    brush.GradientStops.Add(new GradientStop(GradientColor, 0.4f));
                    brush.GradientStops.Add(new GradientStop(FillColor, 1f));

                    float size = 5;
                    using (PictureCanvas picture = new PictureCanvas(0, 0, size, size))
                    {
                        picture.SetFillPaint(brush, Game.ViewRectangle);
                        picture.FillRectangle(1, 1, size - 1, size - 1);
                        
                        _pattern = new PicturePattern(picture.Picture, size, size);
                    }
                }
                canvas.SetFillPattern(_pattern);
            }
            else
            if (GradientFill)
            {
                LinearGradientBrush brush = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                brush.GradientStops.Add(new GradientStop(FillColor, 0));
                brush.GradientStops.Add(new GradientStop(GradientColor, 0.4f));
                brush.GradientStops.Add(new GradientStop(FillColor, 1f));
                canvas.SetFillPaint(brush, Game.ViewRectangle);
                canvas.SetShadow(new SizeF(2, 2), 3, Color.FromRgba(FillColor.Red * .5f, FillColor.Green * .5f, FillColor.Blue * .5f, FillColor.Alpha));
            }
            else
            {
                canvas.FillColor = FillColor;
                canvas.SetShadow(new SizeF(2, 2), 3, Color.FromRgba(FillColor.Red * .5f, FillColor.Green * .5f, FillColor.Blue * .5f, FillColor.Alpha));
            }

            canvas.FillPath(_path, WindingMode.NonZero);

            if (!(PatternFill || GradientFill))
            {
                if (StrokeSize > 0)
                {
                    canvas.StrokeColor = StrokeColor;
                    canvas.StrokeSize = 1;
                    canvas.StrokeDashPattern = null;
                    canvas.SetShadow(new SizeF(1, 1), 3, Color.FromRgba(StrokeColor.Red * .5f, StrokeColor.Green * .5f, StrokeColor.Blue * .5f, StrokeColor.Alpha));
                    canvas.DrawPath(_path);
                }
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
