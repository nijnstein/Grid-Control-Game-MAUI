using NSS;
using NSS.GameObjects;
using System.Collections.ObjectModel;
using System.Reflection.Metadata.Ecma335;

namespace Grid.GameObjects
{

    public class Grid : GameObject
    {   
        public readonly int SizeX;
        public readonly int SizeY;

        // reciprocals 
        public readonly float RSizeX;
        public readonly float RSizeY;


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

            GridConnection ca = Connections[p1];
            GridConnection cb = Connections[p2];

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
        // Create a new path from the given point which is included in the path as the starting point 
        //
        // - fx and fy set the direction vector of the path going out : 0, 1 == down  
        //  
        public MoveState CreatePath(Player player, float deltaTime, int intersectionPointIndex, GridConnection cA, GridConnection cB, int fx, int fy)
        {
            float delta = 1 + MathF.Floor(player.Speed * deltaTime);

            GridPoint p = Points[intersectionPointIndex];
            GridConnection iC = Connections[p.Index];

            GridPoint n = new GridPoint(
                Points.Count,
                p.X + fx * (int)delta,
                p.Y + fy * (int)delta);

            GridConnection cN = new GridConnection(
                Points.Count,
                fx == 1 ? p.Index : -1,
                fy == 1 ? p.Index : -1,
                fx == -1 ? p.Index : -1,
                fy == -1 ? p.Index : -1);

            iC.SetConnection(fx, fy, n.Index);

            Points.Add(n);
            Connections.Add(cN);
            Connections[iC.Index] = iC;

            CurrentPath.Clear();
            CurrentPath.Add(intersectionPointIndex); 
            CurrentPath.Add(cN.Index);

            player.A = CurrentPath[0];
            player.B = CurrentPath[1];
            player.ABPosition = delta;

            return MoveState.Moved;
        }


        //
        // split segment on player position (no checks) 
        // 
        // fx and fy set the direction of a-b 
        // 
        public GridPoint SplitSegment(GridPoint a, GridPoint b, out GridConnection cA, out GridConnection cB, float abPosition, PointF connectionDirectionVector)
        {
            int cx = (int)connectionDirectionVector.X;
            int cy = (int)connectionDirectionVector.Y; 

            GridPoint p = new GridPoint(
                Points.Count,
                a.X + cx * (int)MathF.Floor(abPosition),
                a.Y + cy * (int)MathF.Floor(abPosition));

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
        public GridSurface ClosePath(Direction dir, int j1, int j2, int xIntersect, int yIntersect, bool createSurface)
        {
#if DEBUG
            if(CurrentPath.Count == 0)
            {
                 throw new Exception("grid.closepath: path cannot be empty");
            }
#endif 

            // split the segment j1-j2 at the intersection
            GridSurface surface = null;

            int lastInPathIndex = Points.Count - 1; 
            GridPoint lastInPath = Points[lastInPathIndex];
            GridConnection lastInPathConnection = Connections[lastInPathIndex];
          
            GridConnection j1C = Connections[j1];
            GridConnection j2C = Connections[j2];

            // update position of point to its intersection with j1-j2
            lastInPath.X = xIntersect;
            lastInPath.Y = yIntersect;
            
            // set connections to break into the segment 
            switch(dir)
            {
                case Direction.Left:
                case Direction.Right:
                    lastInPathConnection.Top = j1; 
                    lastInPathConnection.Bottom = j2;
                    j1C.Bottom = lastInPathIndex;
                    j2C.Top = lastInPathIndex; 
                    break;

                case Direction.Top:
                case Direction.Bottom:
                    lastInPathConnection.Left = j1;
                    lastInPathConnection.Right = j2;
                    j1C.Right = lastInPathIndex;
                    j2C.Left = lastInPathIndex;
                    break;
            }

            // store updates closing the path  
            Points[lastInPathIndex] = lastInPath;
            Connections[lastInPathIndex] = lastInPathConnection;
            Connections[j1] = j1C;
            Connections[j2] = j2C;

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

                    if (poly != null && poly.Count > 0)
                    {
                        surface = new GridSurface(
                            this.Game as GridGame, 
                            poly.ToArray(),
                            is_ccw,
                            polySurface
                        );
                        Surfaces.Add(surface);
                    }
                }
            }

            // clear path 
            if (surface == null)
            {
                // if there is no surface then remove it from the point list.. -> should be error 
                int i = Points.Count - CurrentPath.Count + 1;
                int c = CurrentPath.Count - 1;
                Points.RemoveRange(i, c);
                Connections.RemoveRange(i, c); 
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





    }
}
