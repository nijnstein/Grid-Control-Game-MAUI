using Grid.GameObjects;
using Microsoft.Maui.Platform;
using NSS.GameObjects;
using NSS.GameObjects.PlatformServices; 


namespace Grid
{
    public partial class MainPage : ContentPage
    {
        public const double PhysicsFPS = 60 * 5;
        public const double TargetFPS = 30;

        private DateTime lastRenderTick = DateTime.MinValue;
        private DateTime lastInputTick = DateTime.MinValue; 
        private DateTime lastPhysicsTick = DateTime.MinValue;

        private GridGame Game;
        private InputService Input;
        private GameInputState InputState; 

        public MainPage()
        {
            InitializeComponent();
            Initialize();

            //Game.PlayAudio("hot-pursuit-loop.mp3", true);
            Game.PlayAudio("final-act-loop.mp3", true);
        }

        public void Initialize()
        {
            var ms = 1000.0 / PhysicsFPS;
            var ts = TimeSpan.FromMilliseconds(ms);

            Dispatcher.StartTimer(ts, TimerLoop);
            lastRenderTick = DateTime.Now;

            Input = new InputService();           
            Game = new GridGame(OnStart, OnPause, OnRunning, OnGameOver, OnWin);
                                    
            view.Drawable = Game;
            Game.Reset((float)view.Width, (float)view.Height);
            Game.DoStart(); 
        }

        public void OnStart(GridGame game)
        {
            title.IsVisible = true;
            title.Text = "GRID CONTROL";

            information.IsVisible = true;
            information.Text = "[space] to start\n[arrow-keys] to move\n[esc] to pause";
        }

        public void OnRunning(GridGame game)
        {
            title.IsVisible = false;
            information.IsVisible = false;
        }

        public void OnPause(GridGame game)
        {
            title.IsVisible = true;
            title.Text = "PAUSED";

            information.IsVisible = true; 
            information.Text = "[esc] to continue\n[space] to restart";
        }

        public void OnGameOver(GridGame game)
        {
            title.IsVisible = true;
            title.Text = "GAME OVER";

            information.IsVisible = true;
            information.Text = "[esc] to restart";
        }

        public void OnWin(GridGame game)
        {
            title.IsVisible = true;
            title.Text = "YOU WON";

            information.IsVisible = true;
            information.Text = "[esc] to restart";
        }

        public void ResetView()
        {
            Game.ResetView((float)view.Width, (float)view.Height);
        }
        public void InvalidateViews()
        {
            view.Invalidate();
        }

        void ResetViewIfNeeded()
        {
            if (Game.ViewportWidth != view.Width || Game.ViewportHeight != view.Height)
            {
                Game.ResetView((float)view.Width, (float)view.Height);
            }
        }

        private void GameView_SizeChanged(object sender, EventArgs e)
        {
            ResetView();
        }

        void OnResetClicked(object sender, EventArgs e)
        {
            Game.Reset((float)view.Width, (float)view.Height);
        }

        private bool TimerLoop()
        {
            int targetFPS = Game.GameState == GameState.Play ? 30 : 1;
            int inputFPS = Game.GameState == GameState.Play ? 30 : 15;

            // skip the first tick 
            if (lastPhysicsTick == DateTime.MinValue)
            {
                lastPhysicsTick = DateTime.Now;
                return true;
            }

            // get deltaTime 
            DateTime now = DateTime.Now;
            double dt = (DateTime.Now - lastPhysicsTick).TotalSeconds;

            // update physics 
            lastPhysicsTick = now;
            try
            {
                Game.SetInput(InputState); 
                Game.Update(MathF.Max((float)dt, 1f / targetFPS));
            }
            catch
            {
                // boom
            }

            // update input state  
            if ((now - lastInputTick).TotalSeconds >= 1f / inputFPS)
            {
                lastInputTick = now;
                InputState = Input.GetInputState(); 
            }

            // update renderer as needed to reach target fps
            if ((now - lastRenderTick).TotalSeconds >= 1f / targetFPS)
            {
                lastRenderTick = now;

                // make sure view is aligned 
                ResetViewIfNeeded();

                // invalidate frame
                InvalidateViews();

                // update score 
                if (Game.GameState == GameState.Play)
                {
                    time.Text = $"TIME: {Game.TotalPlayTime.ToString("0")}";
                    progress.Text = $"{(Game.Score / 100).ToString("0")}%";
                    score.Text = $"SCORE: {Game.Score.ToString("0")}";
                }
            }

            return true;
        }
    }
}