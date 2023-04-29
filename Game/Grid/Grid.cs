using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{

    public class Grid : GameObject
    {
        public const int MIN_DISTANCE_BETWEEN_POINTS = 5;

        public readonly int SizeX;
        public readonly int SizeY;

        // reciprocals 
        public readonly float RSizeX;
        public readonly float RSizeY;

        // the scaling from grid to the internal canvas (which is later transformed to viewspace) 
        public readonly float GridScaleX;
        public readonly float GridScaleY;

        public List<GridPoint> Points;
        public List<GridConnection> Connections;
        public List<GridSurface> Surfaces;
        public List<int> CurrentPath; 

        public Grid(GameObject parent, int width, int height) : base(parent)
        {
            SizeX = width;
            SizeY = height;
            RSizeX = 1f / width;
            RSizeY = 1f / height;
            GridScaleX = RSizeX * GridGame.CanvasWidth;
            GridScaleY = RSizeY * GridGame.CanvasHeight;

            Points = new List<GridPoint>();
            Connections = new List<GridConnection>(); 
            CurrentPath = new List<int>();
            Surfaces = new List<GridSurface>(); 
        }


        /// <summary>
        /// test if the last segment intersects with any other part in the path
        /// </summary>
        public bool IntersectsCurrentPath(out int h1, out int h2, out int xIntersect, out int yIntersect)
        {
            h1 = h2 = xIntersect = yIntersect = -1;

            if (CurrentPath == null || CurrentPath.Count < 3)
            {
                return false;
            }

            PointF a = Points[CurrentPath[CurrentPath.Count - 1]].AsPointF;
            PointF b = Points[CurrentPath[CurrentPath.Count - 2]].AsPointF;

            PointF intersection = default;

            int prev = CurrentPath[0];
            PointF p1 = Points[prev].AsPointF;             

            for(int i = 1; i < CurrentPath.Count - 2; i++)
            {
                int current = CurrentPath[i];
                PointF p2 = Points[current].AsPointF;

                if (MathEx.TryGetSegmentIntersection(a, b, p1, p2, out intersection, true))
                {
                    // segmentize i-c.Right 
                    h1 = prev;
                    h2 = current;
                    xIntersect = (int)intersection.X;
                    yIntersect = (int)intersection.Y;
                    return true;
                }

                p1 = p2;
                prev = i; 
            }

            return false;
        }


        /// <summary>
        /// does a line from p1 to p2 intersect with any other and if so return the indices for the segment 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public bool IntersectPathWithGrid(int p1, int p2, out int h1, out int h2, out int xIntersect, out int yIntersect)
        {
#if DEBUG
            if(!CurrentPath.Contains(p1) && !CurrentPath.Contains(p2))
            {
                throw new Exception("grid.intersect: p1 and/or p2 must be part of the path");
            }

            if(p1 > p2)
            {
                throw new Exception("grid.intersect: p1 must be before p2");
            }

#endif 
            PointF a = Points[p1].AsPointF;
            PointF b = Points[p2].AsPointF;

            // check if the path intersects with the grid 
            int endIndex = Points.Count - CurrentPath.Count + 1;
            int path0 = CurrentPath[0]; 

            for (int i = 0; i < endIndex; i++)
            {
                PointF p = Points[i].AsPointF;
                GridConnection c = Connections[i];

                // dont check colliding endpoints of start of path with grid 
                if (CurrentPath.Count == 2 && (i == path0 || c.ConnectsWith(path0))) continue; 

                // - only check right/bottom direction as to avoid many duplicate checks 
                if ((c.Right >= 0) & (c.Right < endIndex)
                    &&
                    MathEx.TryGetSegmentIntersection(a, b, p, Points[c.Right].AsPointF, out PointF intersection, true))
                {
                    // segmentize i-c.Right 
                    h1 = i;
                    h2 = c.Right;
                    xIntersect = (int)intersection.X;
                    yIntersect = (int)intersection.Y; 
                    return true; 
                }
                else
                if ((c.Bottom >= 0) & (c.Bottom < endIndex)
                    &&
                    MathEx.TryGetSegmentIntersection(a, b, p, Points[c.Bottom].AsPointF, out intersection, true))
                {
                    // segmentize i-c.Bottom 
                    h1 = i;
                    h2 = c.Bottom;
                    xIntersect = (int)intersection.X;
                    yIntersect = (int)intersection.Y;
                    return true; 
                }
            }

            // no intersection 
            h1 = h2 = xIntersect = yIntersect = -1; 
            return false;
        }


        //
        // split segment on player position (no checks) 
        // 
        // fx and fy set the direction of a-b 
        // 
        public GridPoint SplitSegment(GridPoint a, GridPoint b, out GridConnection cA, out GridConnection cB, float abPosition)
        {
            Direction connectionType = GridPoint.GetConnectionType(a, b);
            PointF connectionDirectionVector = connectionType.ToPointF();

            int cx = (int)connectionDirectionVector.X;
            int cy = (int)connectionDirectionVector.Y;

            GridPoint p = new GridPoint(
                Points.Count,
                a.X + (int)(cx * abPosition),
                a.Y + (int)(cy * abPosition));

            GridConnection c =
                cx != 0
                ?
                // horizontal slice
                new GridConnection(
                    p.Index,
                    cx == 1 ? a.Index : b.Index,
                    -1,
                    cx == 1 ? b.Index : a.Index,
                    -1)
                :
                // vertical slice 
                new GridConnection(
                    p.Index,
                    -1,
                    cy == 1 ? a.Index : b.Index,
                    -1,
                    cy == 1 ? b.Index : a.Index);

            Points.Add(p);
            Connections.Add(c);

            cA = Connections[a.Index];
            cB = Connections[b.Index];

            if (cx == 1)
            {
                cA.Right = c.Index;
                cB.Left = c.Index;
            }
            if (cx == -1)
            {
                cA.Left = c.Index;
                cB.Right = c.Index;
            }
            if (cy == 1)
            {
                cA.Bottom = c.Index;
                cB.Top = c.Index;
            }
            if (cy == -1)
            {
                cA.Top = c.Index;
                cB.Bottom = c.Index;
            }

            Connections[a.Index] = cA;            
            Connections[b.Index] = cB;

            // did we split a segment belonging to one (or maybe multiple) surfaces ? ifso, we need to insert the point into the service
            foreach (GridSurface surface in Surfaces)
            {
                for (int i = 0; i < surface.Points.Length - 1; i++)
                {
                    if ((surface.Points[i] == a.Index && surface.Points[i + 1] == b.Index) || (surface.Points[i] == b.Index && surface.Points[i + 1] == a.Index))
                    {
                        //
                        surface.InsertPoint(p.Index, i + 1);
                        break;
                    }
                }
            }

            return p;
        }


        /// <summary>
        /// close path intersection j1-j2 at x,yIntersect 
        /// </summary>
        /// <param name="j1"></param>
        /// <param name="j2"></param>
        /// <param name="xIntersect"></param>
        /// <param name="yIntersect"></param>
        /// <exception cref="NotImplementedException"></exception>
        public GridSurface ClosePath(int j1, int j2, int xIntersect, int yIntersect, bool createSurface)
        {
#if DEBUG
            if (CurrentPath.Count == 0)
            {
                throw new Exception("grid.closepath: path cannot be empty");
            }
#endif 
            // split the segment j1-j2 at the intersection
            GridSurface surface = null;
            GridPoint p = default; 
            float d;

            // remove the point we were using to grow the path
            Connections.RemoveAt(Points.Count - 1);
            Points.RemoveAt(Points.Count - 1);
            CurrentPath.RemoveAt(CurrentPath.Count - 1);

            if (j1 >= 0 && j2 >= 0)
            {
                // update position of point to its intersection with j1-j2
                switch (GridPoint.GetConnectionType(Points[j1], Points[j2]))
                {
                    case Direction.Left:
                        d = xIntersect - Points[j2].X;
                        break;

                    case Direction.Right:
                        d = xIntersect - Points[j1].X;
                        break;

                    case Direction.Top:
                        d = yIntersect - Points[j2].Y;
                        break;

                    case Direction.Bottom:
                        d = yIntersect - Points[j1].Y;
                        break;

                    default: d = 0; break; 
                }

                // split segment creating a new point at the intersection with the path 
                p = SplitSegment(Points[j1], Points[j2], out GridConnection j1C, out GridConnection j2C, d);
            }
            else
            {
                // position is OnPoint j1
                d = 0;
                p = Points[j1]; 
            }

            // get info on the newly inserted point, we need to connect the last point in the path to the point created
            GridConnection pC = Connections[p.Index]; 
            GridPoint pLastInPath = Points[CurrentPath[CurrentPath.Count - 1]]; 
            GridConnection cLast = Connections[pLastInPath.Index];

            // connect the newly created point to the last index in the path 
            // -> this index should be reset if surface could not be created .. bugged anyway then.. 
            CurrentPath.Add(p.Index);
            if (cLast.GetConnectionType(p.Index, out Direction dirLastToP))
            {
                pC.SetConnection(dirLastToP.Opposite(), pLastInPath.Index);
            }

            // store updates closing the path  
            Connections[p.Index] = pC;
            Connections[pLastInPath.Index] = cLast;

            // create a surface from the path 
            if (createSurface)
            {
                // get both surfaces
                List<int> ccw = TracePathSurface(true, true);
                List<int> cw = TracePathSurface(false, true);

                // calculate surface of each poly  
                float ccwSurface = (ccw != null && ccw.Count > 0) ? MathF.Abs(CalculateSurfaceArea(ccw)) : 0;
                float cwSurface = (cw != null && cw.Count > 0) ? MathF.Abs(CalculateSurfaceArea(cw)) : 0;

                if (ccwSurface > 0 || cwSurface > 0)
                {
                    bool is_ccw = (cwSurface > ccwSurface || cwSurface == 0) && ccwSurface != 0;

                    // select the smallest non zero surface 
                    List<int> poly = is_ccw ? ccw : cw;
                    float polySurface = is_ccw ? ccwSurface : cwSurface;
                    float surfaceP = 1f / (float)(SizeX * SizeY) * polySurface;

                    if (poly != null && poly.Count > 0)
                    {
                        // Create a new surface
                        surface = new GridSurface(
                            this.Game as GridGame, 
                            poly.ToArray(),
                            is_ccw,
                            surfaceP,
                            surfaceP * 10000f
                        );
                        Surfaces.Add(surface);
                    }
                }
            }

            // clear path 
            if (surface == null)
            {
                // if there is no surface then remove it from the point list.. -> should be error 
                //int i = Points.Count - CurrentPath.Count + 1;
                //int c = CurrentPath.Count - 1;
                //Points.RemoveRange(i, c);
                //Connections.RemoveRange(i, c); 
            }
            CurrentPath.Clear();

            return surface; 
        }

        public List<int> TracePathSurface(bool ccw, bool close)
        {
#if DEBUG
            if (CurrentPath.Count < 2)
            {
                throw new Exception("path should at least contain 2 points"); 
            }
#endif

            List<int> points = new List<int>();
            int i = CurrentPath[CurrentPath.Count - 1];
            int prev = CurrentPath[CurrentPath.Count - 2];

            while (i != CurrentPath[0] && i >= 0)
            {
                GridConnection current = Connections[i];
                if (!current.GetConnectionType(prev, out Direction incomming))
                {
                    return null; 
                }

                if (current.Next(incomming, ccw, out int next, out Direction direction))
                {
                    points.Add(next);
                }
                else
                {
                    return null;
                }

                if (points.Count >= this.Points.Count)
                {
                    return null; 
                }

                prev = i;
                i = next;
            }

            if(close && points.Count > 0)
            {
                // close polygon 
                points.AddRange(CurrentPath.Skip(1));
                points.Add(points[0]);
            }

            return points; 
        }


        /// <summary>
        /// calculate the surface of a polygon
        /// </summary>
        /// <param name="indices"></param>
        /// <returns>surface, its negative for a cw surface, positive for ccw</returns>
        public float CalculateSurfaceArea(List<int> indices) => MathEx.CalculateSurfaceArea(indices.Select(x => Points[x].AsPointF).ToArray());


        /// <summary>
        /// Move player starting from a point in the graph
        /// </summary>
        public MoveState MoveFromPoint(GridActor actor, Direction inputDirection, Direction nextDirection, float deltaTime)
        {
            // if not given a direction, dont move  
            if (!inputDirection.AnyDir())
            {
                return MoveState.None;
            }

            // check if we can move along an existing connection or need to create a new path
            GridPoint a = Points[actor.A];
            GridConnection c = Connections[actor.A];

            MoveState moved = MoveState.None;
            if (nextDirection != Direction.None)
            {
                // when user is pressing multiple keys allow to move on a split
                // in the alternate direction, without this it would be difficult to change 
                // direction on segments on intersections
                moved = MoveFromPointInDirection(actor, nextDirection, deltaTime, a, c);
            }
            if (moved != MoveState.Moved)
            {
                moved = MoveFromPointInDirection(actor, inputDirection, deltaTime, a, c);
            }
            if (moved != MoveState.Moved)
            {
                if (!c.TryGetConnected(inputDirection, out int dummyIndex))
                {
                    moved = CreatePathInDirectionFromPoint(actor, a, inputDirection, deltaTime);
                }
            }

            return moved;
        }

        /// <summary>
        /// move from point in given direction  
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="deltaTime"></param>
        /// <param name="a"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        MoveState MoveFromPointInDirection(GridActor actor, Direction dir, float deltaTime, GridPoint a, GridConnection c)
        {
            if (dir.IsLeft())
            {
                if (c.Left < 0)
                {
                    return MoveState.Blocked; // cant move that way
                }
                else
                {
                    // can move to left 
                    actor.B = c.Left;
                    actor.ABPosition = (actor.Speed * deltaTime).Clip(0f, GridPoint.DistanceToNeighbour(a, Points[actor.B], Direction.Left));
                }
            }
            else
            if (dir.IsRight())
            {
                if (c.Right < 0)
                {
                    return MoveState.Blocked;
                }
                else
                {
                    actor.B = c.Right;
                    actor.ABPosition = (actor.Speed * deltaTime).Clip(0f, GridPoint.DistanceToNeighbour(a, Points[actor.B], Direction.Right));
                }
            }
            else
            if (dir.IsUp())
            {
                if (c.Top < 0) return MoveState.Blocked;
                else
                {
                    actor.B = c.Top;
                    actor.ABPosition = (actor.Speed * deltaTime).Clip(0f, GridPoint.DistanceToNeighbour(a, Points[actor.B], Direction.Top));
                }
            }
            else
            if (dir.IsDown())
            {
                if (c.Bottom < 0) return MoveState.Blocked;
                else
                {
                    actor.B = c.Bottom;
                    actor.ABPosition = (actor.Speed * deltaTime).Clip(0f, GridPoint.DistanceToNeighbour(a, Points[actor.B], Direction.Bottom));
                }
            }
            return dir.AnyDir() ? MoveState.Moved : MoveState.None;
        }


        /// <summary>
        /// attempt to create a path on the given segment and move actor onto it 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="deltaTime"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inputDirection"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public MoveState AttemptToCreatePathOnSegment(GridActor actor, float deltaTime, int a, int b, Direction inputDirection)
        {
            GridPoint A = Points[a];
            GridPoint B = Points[b];

            // only allow if some minimal distance from a and b 
            Direction connectionDirection = GridPoint.GetConnectionType(A, B);

            if (inputDirection == connectionDirection)
            {
                return MoveState.Blocked;
            }

            float ab = actor.ABPosition;
            float distance = GridPoint.DistanceToNeighbour(A, B);
            bool close = (ab < Grid.MIN_DISTANCE_BETWEEN_POINTS) | (ab > distance - Grid.MIN_DISTANCE_BETWEEN_POINTS);

            if (close)
            {
                return MoveState.Blocked;
            }

            //
            // if moving downward from a -> b and to the left is a filled surface while the right is free 
            // : then if moving left : 
            //  
            // c will be [a.x - delta, (a.y + b.y) / 2]
            //
            // if(c inside poly with a-b) then block move
            // 
            PointF c = PointF.Zero;
            float delta = Grid.MIN_DISTANCE_BETWEEN_POINTS / 3;
            PointF idir = inputDirection.ToPointF();

            switch (connectionDirection)
            {
                case Direction.Up: c = new PointF(A.X + (idir.X * delta), A.Y - (int)ab); break;
                case Direction.Down: c = new PointF(A.X + (idir.X * delta), A.Y + (int)ab); break;
                case Direction.Left: c = new PointF(A.X - (int)ab, A.Y + (idir.Y * delta)); break;
                case Direction.Right: c = new PointF(A.X + (int)ab, A.Y + (idir.Y * delta)); break;
            }
            if ((c == PointF.Zero) | (c.X < 0) | (c.Y < 0) | (c.X >= SizeX) | (c.Y >= SizeY))
            {
                return MoveState.Blocked; 
            }

            // is segment ab already used in a polygon?
            foreach (GridSurface surface in Surfaces)
            {
                for (int i = 0; i < surface.Points.Length - 1; i++)
                {
                    if (surface.Points[i] == a || surface.Points[i + 1] == a || surface.Points[i] == b || surface.Points[i + 1] == b)
                    {
                        if(MathEx.PointInPolygon(surface.Points.Select(x => Points[x].AsPointF).ToArray(), c) > 0)
                        {
                            return MoveState.Blocked;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // split the segment 
            float positionOnPath = deltaTime * actor.Speed;
            GridPoint p = SplitSegment(A, B, out GridConnection cA, out GridConnection cB, actor.ABPosition);
            PointF vInput = inputDirection.ToPointF();

            // and create a path from the newly created point 
            return CreatePath(actor, deltaTime, p, vInput);
        }


        //
        // Create a new path from the given point which is included in the path as the starting point 
        //
        // - fx and fy set the direction vector of the path going out : 0, 1 == down  
        //  
        public MoveState CreatePath(GridActor actor, float deltaTime, GridPoint p, PointF dir)
        {
            float delta = 1 + MathF.Floor(actor.Speed * deltaTime);

            GridConnection iC = Connections[p.Index];

            GridPoint n = new GridPoint(
                Points.Count,
                p.X + (int)dir.X * (int)delta,
                p.Y + (int)dir.Y * (int)delta);

            GridConnection cN = new GridConnection(
                Points.Count,
                dir.X == 1 ? p.Index : -1,
                dir.Y == 1 ? p.Index : -1,
                dir.X == -1 ? p.Index : -1,
                dir.Y == -1 ? p.Index : -1);

            iC.SetConnection(dir, n.Index);

            Points.Add(n);
            Connections.Add(cN);
            Connections[iC.Index] = iC;

            CurrentPath.Clear();
            CurrentPath.Add(p.Index);
            CurrentPath.Add(cN.Index);

            actor.A = CurrentPath[0];
            actor.B = CurrentPath[1];
            actor.ABPosition = delta;

            return MoveState.Moved;
        }


        /// <summary>
        /// create a new path in the direction at the given point if there is no surface yet
        /// </summary>
        MoveState CreatePathInDirectionFromPoint(GridActor actor, GridPoint a, Direction inputDirection, float deltaTime)
        {
            PointF vdir = inputDirection.ToPointF();
            float d = deltaTime * actor.Speed;

            // calculate new position, then do a bound check 
            PointF c = new PointF(a.X + vdir.X * d, a.Y + vdir.Y * d);
            if ((c == PointF.Zero) | (c.X < 0) | (c.Y < 0) | (c.X >= SizeX) | (c.Y >= SizeY))
            {
                return MoveState.Blocked;
            }

            // check if the point is inside an existing surface 
            foreach (GridSurface surface in Surfaces)
            {
                for (int i = 0; i < surface.Points.Length - 1; i++)
                {
                    if (surface.Points[i] == a.Index || surface.Points[i + 1] == a.Index)
                    {
                        if (MathEx.PointInPolygon(surface.Points.Select(x => Points[x].AsPointF).ToArray(), c) > 0)
                        {
                            return MoveState.Blocked;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // create a new path for actor to follow
            return CreatePath(actor, deltaTime, a, vdir);
        }


        /// <summary>
        /// search the shortest path: from->to using a*
        /// </summary>
        public int[] SearchPathTo(int from, int to)
        {
            List<int> open = new List<int>();
            open.Add(from);

            // gScore[n] is the cost of the cheapest path from start to n currently known.
            Span<float> g = stackalloc float[Points.Count];

            // fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
            // how cheap a path could be from start to finish if it goes through n.
            Span<float> f = stackalloc float[Points.Count];

            // the camefrom map recording the best path 
            Span<int> c = stackalloc int[Points.Count]; 

            g.Fill(float.PositiveInfinity);
            f.Fill(float.PositiveInfinity);
            c.Fill(-1); 

            // determine point in the frontier with the least gscore/cost 
            int minOpen(Span<float> f)
            {
                int imin = -1;
                float min = float.MaxValue;

                for (int i = open.Count - 1; i >= 0; i--)
                {
                    if (f[open[i]] < min)
                    {
                        imin = i;
                        min = f[open[i]];
                    }
                }

                return imin;
            }

            // cost == distance to neighbour
            float d (int a, int neighbor) => GridPoint.DistanceToNeighbour(Points[a], Points[neighbor]);

            // our heuristic == distance to start 
            float h (int neighbor) => GridPoint.EuclidianDistanceTo(Points[from], Points[neighbor]);

            // recreate path taken 
            int[] reconstructPath(Span<int> c, int current)
            {
                List<int> path = new List<int>();
                path.Add(current);

                int i = current;

                i = c[i];
                while(i >= 0 && path.Count < 10000)
                {
                    path.Add(i);
                    if (i == from)
                    {
                        path.Reverse();
                        return path.ToArray();
                    }
                    i = c[i];
                }
                return null; 
            }

            // init scores 
            g[from] = 0;
            f[from] = h(from);

            // foreach point in the open set
            int iter = 0; 
            while (open.Count > 0 & iter++ < 10000)
            {
                int iMinOpen = minOpen(f); 
                if(iMinOpen == -1)
                {
                    // bugged or error
                    return null; 
                }

                if(open[iMinOpen] == to)
                {
                    return reconstructPath(c, to); 
                }

                GridConnection current = Connections[open[iMinOpen]];
                open.RemoveAt(iMinOpen);

                // loop all neighbours 
                foreach (int neighbor in current.EnumerateNeighbours())
                {
                    float gscore = g[current.Index] + d(current.Index, neighbor);

                    if (gscore < g[neighbor])
                    {
                        // this path to neighbor is better than any previous one
                        c[neighbor] = current.Index; 
                        g[neighbor] = gscore;
                        f[neighbor] = gscore + h(neighbor);

                        /* if a node is reached by one path, removed from openSet, and subsequently reached by a cheaper path, it will 
                         * be added to openSet again. This is essential to guarantee that the path returned is optimal if the heuristic function
                         * is admissible but not consistent. If the heuristic is consistent, when a node is removed from openSet the path to it
                         * is guaranteed to be optimal so the test ‘gscore < g[neighbor]’ will always fail if the node is reached again. */
                        if (!open.Contains(neighbor))
                        {
                            open.Add(neighbor);
                        }
                    } 
                }
            }

            return null; 
        }
    }
}
