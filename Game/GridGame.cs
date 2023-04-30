using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Graphics;
using NSS;
using NSS.GameObjects;
using System.Net;
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
                                                    
        public Color GridColor = Colors.Red; 
        public Color GridShadow = Colors.Gray;

        public Color PathColor = Colors.Yellow; 

        public Color BackgroundColor = Color.FromRgb(.07f, .04f, .07f);
        public Color BackgroundGradientColor = Color.FromRgb(.17f, .14f, .17f);

        public int Score;
        public double TotalTime;
        public double TotalPlayTime;

        public Direction InputDirection;
        public Direction NextDirection; 

        // prevent sticky keys 
        private float toggleJitter;

        public Action<GridGame> OnGameOver;
        public Action<GridGame> OnRunning;
        public Action<GridGame> OnWin;
        public Action<GridGame> OnPause;
        public Action<GridGame> OnStart; 

        public GridGame() : this(null, null, null, null, null) { } 

        public GridGame(Action<GridGame> onStart, Action<GridGame> onPause, Action<GridGame> onRunning, Action<GridGame> onGameOver, Action<GridGame> onWin)
        {
            FillBackground = false; 
            GameState = GameState.Start;
            ResetIntoPlayState = false;
            InputDirection = Direction.None;
            NextDirection = Direction.None;

            OnStart = onStart;
            OnRunning = onRunning;
            OnPause = onPause;
            OnGameOver = onGameOver; 
            OnWin = onWin;

            DoStart();
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

        public Direction UpdateDirection(Direction alternate)
        {
            InputDirection = alternate;
            NextDirection = Direction.None;
            return alternate;
        }

        #region Game States 


        public void DoStart()
        {
            GameState = GameState.Start;
            if (OnStart != null)
            {
                OnStart(this);
            }
        }
        public void DoRunning()
        {
            GameState = GameState.Play;
            if (OnRunning != null)
            {
                OnRunning(this);
            }
        }

        public void DoPause()
        {
            GameState = GameState.Paused;
            if (OnPause != null)
            {
                OnPause(this);
            }
        }
        public void DoWin()
        {
            if (OnWin != null)
            { 
                OnWin(this);
            }
        }

        public void DoGameOver(int j1 = -1, int j2 = -1)
        {
            if (j1 >= 0 && j2 >= 0 && j1 != j2)
            {
                // highlight segment intersecting
                new GridSegment(Game as GridGame, j1, j2);
            }

            // gameover 
            Game.GameState = GameState.GameOver;

            //
            // game over anymations/:    remove grid and drop all surfaces down,   or remove grid and vanish each surface one by one etc.. multple animations that are randomly called
            // 
            Game.ShakeFrameCountDown = 30;

            if (OnGameOver != null)
            {
                OnGameOver(this);
            }
        }

        #endregion

        #region Game Properties 
        public float PercentageOfGridControlled => Grid.Surfaces.Sum(x => x.Surface) / 100f;

        public bool CheckWinCondition(float minimum = 70) => PercentageOfGridControlled > minimum;

        #endregion

        #region Render / Update 

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // set input direction
            InputDirection = InputState.Direction; 

            // update total time 
            TotalTime += deltaTime;
            if (Game.GameState == GameState.Play)
            {
                TotalPlayTime += deltaTime;
            }

            // check win condition
            if (Game.GameState == GameState.Play)
            {
                if (CheckWinCondition())
                {
                    DoWin();
                }
            }

            // handle menu controls 
            if (toggleJitter <= 0 && InputState.Any)
            {
                toggleJitter = 0.2f; 

                switch (Game.GameState)
                {
                    case GameState.Play:
                        {
                            if (InputState.Escape)
                            {
                                DoPause(); 
                            }
                            break;
                        }

                    case GameState.Won:
                    case GameState.GameOver:
                        {
                            if (InputState.SpaceBar)
                            {
                                Reset();
                                DoRunning();
                            }
                            break;
                        }

                    case GameState.Start:
                        {
                            if (InputState.SpaceBar)
                            {
                                DoRunning(); 
                            }                            
                            break;
                        }

                    case GameState.Paused:
                        {
                            if (InputState.SpaceBar)
                            {
                                Reset();
                                DoRunning(); 
                            }
                            else 
                            if (InputState.Escape)
                            {
                                DoRunning(); 
                            }
                            break;
                        }
                }
            }
            toggleJitter = toggleJitter > 0 ? toggleJitter - deltaTime : 0;              
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            if (ViewportWidth <= 0 || ViewportHeight <= 0 || Grid == null)
            {
                return;
            }

            // fill background 
            LinearGradientBrush brush = new LinearGradientBrush()
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            brush.GradientStops.Add(new GradientStop(BackgroundColor, 0));
            brush.GradientStops.Add(new GradientStop(BackgroundGradientColor, 0.4f));
            brush.GradientStops.Add(new GradientStop(BackgroundColor, 1f));

            Rect rcView = new Rect(0, 0, ViewportWidth + ViewportMarginLeft + ViewportMarginRight, ViewportHeight + ViewportMarginTop + ViewportMarginBottom);
            canvas.FillColor = BackgroundColor;
            canvas.SetFillPaint(brush, rcView);
            canvas.FillRectangle(rcView);

            Color shakeColor = Game.ShakeFrameCountDown % 2 == 1 ? Colors.Red : Colors.WhiteSmoke;

            // the scaling from grid to the internal canvas (which is later transformed to viewspace) 
            float gx = (1f / (float)GridWidth) * CanvasWidth;
            float gy = (1f / (float)GridHeight) * CanvasHeight;

            // draw grid connections
            canvas.StrokeColor = GridColor;
            canvas.StrokeLineJoin = LineJoin.Bevel;
            canvas.StrokeLineCap = LineCap.Butt;
            canvas.StrokeDashPattern = null;
            canvas.FillColor = Colors.White;
            canvas.StrokeSize = 2;
            canvas.SetShadow(new SizeF(0, 0), 0, GridShadow);

            int pathLimit = Grid.CurrentPath.Count == 0 ? Grid.Connections.Count : Grid.Connections.Count - Grid.CurrentPath.Count;
            for (int i = 0; i < Grid.Connections.Count; i++)
            {
                GridConnection connection = Grid.Connections[i];
                GridPoint point = Grid.Points[connection.Index];
                PointF p1 = PointToView(new PointF(point.X * gx, point.Y * gy));

                // draw only connections to the right and bottom preventing overdraw 
                if (connection.Right >= 0)
                {
                    canvas.StrokeColor = (connection.Right >= pathLimit && i >= pathLimit) ? PathColor : GridColor;
                    GridPoint b = Grid.Points[connection.Right];
                    PointF p2 = new PointF(b.X * gx, b.Y * gy);
                    canvas.DrawLine(p1, PointToView(p2));
                }
                if (connection.Bottom >= 0)
                {
                    canvas.StrokeColor = (connection.Bottom >= pathLimit && i >= pathLimit) ? PathColor : GridColor;
                    GridPoint b = Grid.Points[connection.Bottom];
                    PointF p2 = new PointF(b.X * gx, b.Y * gy);
                    canvas.DrawLine(p1, PointToView(p2));
                }

                // attenuate the connection point with a filled dot 
                canvas.FillColor = Colors.White;
                canvas.FillCircle(p1, DOT_SCALE * gx);
            }
        }

        public override void PostRender(ICanvas canvas, Rect dirty)
        {
#if DEBUG
            if ((Game.GameState & (GameState.Play | Game.GameState)) != 0)
            {
                float gx = (1f / (float)GridWidth) * CanvasWidth;
                float gy = (1f / (float)GridHeight) * CanvasHeight;

                for (int i = 0; i < Grid.Connections.Count; i++)
                {
                    GridConnection connection = Grid.Connections[i];
                    GridPoint point = Grid.Points[connection.Index];
                    PointF p1 = PointToView(new PointF(point.X * gx, point.Y * gy));

                    string s = $"{i} ({connection.Left},{connection.Top},{connection.Right},{connection.Bottom})";
                    SizeF r = canvas.GetStringSize(s, Font.Default, 14);

                    RectF rc = new RectF(p1.X, p1.Y, r.Width, r.Height);

                    canvas.Font = Font.Default;
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 12;
                    canvas.FillColor = Colors.Black;
                    canvas.FillRectangle(rc);
                    canvas.DrawString(s, rc, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
#endif 

            // Paused header 
            switch (Game.GameState)
            {
                case GameState.Play:
                    {
                        canvas.FontColor = Colors.Red;
                        canvas.Font = Font.DefaultBold; 
                        canvas.FontSize = 20;
                        canvas.SetShadow(new SizeF(0, 0), 0, Colors.Black);

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

                default: 
                    {
                        DrawBackground(canvas);
                        break;
                    }
            }

            void DrawBackground(ICanvas canvas)
            {
                LinearGradientBrush brush = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                brush.GradientStops.Add(new GradientStop(Color.FromRgba(.1f, .1f, .1f, .6f), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromRgba(.2f, .2f, .2f, .6f), 0.4f));
                brush.GradientStops.Add(new GradientStop(Color.FromRgba(.1f, .1f, .1f, .6f), 1f));

                Rect rcView = new Rect(0, 0, ViewportWidth + ViewportMarginLeft + ViewportMarginRight, ViewportHeight + ViewportMarginTop + ViewportMarginBottom);

                canvas.SetFillPaint(brush, rcView);
                canvas.FillRectangle(rcView); 
            }
        }
        #endregion 
    }
}
