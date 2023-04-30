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
                        moveState = MoveOnConnection(direction, deltaTime); 
                    }
                    if (inputDirection != Direction.None && moveState != MoveState.Moved)
                    {
                        moveState = MoveOnConnection(inputDirection, deltaTime); 
                        direction = (Game as GridGame).UpdateDirection(inputDirection); 
                    }
                }
                else
                if (IsOnPoint)
                {
                    // 
                    moveState = Grid.MoveFromPoint(this, direction, inputDirection, deltaTime);
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

                        GridPoint pi1 = Grid.Points[i1];
                        GridSurface surface = null;

                        // test for point intersection 
                        for (int i = 0; i < Grid.Points.Count - Grid.CurrentPath.Count; i++)
                        {
                            if (!Grid.CurrentPath.Contains(i))
                            {
                                GridPoint test = Grid.Points[i];

                                if (// horizontal hit from left or right on point test
                                    (test.X == pi1.X & (test.Y >= pi1.Y - 1) & (test.Y <= pi1.Y + 1))
                                    |
                                    // vertical hit from left or right on point test
                                    (test.Y == pi1.Y & (test.X >= pi1.X - 1) & (test.X <= pi1.X + 1)))
                                {
                                    surface = Grid.ClosePath(test.Index, -1, 0, 0, true);
                                    A = test.Index;
                                    B = -1;
                                    ABPosition = 0;
                                    moveState = MoveState.Moved;
                                }
                            }
                        }

                        // insersect grid segments:  does p1-p2 intersect with another section ? 
                        // todo > the intersection can be greatly simplified 
                        if (surface == null && Grid.IntersectPathWithGrid(
                            i1, i2,
                            out j1, out j2, out xIntersect, out yIntersect)
                            && xIntersect >= 0
                            && yIntersect >= 0)
                        {
                            // yes intersection with j1-j2 at x-yIntersect
                            surface = Grid.ClosePath(j1, j2, xIntersect, yIntersect, true);

                            // player is now on Point 
                            A = Grid.Points.Count - 1;
                            B = -1;
                            ABPosition = 0;
                            moveState = MoveState.Moved;
                        }

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
                                Game,
                                $"{(int)surface.Score}",
                                MathF.Min(100, 18 + surface.Surface * 400),
                                true,
                                rc,
                                new PointF(0, 0),
                                Colors.LightYellow);
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
        /// move along the open path moving the last point along with the player
        /// </summary>
        MoveState MoveAlongPath(Direction inputDirection, float deltaTime)
        { 
            GridPoint a = Grid.Points[A];
            GridPoint b = Grid.Points[B];
            Direction connectionType = GridPoint.GetConnectionType(a, b);
            float distance = GridPoint.DistanceToNeighbour(a, b, connectionType);

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
