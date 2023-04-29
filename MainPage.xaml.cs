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
        }

        public void Initialize()
        {
            var ms = 1000.0 / PhysicsFPS;
            var ts = TimeSpan.FromMilliseconds(ms);

            Dispatcher.StartTimer(ts, TimerLoop);
            lastRenderTick = DateTime.Now;

            Input = new InputService(); 
            Game = new GridGame();
                                    
            view.Drawable = Game;
            Game.Reset((float)view.Width, (float)view.Height);
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
            const int targetFPS = 60; 

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

            // update input state at 60 fps
            if ((now - lastInputTick).TotalSeconds >= 1f / targetFPS)
            {
                lastInputTick = now;
                InputState = Input.GetInputState(); 
            }

            // update renderer as needed to reach 60fps
            if ((now - lastRenderTick).TotalSeconds >= 1f / targetFPS)
            {
                lastRenderTick = now;

                // make sure view is aligned 
                ResetViewIfNeeded();

                // invalidate frame
                InvalidateViews();
            }
            return true;
        }
    }
}