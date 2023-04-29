using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{

    public class Enemy : GridActor
    {
        public Enemy(GridGame game, Grid grid) : base(game, grid)
        {
            Color = Colors.Yellow;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            MoveState moved = MoveState.None;

            if(IsOnPoint)
            {
                moved = MoveTowardsPlayer(deltaTime); 
            }
            else
            if(IsOnSegment)
            {
                moved = MoveAlongSegment(deltaTime);
            }

            CheckSegmentPosition(); 

            if (moved == MoveState.Moved)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// if moving onSegment, check if we reached a point and should toggle into IsOnPoint 
        /// </summary>
        protected virtual void CheckSegmentPosition()
        {
            // if moving onSegment after updating, check if we hit a point (even if starting from onPoint, speed might be high due to delta)
            if (IsOnSegment)
            {
                if (ABPosition <= 0)
                {
                    // on point A 
                    B = -1;
                }
                else
                if (ABPosition >= GridPoint.DistanceToNeighbour(Grid.Points[A], Grid.Points[B]))
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


        private MoveState MoveTowardsPlayer(float deltaTime)
        {
            Player player = (Game as GridGame).Player;
            if (player == null) return MoveState.None;

            int[] path = Grid.SearchPathTo(A, player.A);
            if (path != null)
            {
                if (path.Length <= 1)
                {

                }
                else
                {
                    GridPoint a = Grid.Points[A];
                    GridConnection c = Grid.Connections[A];
                    if (c.GetConnectionType(path[1], out Direction dir))
                    {
                        return MoveInDirection(deltaTime, a, c, dir, false);
                    }
                }
                return MoveState.None; 
            }
            else
            {
                Direction dir = (Direction)Random.Shared.Next(4); 
                GridPoint a = Grid.Points[A];
                GridConnection c = Grid.Connections[A];
                MoveState moved = MoveState.None;

                // if we cant move freely, force moving into section 
                for (int i = (int)dir, j = 0, k = 0; k < 2 & moved != MoveState.Moved; k++)
                {
                    // first try to move in prefered direction
                    moved = MoveInDirection(deltaTime, a, c, dir, k == 1);

                    // then try the others 
                    while (moved != MoveState.Moved & j < 3)
                    {
                        i = i > 3 ? 0 : i + 1;
                        j++;
                        moved = MoveInDirection(deltaTime, a, c, (Direction)i, k == 1);
                    }
                }
             
                return moved;
            }
        }

        protected virtual MoveState MoveInDirection(float deltaTime, GridPoint a, GridConnection c, Direction dir, bool forced)
        {
            if (c.TryGetConnected(dir, out int pointIndex))
            {
                if(!forced)
                {
                    List<GridActor> enemies = (Game as GridGame).Enemies;

                    // check for other enemies on the target segment, we dont move into a segment that contains an enemy unless forced = true  
                    if(enemies.Any(x => (x.A == A || x.A == pointIndex) && (x.B == A || x.B == pointIndex)))
                    {
                        return MoveState.None; 
                    }
                }
                return MoveTowards(deltaTime, a, pointIndex, dir);
            }
            else
            {
                return MoveState.None;
            }
        }

        protected virtual MoveState MoveTowards(float deltaTime, GridPoint a, int b, Direction dir)
        {
            B = b;
            ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.DistanceToNeighbour(a, Grid.Points[B], dir));
            return MoveState.Moved;
        }

        private MoveState MoveAlongSegment(float deltaTime)
        {
            if (A >= 0 && B >= 0)
            {
                return MoveAlongConnection(deltaTime, A, B);
            }
            return MoveState.None; 
        }

    }
}
