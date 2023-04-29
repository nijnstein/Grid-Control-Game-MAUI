﻿using NSS.GameObjects;
using Font = Microsoft.Maui.Graphics.Font;

namespace Grid.GameObjects
{
    public class PopupText : GameObject
    {
        public PointF TextAlignment;

        public Font Font;
        public float FontSize;
        public Color FontColor;

        public Color FillColor;
        public Color GradientColor; 

        public string Text;

        public PopupText(GameObject parent, string text, float size, bool bold, RectF region, PointF alignment, Color color) : base(parent) 
        {
            Text = text;
            FontSize = size;
            Font = bold ? Font.Default : Font.DefaultBold;
            FontColor = color;

            FillColor = Color.FromRgba(1 - color.Red, 1 - color.Green, 1 - color.Blue, color.Alpha);
            GradientColor = Color.FromRgba(FillColor.Red, MathF.Cbrt(MathF.Max(0.2f, FillColor.Green)), FillColor.Blue, color.Alpha);

            Position = new PointF(region.X + region.Width / 2, region.Y + region.Height / 2);
            Extent = new PointF(region.Width / 2, region.Height / 2);
            TextAlignment = alignment; 
        }


        public override void PostRender(ICanvas canvas, Rect dirtyRect)
        {
            if (FontSize > 0f)
            {
                PointF p = PointToView(Position);
                PointF e = PointToView(Extent);

                SizeF min = canvas.GetStringSize(Text, Font, FontSize);

                e = new PointF(min.Width * 0.8f, min.Height * 0.8f);

                float x = MathF.Max(0, p.X - e.X);
                float y = MathF.Max(0, p.Y - e.Y);
                float w = e.X * 2;
                float h = e.Y * 2; 

                canvas.FillColor = FillColor;

                LinearGradientBrush brush = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                brush.GradientStops.Add(new GradientStop(FillColor, 0));
                brush.GradientStops.Add(new GradientStop(GradientColor, 0.4f));
                brush.GradientStops.Add(new GradientStop(FillColor, 1f));

                RectF rc = new RectF(x, y, w, h); 
                canvas.SetFillPaint(brush, rc);
                canvas.FillRoundedRectangle(rc, min.Height * 0.3f);

                canvas.Font = Font;
                canvas.FontSize = FontSize;
                canvas.FontColor = FontColor;

                canvas.DrawString(
                    Text,
                    x, y, w, h,
                    TextAlignment.X == 0 ? HorizontalAlignment.Center : (TextAlignment.X < 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right),
                    TextAlignment.Y == 0 ? VerticalAlignment.Center : (TextAlignment.Y < 0 ? VerticalAlignment.Top : VerticalAlignment.Bottom)
                    );
            }
        }
    }
}