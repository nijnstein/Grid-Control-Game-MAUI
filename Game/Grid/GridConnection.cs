using NSS;

namespace Grid.GameObjects
{
    public struct GridConnection
    {
        public int Index = -1; 

        public int Left = -1;
        public int Top = -1;
        public int Right = -1;
        public int Bottom = -1;

        public GridConnection(int c, int l, int t, int r, int b)
        {
            Index = c; 
            Left = l;
            Top = t;
            Right = r;
            Bottom = b;         
        }

        /// <summary>
        /// </summary>
        /// <returns>true if point is part of connectionlist, -1 always returns false</returns>
        public bool ConnectsWith(int point)
        {
            return (point >= 0) & ((point == Left) | (point == Top) | (point == Right) | (point == Bottom));
        }

        public bool HasConnection(Direction type)
        {
            switch(type)
            {
                case Direction.Left: return Left >= 0;
                case Direction.Right: return Right >= 0;
                case Direction.Top: return Top >= 0;
                case Direction.Bottom: return Bottom >= 0;
                default: return false; 
            }
        }

        public bool Next(Direction type, bool ccw, out int output, out Direction dir)
        {
            int i = (int)type;
             for(int k = 0; k < 3; k++)
            {
                if (ccw)
                {
                    i = i == 3 ? 0 : i + 1;
                }
                else
                {
                    i = i == 0 ? 3 : i - 1;
                }
                switch ((Direction)i)
                {
                    case Direction.Left:   if (Left >= 0)   { dir = (Direction)i; output = Left;   return true; } break;
                    case Direction.Right:  if (Right >= 0)  { dir = (Direction)i; output = Right;  return true; } break;
                    case Direction.Top:    if (Top >= 0)    { dir = (Direction)i; output = Top;    return true; } break;
                    case Direction.Bottom: if (Bottom >= 0) { dir = (Direction)i; output = Bottom; return true; } break;
                }
            }
            dir = Direction.None;
            output = -1; 
            return false; 
        }
 
        public bool GetConnectionType(int pointIndex, out Direction type)
        {
            if(Left == pointIndex)
            {
                type = Direction.Left;
                return true; 
            }
            if (Top == pointIndex)
            {
                type = Direction.Top;
                return true;
            }
            if (Right == pointIndex)
            {
                type = Direction.Right;
                return true;
            }
            if (Bottom == pointIndex)
            {
                type = Direction.Bottom;
                return true;
            }

            type = Direction.None;
            return false; 
        }

        public bool TryGetConnected(Direction dir, out int pointIndex)
        {
            if (Left >= 0 & dir == Direction.Left)
            {
                pointIndex = Left; 
                return true;
            }
            if (Top >= 0 & dir == Direction.Top)
            {
                pointIndex = Top; 
                return true;
            }
            if (Right >= 0 & dir == Direction.Right)
            {
                pointIndex = Right; 
                return true;
            }
            if (Bottom >= 0 & dir == Direction.Bottom)
            {
                pointIndex = Bottom;
                return true;
            }
            pointIndex = -1; 
            return false; 
        }

        /// <summary>
        /// get the other connection when following a path
        /// </summary>
        /// <param name="from">connection started from as seen from current point</param>
        /// <param name="pointIndex">the new point index in the other direction</param>
        /// <param name="dir">the other direction</param>
        /// <returns>false if there are multiple other directions or if there is none</returns>
        public bool TryGetOther(Direction from, out int pointIndex, out Direction dir)
        {
            if (Left >= 0 & from == Direction.Left)
            {
                // verify only 1 other is set 
                if (Right >= 0 & Top == 0 & Bottom == 0)
                {
                    pointIndex = Right;
                    dir = Direction.Right;
                    return true;
                }
                else
                if (Right == 0 & Top >= 0 & Bottom == 0)
                {
                    pointIndex = Top;
                    dir = Direction.Top;
                    return true;
                }
                else
                if (Right == 0 & Top == 0 & Bottom >= 0)
                {
                    pointIndex = Bottom;
                    dir = Direction.Bottom;
                    return true;
                }
            }
            else
            if (Top >= 0 & from == Direction.Top)
            {
                if (Right >= 0 & Left == 0 & Bottom == 0)
                {
                    pointIndex = Right;
                    dir = Direction.Right;
                    return true;
                }
                else
                if (Right == 0 & Left >= 0 & Bottom == 0)
                {
                    pointIndex = Left;
                    dir = Direction.Left;
                    return true;
                }
                else
                if (Right == 0 & Left == 0 & Bottom >= 0)
                {
                    pointIndex = Bottom;
                    dir = Direction.Bottom;
                    return true;
                }
            }
            else
            if (Right >= 0 & from == Direction.Right)
            {
                if (Left >= 0 & Top == 0 & Bottom == 0)
                {
                    pointIndex = Left;
                    dir = Direction.Left;
                    return true;
                }
                else
                if (Left == 0 & Top >= 0 & Bottom == 0)
                {
                    pointIndex = Top;
                    dir = Direction.Top;
                    return true;
                }
                else
                if (Left == 0 & Top == 0 & Bottom >= 0)
                {
                    pointIndex = Bottom;
                    dir = Direction.Bottom;
                    return true;
                }
            }
            else
            if (Bottom >= 0 & from == Direction.Bottom)
            {
                if (Right >= 0 & Left == 0 & Top == 0)
                {
                    pointIndex = Right;
                    dir = Direction.Right;
                    return true;
                }
                else
                if (Right == 0 & Left >= 0 & Top == 0)
                {
                    pointIndex = Left;
                    dir = Direction.Left;
                    return true;
                }
                else
                if (Right == 0 & Left == 0 & Top >= 0)
                {
                    pointIndex = Top;
                    dir = Direction.Top;
                    return true;
                }
            }

            pointIndex = -1;
            dir = Direction.None;
            return false;
        }

        /// <summary>
        /// set the connection at the given direction vector 
        /// </summary>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        /// <param name="count"></param>
        public void SetConnection(PointF dir, int index)
        {
            if (dir.X == 0)
            {
                if (dir.Y < 0) Top = index;
                if (dir.Y > 0) Bottom = index; 
            }
            else
            {
                if (dir.X < 0) Left = index;
                if (dir.X > 0) Right = index; 
            }
        }

        public void SetConnection(Direction dir, int index)
        {
            switch (dir)
            {
                case Direction.Left: Left = index; break;
                case Direction.Right: Right = index; break;
                case Direction.Top: Top = index; break; 
                case Direction.Bottom: Bottom = index; break;
                default:
                case Direction.None: Left = Right = Top = Bottom = -1; break;
            }
        }

        public IEnumerable<int> EnumerateNeighbours()
        {
            if (Left >= 0) yield return Left;
            if (Top >= 0) yield return Top;
            if (Right >= 0) yield return Right;
            if (Bottom >= 0) yield return Bottom;
        }

    }
}
