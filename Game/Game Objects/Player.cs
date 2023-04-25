using NSS;
using NSS.GameObjects;
using System.Runtime.InteropServices;

namespace Grid.GameObjects
{

    public class Player : GridActor
    {
        public Player(GridGame game, Grid grid) : base(game, grid)
        {
            Color = Colors.LimeGreen;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
              
            MoveState moveState = MoveState.None; 

            if (Game.InputState.Move)
            {
                if (Grid.CurrentPath.Count > 0)
                {
                    moveState = MoveAlongPath(deltaTime);
                }
                else
                if (IsOnSegment)
                {
                    moveState = MoveOnSegment(deltaTime);
                }
                else
                if (IsOnPoint)
                {
                    moveState = MoveFromPoint(deltaTime);
                }

                // check position in segment, we may have moved onto a point 
                CheckSegmentPosition();

                // check if hitting the grid 
                if (Grid.CurrentPath.Count > 0)
                {
                    // check if hitting self 
                    if (Grid.IntersectsCurrentPath(out int j1, out int j2, out int xIntersect, out int yIntersect))
                    {
                        // highlight segment intersecting
                        new GridSegment(Game as GridGame, j1, j2);

                        // gameover 
                        Game.GameState = GameState.GameOver;

                        Game.ShakeFrameCountDown = 30;
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
                            Direction dir;
                            if (Game.InputState.Left) dir = Direction.Left;
                            else if (Game.InputState.Right) dir = Direction.Right;
                            else if (Game.InputState.Up) dir = Direction.Top;
                            else dir = Direction.Bottom;

                            // yes intersection with j1-j2 at x-yIntersect
                            GridSurface surface = Grid.ClosePath(dir, j1, j2, xIntersect, yIntersect, true);
                            if (surface != null)
                            {
                                (Game as GridGame).Score += (int)(surface.Surface);
                            }

                            // player is now on Point 
                            A = Grid.Points.Count - 1;
                            B = -1;
                            ABPosition = 0;
                            moveState = MoveState.Moved;
                        }
                    }
                }

                // if on a point not on the path -> kill path (or fill .. later) 
                if (Grid.CurrentPath.Count > 0 && IsOnPoint)
                {
                    if (!Grid.CurrentPath.Contains(A))
                    {
                        Grid.Points.RemoveRange(Grid.Points.Count - Grid.CurrentPath.Count, Grid.CurrentPath.Count);
                        Grid.Connections.RemoveRange(Grid.Connections.Count - Grid.CurrentPath.Count, Grid.CurrentPath.Count);
                        Grid.CurrentPath.Clear();
                    }
                }

                // shake grid if move was blocked 
                if (moveState == MoveState.Blocked)
                {
                   //  Game.StartShake(10);
                }

                // update position in canvas based on the position in the segment or point 
                UpdatePosition();
            }
        }

        /// <summary>
        /// move player on an existing segment  
        /// </summary>
        MoveState MoveOnSegment(float deltaTime)
        {
            // move along the segment in direction of input 
            GridPoint a = Grid.Points[A];
            GridPoint b = Grid.Points[B];
            Direction connectionType = GridPoint.GetConnectionType(a, b);

            // check if the movement direction is valid for the given connection 
            PointF vc = connectionType.ToPointF();
            PointF vinput = Game.InputState.Direction.ToPointF();

            if (vc.X != 0 && Game.InputState.Up)
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
            if (vc.X != 0 && Game.InputState.Down)
            {
                if (a.Y > (Grid.SizeY - 1)) return MoveState.Blocked;                
            }
            else
            if (vc.Y != 0 && Game.InputState.Left)
            {
                if (a.X < 0) return MoveState.Blocked; 
            }
            else
            if (vc.Y != 0 && Game.InputState.Right)
            {
                if (a.X > (Grid.SizeX - 1)) return MoveState.Blocked;
            }
            else
            {
                return MoveAlongConnection(deltaTime, a, b, connectionType, Game.InputState.Direction);
            }

            return Grid.CreatePath(
                            this,
                            deltaTime,
                            Grid.SplitSegment(a, b, out GridConnection cA, out GridConnection cB, ABPosition, vc).Index, // left
                            cA, cB,
                            (int)vinput.X, (int)vinput.Y);
        }


        /// <summary>
        /// Move player starting from a point in the graph
        /// </summary>
        MoveState MoveFromPoint(float deltaTime)
        {
            // check if we can move along an existing connection or need to create a new path
            GridPoint a = Grid.Points[A];
            GridConnection c = Grid.Connections[A];
            if (Game.InputState.Left)
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
            if (Game.InputState.Right)
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
            if (Game.InputState.Up)
            {
                if (c.Top < 0) return MoveState.Blocked;
                else
                {
                    B = c.Top;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Top));
                }
            }
            else
            if (Game.InputState.Down)
            {
                if (c.Bottom < 0) return MoveState.Blocked;
                else
                {
                    B = c.Bottom;
                    ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], Direction.Bottom));
                }
            }
            return Game.InputState.Move ? MoveState.Moved : MoveState.None;
        }

        /// <summary>
        /// move along the open path 
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        MoveState MoveAlongPath(float deltaTime)
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
                        // down = moving in direction of path 
                        if (Game.InputState.Down)
                        {
                            return UpdatePosition(deltaTime, b, distance, 0, 1);
                        }
                        // left/right might create a new segment
                        if (Game.InputState.Left)
                        {
                            // while going down create a new point to grow to the left 
                            return UpdatePath(-1, 0);
                        }
                        if (Game.InputState.Right)
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
                        if (Game.InputState.Up)
                        {
                            return UpdatePosition(deltaTime, b, distance, 0, -1);
                        }
                        if (Game.InputState.Left)
                        {
                            return UpdatePath(-1, 0);
                        }
                        if (Game.InputState.Right)
                        {
                            return UpdatePath(1, 0);
                        }
                        return MoveState.None;
                    }

                //
                // Connection == LEFT 
                //
                case Direction.Left:
                    {
                        if (Game.InputState.Left)
                        {
                            return UpdatePosition(deltaTime, b, distance, -1, 0);
                        }
                        if (Game.InputState.Up)
                        {
                            return UpdatePath(0, -1); 
                        }
                        if(Game.InputState.Down)
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
                        if (Game.InputState.Right)
                        {
                            return UpdatePosition(deltaTime, b, distance, 1, 0);
                        }
                        if (Game.InputState.Up)
                        {
                            return UpdatePath(0, -1);
                        }
                        if (Game.InputState.Down)
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
