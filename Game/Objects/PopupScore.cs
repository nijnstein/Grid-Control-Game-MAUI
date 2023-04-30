using Microsoft.Maui.Animations;
using NSS.GameObjects;

namespace Grid.GameObjects
{
    public class PopupScore : GameText
    {
        private float BaseFontSize;

        private PointF startPosition; 
       
         

        public PopupScore(GameObject parent, string text, float size, bool bold, RectF region, PointF alignment, Color color) 
            : base(parent, text, size, bold, region, alignment, color, true, true)
        {
            BaseFontSize = size;

            GradientFillBackground = false;
            FillColor = Colors.Black;

            startPosition = Position;

            // popin
            new ValueAnimation(Parent, (float step) =>
            {
                FontSize = 0.2f + (1f - step) * BaseFontSize;
            }, 1, StepFunction.Linear2Ways, true)
            // then vanish into left corner 
            .OnStop(() =>
            {
                BaseFontSize = FontSize;

                return new ValueAnimation(Parent, (float step) =>
                {
                    float s = step - 0.7f; // delay
                    if (s > 0)
                    {
                        float f = s * 3.33f; 
                        FontSize = (1f - f) * BaseFontSize;

                        RectF rv = Game.ViewRectangle; 
                        Position = startPosition.Lerp(new PointF(30, rv.Bottom), f);
                    }


                }, 5f, StepFunction.Linear, false, true);
            });

        }
    
    }
}
