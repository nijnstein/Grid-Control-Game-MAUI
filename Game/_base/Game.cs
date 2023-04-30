using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.Numerics;

namespace NSS.GameObjects
{
    public class Game : GameObject, IDrawable
    {
        public const float CanvasWidth = 1000f;
        public const float CanvasHeight = 1000f;
        public const float Gravity = 98.1f;

        public float GridSizeX = 1f;
        public float GridSizeY = 1f;

        public float ViewportWidth = 1; 
        public float ViewportHeight = 1;
        public float ViewportXscaler = 1;
        public float ViewportYscaler = 1;

        public float ViewportMarginTop = 10;
        public float ViewportMarginLeft = 10;
        public float ViewportMarginBottom = 40;
        public float ViewportMarginRight = 10;

        public float ShakeStrength = 250f;
        public Vector2 ShakeVector = Vector2.Zero; 
        public int ShakeFrameCountDown = 0; 

        public const float PlayerHitPoints = 1000f;

        public GameState GameState = GameState.Start;
        public bool ResetIntoPlayState = true;
        public GameInputState InputState = default; 

        public Color BorderColor = Colors.Gray;
        public float BorderStroke = 2f;
        public Color BackgroundColor = Color.FromRgb(10, 10, 10);
        public bool FillBackground = true;

        public Game() : base(null) 
        {
        }

        public Rect ViewRectangle => new Rect(0, 0, ViewportWidth + ViewportMarginLeft + ViewportMarginRight, ViewportHeight + ViewportMarginTop + ViewportMarginBottom);

        public virtual void ResetView(float width, float height)
        {
            ViewportWidth = width - Math.Min(width / 2, ViewportMarginLeft + ViewportMarginRight);
            ViewportHeight = height - Math.Min(height /2, ViewportMarginTop + ViewportMarginBottom);

            ViewportXscaler = 1f / CanvasWidth * ViewportWidth; 
            ViewportYscaler = 1f / CanvasHeight * ViewportHeight;

            this.Extent = new PointF(CanvasWidth / 2f, CanvasHeight / 2f);
            this.Position = this.Extent;
        }

        public void Reset() => Reset(ViewportWidth, ViewportHeight);

        public virtual void Reset(float width, float height)
        {
            this.Children.Clear(); 

            ResetView(width, height);

            ShakeVector = Vector2.Zero;
            ShakeFrameCountDown = 0;

            GameState = ResetIntoPlayState ? GameState.Play : GameState.Start;
        }
        
        public void SetInput(GameInputState state) => InputState = state;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (ViewportWidth <= 0 || ViewportHeight <= 0)
            {
                return;
            }

            DoRender(canvas, dirtyRect);
            DoPostRender(canvas, dirtyRect);
        }

        public override void Render(ICanvas canvas, RectF dirtyRect)
        {
            if(ViewportWidth <= 0 || ViewportHeight <= 0)
            {
                return; 
            }
            if (FillBackground)
            {
                PointF p = UnshakenPointToView(Position);
                PointF e = UnshakenPointToView(Extent);

                if (dirtyRect.IntersectsWith(new RectF(p.X, p.Y, e.X, e.Y)))
                {
                    // background frame 
                    canvas.StrokeColor = BorderColor;
                    canvas.StrokeSize = BorderStroke;
                    canvas.FillColor = BackgroundColor;
                    canvas.FillRectangle(p.X - e.X, p.Y - e.Y, e.X * 2f, e.Y * 2f);
                    canvas.DrawRectangle(p.X - e.X, p.Y - e.Y, e.X * 2f, e.Y * 2f);
                }
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (GameState == GameState.Play)
            {
                // loop all entities in the game and update them 
                foreach (GameObject go in Children)
                {
                    go.Update(deltaTime);
                }
            }

            // shake the world 
            UpdateShake(deltaTime);
        }

        public void StartShake(int framecount = 60)
        {
            ShakeFrameCountDown = Math.Max(framecount, ShakeFrameCountDown); 
        }

        public void UpdateShake(float deltaTime)
        {
            if (ShakeFrameCountDown > 0)
            {
                ShakeFrameCountDown--;
                if (ShakeFrameCountDown == 0)
                {
                    ShakeVector = Vector2.Zero;
                }
                else
                {
                    ShakeVector = new Vector2(
                        ShakeVector.X + (Random.Shared.NextSingle() - 0.5f) * ShakeStrength * deltaTime,
                        ShakeVector.Y + (Random.Shared.NextSingle() - 0.5f) * ShakeStrength * deltaTime);
                }
            }             
        }
        public async void PlayAudio(string sound, bool loop)
        {
            var audioPlayer = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(sound));
            if (loop)
            {
                audioPlayer.Loop = true;
            }
            audioPlayer.Play();
        }
    }
}
