using Microsoft.Maui.Animations;
using System.Numerics;

namespace NSS.GameObjects
{
    public class PhysicsGameObject: GameObject
    {
        public Vector2 P1;
        public Vector2 P2;
        public Vector2 LinearVelocity; 
        public float RadialVelocity;

        public Color Color = Colors.Yellow;
        public float StrokeSize = 1f; 
        
        protected float updateTime = 0;
        protected float updateInterval = 1f / 60f;

        public PhysicsGameObject(GameObject parent, Vector2 p1, Vector2 p2, Vector2 linearVelocity, float radialVelocity)
            : base(parent)
        {
            P1 = p1;
            P2 = p2;
            Position = new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
            Rotation = MathEx.AngleFromCenter(Position, P1);
            LinearVelocity = linearVelocity;
            RadialVelocity = radialVelocity;
            
            Color = Color.Lerp(Colors.LightYellow, Random.Shared.NextSingle());
            StrokeSize += Random.Shared.NextSingle() * 0.4f;
        }


        public override void Update(float deltaTime)
        {
            // limit updates to once per render frame
            // - debree has no interaction with environment 
            // - saves 5x performance 
            updateTime += deltaTime;
            if(updateTime > updateInterval)
            {
                updateTime -= updateInterval;
                deltaTime = updateInterval; 
            }
            else
            {
                return; 
            }
                           

            // update position and rotation 
            Position = new PointF(
                Position.X + LinearVelocity.X * deltaTime, 
                Position.Y + (LinearVelocity.Y + Game.Gravity) * deltaTime);
            
            Rotation += RadialVelocity * deltaTime;
            if (Rotation > 360f) Rotation -= 360f;
            if (Rotation < 0f) Rotation += 360f; 
                            
            // calculate new endpoints as position is center point of line 
            float length = Vector2.Distance(P1, P2); 
            MathEx.LineSegmentPointsFromAngle(Position, Rotation, length, out P1, out P2);
            return ;
        }

        public static Color Darken(Color c, float factor)
        {
            return new Color(c.Red * factor, c.Green * factor, c.Blue * factor, c.Alpha);
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color;
            canvas.StrokeSize = StrokeSize;
            canvas.DrawLine(PointToView(P1), PointToView(P2));
        }

    }
}
