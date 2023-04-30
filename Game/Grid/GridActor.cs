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

        /// <summary>
        /// move player on an existing segment  
        /// </summary>
        protected virtual MoveState MoveOnConnection(Direction moveDirection, float deltaTime)
        {
            // move along the segment in direction of input 
            GridPoint a = Grid.Points[A];
            GridPoint b = Grid.Points[B];
            Direction connectionType = GridPoint.GetConnectionType(a, b);

            // check if the movement direction is valid for the given connection 
            PointF vc = connectionType.ToPointF();
            PointF vinput = moveDirection.ToPointF();

            if (vc.X != 0 && moveDirection.IsUp())
            {
                if (  /* bound check     */ a.Y < 0
                    //& /* fill check      */ 
                    //& /* snap2grid check */ 
                    )
                {
                    return MoveState.Blocked;
                }
            }
            else
            if (vc.X != 0 && moveDirection.IsDown())
            {
                if (a.Y > (Grid.SizeY - 1)) return MoveState.Blocked;
            }
            else
            if (vc.Y != 0 && moveDirection.IsLeft())
            {
                if (a.X < 0) return MoveState.Blocked;
            }
            else
            if (vc.Y != 0 && moveDirection.IsRight())
            {
                if (a.X > (Grid.SizeX - 1)) return MoveState.Blocked;
            }
            else
            {
                return this.MoveAlongConnection(deltaTime, a, b, connectionType, moveDirection);
            }

            return MoveState.None;
        }


        protected virtual MoveState MoveAlongConnection(float deltaTime, GridPoint a, GridPoint b, Direction connectionDirection, Direction inputDirection)
        {
            // + or - depends on connection type of ABposition and the movement direction 
            float v = connectionDirection == Direction.Left || connectionDirection == Direction.Top
                ? -Speed
                : Speed;

            v = inputDirection == Direction.Left | inputDirection == Direction.Up
                ? -v
                : v;

            ABPosition += (v * deltaTime);

            float distance = GridPoint.DistanceToNeighbour(a, b, connectionDirection);

            // bound ABPosition by point a and b 
            // - effectively this means that our movement is not 100% smooth as we 'stop' at
            //   each point
            if (ABPosition <= 0)
            {
                ABPosition = 0;
                B = -1;
            }
            else
            if (ABPosition >= distance)
            {
                ABPosition = 0;
                A = B;
                B = -1;
            }

            return MoveState.Moved;
        }

        protected virtual MoveState MoveAlongConnection(float deltaTime, int a, int b)
        {
            if (a >= 0 && b >= 0)
            {
                GridPoint A = Grid.Points[a];
                GridPoint B = Grid.Points[b];
                GridConnection aC = Grid.Connections[a];
                Direction dir = GridPoint.GetConnectionType(A, B);

                if (!aC.ConnectsWith(b))
                {
                    // most probably the connection got severed by the player making new segments 
                    GridConnection bC = Grid.Connections[b];

                    if (aC.TryGetConnected(dir, out int nA)
                        && 
                        bC.TryGetConnected(dir.Opposite(), out int nB)
                        && 
                        nA == nB)
                    {
                        // nA/nB was injected, now check if we are between A and n or n and B 
                        float d = GridPoint.DistanceToNeighbour(A, Grid.Points[nA]);
                        if (ABPosition < d)
                        {
                            this.A = a;
                            this.B = nA; 
                            return MoveState.Moved;
                        }
                        else
                        if (ABPosition > d)
                        {
                            this.A = nA;
                            this.B = b;
                            return MoveState.Moved;
                        }
                        else
                        {
                            this.A = nA;
                            this.B = -1;
                            this.ABPosition = 0;
                            return MoveState.Moved; 
                        }
                    }
                }

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
            canvas.SetShadow(new SizeF(1, 1), 3, Color.FromRgba(Color.Red * .5f, Color.Green * .5f, Color.Blue * .5f, Color.Alpha)); 

            canvas.FillCircle(PointToView(Position), 5); 
        }
    }
}
