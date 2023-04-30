namespace NSS.GameObjects
{
    public struct GameInputState
    {
        public bool Up = false;
        public bool Down = false;
        public bool Left = false;
        public bool Right = false;

        public bool Escape = false;
        public bool SpaceBar = false; 

        public bool Move => Up | Down | Left | Right;
        public Direction Direction 
        {
            get
            {
                if (Up) return Direction.Top;
                if (Down) return Direction.Bottom;
                if (Left) return Direction.Left;
                if (Right) return Direction.Right;
                return Direction.None; 
            }
        }

        public bool Any => Move | Escape | SpaceBar;

        public GameInputState()
        {
        }        
    }
}
