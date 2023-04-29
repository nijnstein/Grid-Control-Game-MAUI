using SkiaSharp;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace NSS.GameObjects
{


    public abstract class GameObject : GameObjectMAUIBase 
    {
        private List<ValueAnimation> _animations = null;

        public GameObject Parent { get; set; } = null;
        public List<GameObject> Children { get; set; } = new List<GameObject>();

        public PointF Position { get; set; } = PointF.Zero;
        public PointF PreviousPosition { get; set; } = PointF.Zero;

        public float Rotation { get; set; }
        public float PreviousRotation { get; set; }

        public List<ValueAnimation> Animations 
        { 
            get
            {
                if (_animations == null) _animations = new List<ValueAnimation>(); 
                return _animations; 
            }                       
        }

        public bool HasAnimations => _animations != null && _animations.Count > 0; 

        public PointF Velocity { get; set; } = PointF.Zero;
        public PointF Extent { get; set; } = PointF.Zero;
        public RectF BoundingRect => new RectF(Position.X - Extent.X, Position.Y - Extent.Y, Extent.X * 2f, Extent.Y * 2f);
        public PointF TopLeft => new PointF(Position.X - Extent.X, Position.Y - Extent.Y);
        public PointF BottomLeft => new PointF(Position.X - Extent.X, Position.Y + Extent.Y);
        public PointF TopRight => new PointF(Position.X + Extent.X, Position.Y - Extent.Y);
        public PointF BottomRight => new PointF(Position.X + Extent.X, Position.Y + Extent.Y);
        public float Width => Extent.X * 2;
        public float Height => Extent.Y * 2;


        public GameObject(GameObject parent)
        {
            if (parent != null)
            {
                Parent = parent;
                Parent.Add(this);
            }
        }
        public GameObject()
        {
        }

        public Game Game
        {
            get
            {
                GameObject p = this; 
                while (p.Parent != null) p = p.Parent;
                return p as Game; 
            }
        }

        public void Add(params GameObject[] child)
        {
            foreach (GameObject go in child)
            {
                Children.Add(go);
                go.Parent = this;
            }
        }

        public virtual void Render(ICanvas canvas, RectF dirtyRect)
        {
        }

        public virtual void PostRender(ICanvas canvas, Rect dirtyRect)
        { 
        }

        public PointF PointToView(float x, float y) => PointToView(new PointF(x, y)); 

        public PointF PointToView(PointF p) =>
            new PointF(
                (p.X + Game.ShakeVector.X) * Game.ViewportXscaler + Game.ViewportMarginLeft,
                (p.Y + Game.ShakeVector.Y) * Game.ViewportYscaler + Game.ViewportMarginTop);
        public PointF UnshakenPointToView(float x, float y) => UnshakenPointToView(new PointF(x, y));

        public PointF UnshakenPointToView(PointF p) =>
            new PointF(
                p.X * Game.ViewportXscaler + Game.ViewportMarginLeft,
                p.Y * Game.ViewportYscaler + Game.ViewportMarginTop);

        public RectF RectToView(RectF rect) => new RectF(
                (rect.X + Game.ShakeVector.X) * Game.ViewportXscaler + Game.ViewportMarginLeft,
                (rect.Y + Game.ShakeVector.Y) * Game.ViewportYscaler + Game.ViewportMarginTop,
                rect.Width * Game.ViewportXscaler,
                rect.Height * Game.ViewportYscaler);

        public RectF UnshakenRectToView(RectF rect) => new RectF(
                rect.X * Game.ViewportXscaler + Game.ViewportMarginLeft,
                rect.Y * Game.ViewportYscaler + Game.ViewportMarginTop,
                rect.Width * Game.ViewportXscaler,
                rect.Height * Game.ViewportYscaler);

        public SizeF ScaleToView(SizeF size) => new Size(ScaleToViewHorizontal(size.Width), ScaleToViewVertical(size.Height));
        
        public float ScaleToViewHorizontal(float width) => width * Game.ViewportXscaler;
        
        public float ScaleToViewVertical(float height) => height * Game.ViewportYscaler;

        public virtual void Update(float deltaTime)
        {
            if(Velocity != PointF.Zero)
            {
                PointF newPosition = new PointF(
                    Position.X + Velocity.X * deltaTime,
                    Position.Y + Velocity.Y * deltaTime);

                PreviousPosition = Position; 
                Position = newPosition; 
            }
            else
            {
                PreviousPosition = Position; 
            }

            DoAnimations(deltaTime); 
        }

        public virtual void OnCollision(GameObject other, PointF intersection, PointF velocity)
        {

        }

        public virtual IEnumerable<Tuple<PointF, PointF>> GetBorders()
        {
            yield return new Tuple<PointF, PointF>(TopLeft, TopRight);
            yield return new Tuple<PointF, PointF>(TopRight, BottomRight);
            yield return new Tuple<PointF, PointF>(BottomRight, BottomLeft);
            yield return new Tuple<PointF, PointF>(BottomLeft, TopLeft);
        }

        public virtual void DoRender(ICanvas canvas, RectF dirtyRect)
        {
            Render(canvas, dirtyRect);
            foreach (GameObject go in Children)
            {
                go.DoRender(canvas, dirtyRect);
            }
        }

        public virtual void DoPostRender(ICanvas canvas, RectF dirtyRect)
        {
            PostRender(canvas, dirtyRect);
            foreach (GameObject go in Children)
            {
                go.DoPostRender(canvas, dirtyRect);
            }
        }

        public virtual void DoUpdate(float deltaTime)
        {
            int c = Children.Count;

            // dont update children if the game is paused (assumes game is root of all objects)
            if (Game.GameState == GameState.Play)
            {
                for (int i = 0; i < Children.Count;)
                {
                    GameObject go = Children[i];
                    go.Update(deltaTime);
                    if (c >= Children.Count)
                    {
                        i++;
                    }
                    else
                    {
                        c = Children.Count;
                    }
                }
            }
            Update(deltaTime);
        }

        public virtual void DoAnimations(float deltaTime)
        {
            if(HasAnimations)
            {
                foreach(ValueAnimation animation in Animations)
                {
                    animation.Update(deltaTime); 
                }
            }
        }
    }
}
