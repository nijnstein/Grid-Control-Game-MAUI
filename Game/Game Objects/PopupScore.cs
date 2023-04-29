﻿using NSS.GameObjects;

namespace Grid.GameObjects
{
    public class PopupScore : PopupText
    {
        private float BaseFontSize;

        public PopupScore(GameObject parent, string text, float size, bool bold, RectF region, PointF alignment, Color color) 
            : base(parent, text, size, bold, region, alignment, color)
        {
            BaseFontSize = size; 

            // popin
            new ValueAnimation(Parent, (float step) =>
            {
                FontSize = 0.2f + (1f - step) * BaseFontSize;
            }, 1, StepFunction.Linear2Ways, true)
            // then vanish
            .OnStop(() =>
            {
                BaseFontSize = FontSize;

                return new ValueAnimation(Parent, (float step) =>
                {
                    float s = step - 0.7f; // delay
                    if (s > 0)
                    {
                        FontSize = (1f - (s * 3.33f)) * BaseFontSize;
                    }
                }, 5f, StepFunction.Linear, false, true);
            });

        }

        public override void PostRender(ICanvas canvas, Rect dirtyRect)
        {
            base.PostRender(canvas, dirtyRect);
        }
    }
}
