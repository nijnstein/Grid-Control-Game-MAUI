using NSS;
using NSS.GameObjects;

namespace Grid.GameObjects
{

    public class Enemy : GridActor
    {
        public Enemy(GridGame game, Grid grid) : base(game, grid)
        {
            Color = Colors.Red;
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

        private MoveState MoveTowardsPlayer(float deltaTime)
        {
            // TODO : calc route to player with a*
  
            Player player = (Game as GridGame).Player;
            if (player == null) return MoveState.None;

            float x = player.Position.X - Position.X;
            float y = player.Position.Y - Position.Y;

            Direction dir = Direction.Left;

            if (Math.Abs(x) > Math.Abs(y))
            {
                if (x > 0) dir = Direction.Right;
                else
                if (x < 0) dir = Direction.Left;
            }
            else
            {
                if (y > 0) dir = Direction.Bottom;
                else
                if (y < 0) dir = Direction.Top;
            }

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
            ABPosition = (Speed * deltaTime).Clip(0f, GridPoint.Distance(a, Grid.Points[B], dir));
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
