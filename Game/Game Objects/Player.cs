using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{

    public class Player : GridActor
    {
        private GridGame GridGame;
        private Direction LastDirection = Direction.None; 

        public Player(GridGame game, Grid grid) : base(game, grid)
        {
            GridGame = game;
            Color = Colors.LimeGreen;
        }


        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            MoveState moveState = MoveState.None;
            Direction inputDirection = (Game as GridGame).InputDirection;
            Direction direction = LastDirection; 

            if(direction == Direction.None)
            {
                direction = inputDirection; 
            }

            if (direction.AnyDir())
            {
                if (Grid.CurrentPath.Count > 0)
                {
                    moveState = MoveAlongPath(inputDirection, deltaTime);
                }
                else
                if (IsOnSegment)
                {
                    if (inputDirection.IsOppositeOf(direction))
                    {
                        direction = (Game as GridGame).UpdateDirection(inputDirection);
                    }
                    if (inputDirection != Direction.None && inputDirection != direction)
                    {
                        // start a new path if possible
                        moveState = Grid.AttemptToCreatePathOnSegment(this, deltaTime, A, B, inputDirection);
                        if(moveState == MoveState.Moved)
                        {
                            // update the direction along the path 
                            direction = (Game as GridGame).UpdateDirection(inputDirection); 
                        }
                    }
                    // if no path was created just move along the segment
                    if (direction != Direction.None && moveState != MoveState.Moved)
                    {
                        moveState = MoveOnSegment(direction, deltaTime); 
                    }
                    if (inputDirection != Direction.None && moveState != MoveState.Moved)
                    {
                        moveState = MoveOnSegment(inputDirection, deltaTime); 
                        direction = (Game as GridGame).UpdateDirection(inputDirection); 
                    }
                }
                else
                if (IsOnPoint)
                {
                    // 
                    moveState = MoveFromPoint(direction, inputDirection, deltaTime);
                }

                // if a path is present check if it hits the grid 
                if (Grid.CurrentPath.Count > 0)
                {
                    // check if hitting self 
                    if (Grid.IntersectsCurrentPath(out int j1, out int j2, out int xIntersect, out int yIntersect))
                    {
                        (Game as GridGame).DoGameOver(j1, j2); 
                    }
                    else
                    {
                        // check if closing path 

                        // get last segment point indices running from i1 to i2
                        int i1 = Grid.CurrentPath[Grid.CurrentPath.Count - 2];
                        int i2 = Grid.CurrentPath[Grid.CurrentPath.Count - 1];

                        // does p1-p2 intersect with another section ? 
                        if (Grid.IntersectPathWithGrid(
                            i1, i2,
                            out j1, out j2, out xIntersect, out yIntersect)
                            && xIntersect >= 0
                            && yIntersect >= 0)
                        {
                            // yes intersection with j1-j2 at x-yIntersect
                            GridSurface surface = Grid.ClosePath(j1, j2, xIntersect, yIntersect, true);
                            if (surface != null)
                            {
                                (Game as GridGame).Score += (int)surface.Score;

                                // calculate bounding rect of poly and its center of mass
                                RectF b = surface.BoundingRect;
                                float w2 = b.Width * Grid.GridScaleX * 0.5f;
                                float h2 = b.Height * Grid.GridScaleX * 0.5f;
                                RectF rc = new RectF(surface.Centroid.X * Grid.GridScaleX - w2, surface.Centroid.Y * Grid.GridScaleY - h2, w2 * 2, h2 * 2);

                                // create a score popup at the centroid 
                                new PopupScore(
                                    this,
                                    $"{(int)surface.Score}",
                                    MathF.Min(100, 18 + surface.Surface * 400),
                                    true,
                                    rc,
                                    new PointF(0, 0),
                                    Colors.LightYellow);
                            }

                            // player is now on Point 
                            A = Grid.Points.Count - 1;
                            B = -1;
                            ABPosition = 0;
                            moveState = MoveState.Moved;
                        }
                    }
                }

                // if blocked in current direction, move in other direction 
                if (moveState == MoveState.Blocked)
                {
                    LastDirection = (Game as GridGame).UpdateDirection(Direction.None);
                }
                if (moveState == MoveState.None && direction != Direction.None)
                {
                    LastDirection = inputDirection; 
                }
                else
                {
                    LastDirection = direction;
                }

                // update position in canvas based on the position in the segment or point 
                UpdatePosition();
            }
        }

        /// <summary>
        /// move player on an existing segment  
        /// </summary>
        MoveState MoveOnSegment(Direction moveDirection, float deltaTime)
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


        /// <summary>
        /// Move player starting from a point in the graph
        /// </summary>
        MoveState MoveFromPoint(Direction inputDirection, Direction nextDirection, float deltaTime)
        {
            // if not given a direction, dont move  
            if (!inputDirection.AnyDir())
            {
                return MoveState.None;
            }

            // check if we can move along an existing connection or need to create a new path
            GridPoint a = Grid.Points[A];
            GridConnection c = Grid.Connections[A];

            MoveState moved = MoveState.None;
            if (nextDirection != Direction.None)
            {
                // when user is pressing multiple keys allow to move on a split
                // in the alternate direction, without this it would be difficult to change 
                // direction on segments on intersections
                moved = MoveFromPointInDirection(nextDirection, deltaTime, a, c);
            }
            if (moved != MoveState.Moved)
            {
                moved = MoveFromPointInDirection(inputDirection, deltaTime, a, c);
            }
            if (moved == MoveState.None)
            {
               // if (can create path in input direction)
               // {
               //     CreatePathInDirection();
               // }
            }
            return moved;
        }

        /// <summary>
        /// move from point in given direction  
        /// </summary>
        /// <param name="inputDirection"></param>
        /// <param name="deltaTime"></param>
        /// <param name="a"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        MoveState MoveFromPointInDirection(Direction inputDirection, float deltaTime, GridPoint a, GridConnection c)
        {
            if (inputDirection.IsLeft())
            {
                if (c.Left < 0)
                {
                    return MoveState.Blocked; // cant move that way
                }
                else
                {
                    // can move to left 
                    B = c.Left;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Left));
                }
            }
            else
            if (inputDirection.IsRight())
            {
                if (c.Right < 0)
                {
                    return MoveState.Blocked;
                }
                else
                {
                    B = c.Right;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Right));
                }
            }
            else
            if (inputDirection.IsUp())
            {
                if (c.Top < 0) return MoveState.Blocked;
                else
                {
                    B = c.Top;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Top));
                }
            }
            else
            if (inputDirection.IsDown())
            {
                if (c.Bottom < 0) return MoveState.Blocked;
                else
                {
                    B = c.Bottom;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Bottom));
                }
            }
            return inputDirection.AnyDir() ? MoveState.Moved : MoveState.None;
        }
    

        /// <summary>
        /// move along the open path, only the inputDirection is used
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        MoveState MoveAlongPath(Direction inputDirection, float deltaTime)
        { 
            GridPoint a = Grid.Points[A];
            GridPoint b = Grid.Points[B];
            Direction connectionType = GridPoint.GetConnectionType(a, b);
            float distance = GridPoint.Distance(a, b, connectionType);

            switch (connectionType)
            {
                //
                // Connection == DOWN 
                // 
                case Direction.Bottom:
                    {
                        if (inputDirection == Direction.Down || inputDirection == Direction.None)
                        {
                            return UpdatePosition(deltaTime, b, distance, 0, 1);
                        }
                        else
                        if (inputDirection == Direction.Left)
                        {
                            return UpdatePath(-1, 0);
                        }
                        else
                        if (inputDirection == Direction.Right)
                        {
                            if (/*bounds*/ a.X < (Grid.SizeX - 1) & /*not filled*/ true)
                            {
                                return UpdatePath(1, 0); 
                            }
                            return MoveState.Blocked;
                        }
                        return MoveState.None;
                    }

                //
                // Connection == UP
                //
                case Direction.Top:
                    {
                        if (inputDirection == Direction.Up || inputDirection == Direction.None)
                        {
                            return UpdatePosition(deltaTime, b, distance, 0, -1);
                        }
                        else
                        if (inputDirection == Direction.Left)
                        {
                            return UpdatePath(-1, 0);
                        }
                        else
                        if (inputDirection == Direction.Right)
                        {
                            return UpdatePath(1, 0);
                        }
                        return MoveState.None;
                    }

                //
                // Connection == LEFT | RIGHT
                //
                case Direction.Left:
                    {
                        if (inputDirection == Direction.Left || inputDirection == Direction.None)
                        {
                            return UpdatePosition(deltaTime, b, distance, -1, 0);
                        }
                        else
                        if (inputDirection == Direction.Up)
                        {
                            return UpdatePath(0, -1); 
                        }
                        else
                        if (inputDirection == Direction.Bottom)
                        {
                            return UpdatePath(0, 1); 
                        }
                        return MoveState.None; 
                    }

                //
                // Connection == RIGHT
                //
                case Direction.Right:
                    {
                        if (inputDirection == Direction.Right || inputDirection == Direction.None)
                        {
                            return UpdatePosition(deltaTime, b, distance, 1, 0);
                        }
                        else
                        if (inputDirection == Direction.Up)
                        {
                            return UpdatePath(0, -1);
                        }
                        else
                        if (inputDirection == Direction.Bottom)
                        {
                            return UpdatePath(0, 1); 
                        }
                        return MoveState.None;
                    }
            }

            return MoveState.None;

            //
            // update path: create a new path segment and move player into the last 
            //
            MoveState UpdatePath(int fx, int fy)
            {
                // check bounds in direction 
                if(!((fx == -1 && /*bounds */ a.X > 0 & /*not filled               */ true)
                     ||
                    (fx == 1 && /*bounds   */ a.X < (Grid.SizeX - 1) & /*not filled*/ true)
                     ||
                    (fy == -1 && /*bounds  */ a.Y > 0 & /*not filled               */ true)
                     ||
                    (fy == 1 && /*bounds   */ a.Y < (Grid.SizeY - 1) & /*not filled*/ true)                     
                ))
                {
                    return MoveState.Blocked;                
                }

                int d = 1 + (int)MathF.Ceiling(Speed * deltaTime);

                GridPoint n = new GridPoint(
                    Grid.Points.Count,
                    b.X + fx * d,
                    b.Y + fy * d);
                
                GridConnection cN = new GridConnection(Grid.Points.Count,
                    fx == 1 ? B : -1,
                    fy == 1 ? B : -1,
                    fx == -1 ? B : -1,
                    fy == -1 ? B : -1);

                GridConnection cP = Grid.Connections[Grid.Points.Count - 1];

                if (fx == -1) cP.Left = cN.Index;
                if (fx == 1) cP.Right = cN.Index;
                if (fy == -1) cP.Top = cN.Index;
                if (fy == 1) cP.Bottom = cN.Index;

                Grid.Points.Add(n);
                Grid.Connections.Add(cN);
                Grid.Connections[cP.Index] = cP;

                Grid.CurrentPath.Add(n.Index);

                B = Grid.CurrentPath[Grid.CurrentPath.Count - 1];
                A = Grid.CurrentPath[Grid.CurrentPath.Count - 2];
                ABPosition = Speed * deltaTime;

                return MoveState.Moved;
            }

            //
            // update position along a segment of a path moving the last point along with the player 
            //
            MoveState UpdatePosition(float deltaTime, GridPoint b, float distance, int fx, int fy)
            {
                ABPosition += Speed * deltaTime;
                if (ABPosition >= distance)
                {
                    // move the last point along with the player 
                    int d = (int)MathF.Ceiling(ABPosition - distance); 
                    b.X += fx * d;
                    b.Y += fy * d;
                    Grid.Points[B] = b;
                }
                return MoveState.Moved;
            }
        }
    }
}
