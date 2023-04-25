using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{
    public class GridActor : GameObject
    {
        public Color Color = Colors.Crimson;
        public readonly Grid Grid;

        public int A;
        public int B;

        /// <summary>
        /// position of actor between a and b seen from a as graphunits 
        /// </summary>
        public float ABPosition;

        /// <summary>
        /// speed of actor in graph units 
        /// </summary>
        public float Speed = 30f; 


        public SizeF Size = new Size(Game.CanvasWidth / GridGame.GridWidth * 2, Game.CanvasHeight / GridGame.GridHeight * 2);
        public bool IsOnSegment => A >= 0 & B >= 0;
        public bool IsOnPoint => A >= 0 & B == -1;

        public GridActor(GameObject parent, Grid grid) : base(parent)
        {
            Grid = grid; 
        }

        /// <summary>
        /// set position in canvas from location in grid 
        /// </summary>
        /// <param name="a">point a in graph: 'starting point'</param>
        /// <param name="b">endpoint b in graph</param>
        /// <param name="distance">distance in graphunits from a in direction of b</param>
        public void SetPosition(int a, int b, float distance)
        {
#if DEBUG
            // verify B is a connection of A
            if (a < 0 || a >= Grid.Points.Count) throw new Exception($"actor.setposition: point index A, {a} is out of range 0..{Grid.Points.Count}");

            GridConnection c = Grid.Connections[a];
            if (c.Index != a) throw new Exception($"actor.setposition: index mismatch, at index {a} pointindex {c.Index} was found ");
            if (b >= 0 && !c.ConnectsWith(b)) throw new Exception($"actor.setposition: point at index {b} not part of connectionlist of point {a}");
            if (b == -1 && distance != 0) throw new Exception($"actor.setposition: if setting position onpoint to index {a} distance must be 0");
            if (b >= 0 && distance == 0) throw new Exception($"actor.setposition: if setting position onsegment from {a} to {b} distance must be between bounds");

#endif
            A = a;
            B = b;
            ABPosition = (b == -1) ? 0 : distance;

            UpdatePosition();
        }

        public void SetPosition(int a) => SetPosition(a, -1, 0);
        
        /// <summary>
        /// if moving onSegment, check if we reached a point and should toggle into IsOnPoint 
        /// </summary>
        protected virtual void CheckSegmentPosition()
        {
            // if moving onSegment after updating, check if we hit a point (even if starting from onPoint, speed might be high due to delta)
            if (IsOnSegment)
            {
                if (ABPosition == 0)
                {
                    // on point A 
                    B = -1;
                }
                else
                if (ABPosition >= GridPoint.Distance(Grid.Points[A], Grid.Points[B]))
                {
                    // only if point B is not the last point in the path 
                    // - this happens growing the last path segment on creating a surface 
                    if (Grid.CurrentPath.Count == 0 || (B != Grid.CurrentPath[Grid.CurrentPath.Count - 1]))
                    {
                        // on point B 
                        A = B;
                        B = -1;
                        ABPosition = 0;
                    }
                }
            }
        }

        /// <summary>
        /// update canvas position form A, B and the distance travelled between them 
        /// </summary>
        protected void UpdatePosition()
        {
            GridPoint a = Grid.Points[A];

            if (IsOnSegment)
            {
                // set position from location in graph
                GridPoint b = Grid.Points[B];

                switch (GridPoint.GetConnectionType(a, b))
                {
                    case Direction.Left:
                        {
                            float diff = a.X - b.X;
                            float fraction = ABPosition / diff;// ABposition is given in graph units, normalize to a fraction
                            Position = GridToCanvas(new PointF(a.X - diff * fraction, a.Y));
                            break;
                        }

                    case Direction.Right:
                        {
                            float diff = b.X - a.X;
                            float fraction = ABPosition / diff;// ABposition is given in graph units, normalize to a fraction
                            Position = GridToCanvas(new PointF(a.X + diff * fraction, a.Y));
                            break;
                        }

                    case Direction.Top:
                        {
                            float diff = a.Y - b.Y;
                            float fraction = ABPosition / diff;
                            Position = GridToCanvas(new PointF(a.X, a.Y - diff * fraction));
                            break;
                        }

                    case Direction.Bottom:
                        {
                            float diff = b.Y - a.Y;
                            float fraction = ABPosition / diff;
                            Position = GridToCanvas(new PointF(a.X, a.Y + diff * fraction));
                            break;
                        }
                }
            }
            else
            if(IsOnPoint)
            {
                Position = GridToCanvas(new PointF(a.X, a.Y));
            }
        }

        protected virtual MoveState MoveAlongConnection(float deltaTime, GridPoint a, GridPoint b, Direction connectionDirection, Direction input)
        {
            // + or - depends on connection type of ABposition and the movement direction 
            float v = connectionDirection == Direction.Left || connectionDirection == Direction.Top
                ? -Speed
                : Speed;

            v = input == Direction.Left | input == Direction.Up
                ? -v
                : v;

            ABPosition += (v * deltaTime);

            // bound ABPosition by point a and b 
            ABPosition = ABPosition.Clip(0f, GridPoint.Distance(a, b, connectionDirection));
            return MoveState.Moved;
        }

        protected virtual MoveState MoveAlongConnection(float deltaTime, int a, int b)
        {
            if (a >= 0 && b >= 0)
            {
                GridPoint A = Grid.Points[a];
                GridPoint B = Grid.Points[b];
                Direction dir = GridPoint.GetConnectionType(A, B); 
                return MoveAlongConnection(deltaTime, A, B, dir, dir);
            }
            return MoveState.None; 
        }



        /// <summary>
        /// transform grid cooordinates into canvas cooordinates 
        /// </summary>
        public PointF GridToCanvas(PointF p)
        {
            return new PointF(p.X * Grid.RSizeX * Game.CanvasWidth, p.Y * Grid.RSizeY * Game.CanvasHeight);
        }

        /// <summary>
        /// update physics for this actor 
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }

        /// <summary>
        /// render this actor 
        /// </summary>
        public override void Render(ICanvas canvas, RectF dirty)
        {
            base.Render(canvas, dirty);

            canvas.FillColor = Color; 
            canvas.FillCircle(PointToView(Position), 5); 
        }
    }
}
