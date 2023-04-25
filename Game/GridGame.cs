using Microsoft.Maui.Animations;
using NSS.GameObjects;
using Font = Microsoft.Maui.Graphics.Font;

namespace Grid.GameObjects
{
    public class GridGame : Game , IDrawable
    {
        public const int GridWidth = 200;
        public const int GridHeight = 200;

        public const float DOT_SCALE = 0.5f;

        public Grid Grid; 
        public Player Player;
        public List<GridActor> Enemies;

        public int Score;
        public double TotalTime;
        public double TotalPlayTime; 

        // prevent sticky keys 
        private float toggleJitter = 0; 

               
        public GridGame()
        {
            GameState = GameState.Start;
            ResetIntoPlayState = false; 
        }

        void Initialize()
        {
            Grid = new Grid(this, GridWidth, GridHeight);

            // setup graph borders 
            Grid.Points.Add(new GridPoint(0, 0, 0));
            Grid.Points.Add(new GridPoint(1, GridWidth, 0));
            Grid.Points.Add(new GridPoint(2, GridWidth, GridHeight));
            Grid.Points.Add(new GridPoint(3, 0, GridHeight));

            Grid.Connections.Add(new GridConnection(0, -1, -1,  1,  3));
            Grid.Connections.Add(new GridConnection(1,  0, -1, -1,  2));
            Grid.Connections.Add(new GridConnection(2,  3,  1, -1, -1));
            Grid.Connections.Add(new GridConnection(3, -1,  0,  2, -1));

            // the actors 
            Player = new Player(this, Grid);
            Player.SetPosition(0, -1, 0); // position between point 0 and 1 at position 0 (at point 0)
            
            Enemies = new List<GridActor>();
            Enemies.Add(new Enemy(this, Grid));
            Enemies.Add(new Enemy(this, Grid));
            Enemies.Add(new Enemy(this, Grid));
            Enemies[0].SetPosition(1);
            Enemies[1].SetPosition(2);
            Enemies[2].SetPosition(3);

            // reset the score  
            Score = 0;
            TotalTime = 0;
            TotalPlayTime = 0; 
        }

        public override void Reset(float viewWidth, float viewHeight)
        {
            base.Reset(viewWidth, viewHeight);
            Initialize();
        }


        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            TotalTime += deltaTime;
            if (Game.GameState == GameState.Play)
            {
                TotalPlayTime += deltaTime;
            }

            if (toggleJitter <= 0 && InputState.Any)
            {
                toggleJitter = 0.2f; 

                switch (Game.GameState)
                {
                    case GameState.Play:
                        {
                            if (InputState.Escape)
                            {
                                GameState = GameState.Paused;
                            }
                            break;
                        }

                    case GameState.Won:
                    case GameState.GameOver:
                        {
                            if (InputState.SpaceBar)
                            {
                                Reset();
                                GameState = GameState.Play; 
                            }
                            break;
                        }

                    case GameState.Start:
                        {
                            if (InputState.SpaceBar)
                            {
                                GameState = GameState.Play;
                            }                            
                            break;
                        }

                    case GameState.Paused:
                        {
                            if (InputState.SpaceBar)
                            {
                                Reset();
                                GameState = GameState.Play;
                            }
                            else 
                            if (InputState.Escape)
                            {
                                GameState = GameState.Play;
                            }
                            break;
                        }
                }
            }
            toggleJitter = toggleJitter > 0 ? toggleJitter - deltaTime : 0;              
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            if(ViewportWidth <= 0 || ViewportHeight <= 0 || Grid == null)
            {
                return;
            }

            // the border
            Color shakeColor = Game.ShakeFrameCountDown % 2 == 1 ? Colors.Red : Colors.WhiteSmoke;
            canvas.StrokeColor = shakeColor;
            canvas.StrokeSize = this.ShakeFrameCountDown > 0 ? 9 : 3; 
            canvas.StrokeDashPattern = new float[] { 0.5f };
            canvas.DrawRectangle(0, 0, ViewportWidth + ViewportMarginLeft + ViewportMarginRight, ViewportHeight + ViewportMarginTop + ViewportMarginBottom); 
  
            // the scaling from grid to the internal canvas (which is later transformed to viewspace) 
            float gx = (1f / (float)GridWidth) * CanvasWidth;
            float gy = (1f / (float)GridHeight) * CanvasHeight;

            // draw grid connections
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeLineJoin = LineJoin.Bevel;
            canvas.StrokeLineCap = LineCap.Butt;
            canvas.StrokeDashPattern = null;
            canvas.FillColor = Colors.White;
            canvas.StrokeSize = 2; 
            for (int i = 0; i < Grid.Connections.Count; i++)
            {
                GridConnection connection = Grid.Connections[i];
                GridPoint point = Grid.Points[connection.Index];
                PointF p1 = PointToView(new PointF(point.X * gx, point.Y * gy));

                // draw only connections to the right and bottom preventing overdraw 
                if (connection.Right >= 0)
                {
                    GridPoint b = Grid.Points[connection.Right]; 
                    PointF p2 = new PointF(b.X * gx, b.Y * gy);
                    canvas.DrawLine(p1, PointToView(p2));
                }
                if (connection.Bottom >= 0)
                {
                    GridPoint b = Grid.Points[connection.Bottom];
                    PointF p2 = new PointF(b.X * gx, b.Y * gy);
                    canvas.DrawLine(p1, PointToView(p2));
                }

                // attenuate the connection point with a filled dot 
                canvas.FillCircle(p1, DOT_SCALE * gx); 
            }

            // Paused header 
            switch (Game.GameState)
            {
                case GameState.Play:
                    {
                        canvas.FontColor = Colors.White;
                        canvas.FontSize = 20;
                        canvas.DrawString($"SCORE: {Score}",
                            10 + ViewportMarginLeft, 
                            -10 + ViewportMarginTop + ViewportMarginBottom, 
                            ViewportWidth - 20, 
                            ViewportHeight, 
                            HorizontalAlignment.Right,
                            VerticalAlignment.Bottom);

                        canvas.DrawString($"TIME: {TotalPlayTime.ToString("0")}",
                            10 + ViewportMarginLeft, 
                            -10 + ViewportMarginTop + ViewportMarginBottom, 
                            ViewportWidth - 20, 
                            ViewportHeight, 
                            HorizontalAlignment.Left, 
                            VerticalAlignment.Bottom);
                        break; 
                    }

                case GameState.Paused:
                    {
                        canvas.FontColor = Colors.White;
                        canvas.FontSize = 40;
                        canvas.DrawString("PAUSE\n\n[esc] to continue\n[space] to restart", 0, ViewportHeight / 4, ViewportWidth, ViewportHeight / 2, HorizontalAlignment.Center, VerticalAlignment.Center);
                        break;
                    }

                case GameState.GameOver:
                    {
                        canvas.FontColor = Colors.White;
                        canvas.FontSize = 40;
                        canvas.DrawString("YOU SUCK\n\n[space] to restart", 0, ViewportHeight / 4, ViewportWidth, ViewportHeight / 2, HorizontalAlignment.Center, VerticalAlignment.Center);
                        break;
                    }

                case GameState.Start:
                    {
                        canvas.FontColor = Colors.Green;
                        canvas.FontSize = ViewportHeight / 10;
                        canvas.SetShadow(new SizeF(6, 6), 4, Colors.Yellow);
                        canvas.Font = Font.DefaultBold;

                        canvas.DrawString("START", 0, 0, ViewportWidth, ViewportHeight / 2, HorizontalAlignment.Center, VerticalAlignment.Center);

                        canvas.FontColor = Colors.White;
                        canvas.FontSize = 30;
                        canvas.SetShadow(new SizeF(0, 0), 0, Colors.Gray);
                        canvas.Font = Font.Default;

                        canvas.DrawString("[space] to start\n[arrow-keys] to move\n[esc] to pause", 0, ViewportHeight / 2, ViewportWidth, ViewportHeight / 2, HorizontalAlignment.Center, VerticalAlignment.Center);
                        break;
                    }
            }
        }
    }
}
