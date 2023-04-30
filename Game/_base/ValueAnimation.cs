using System.ComponentModel.DataAnnotations;

namespace NSS.GameObjects
{
    public enum StepFunction
    {
        /// <summary>
        /// linear stepping from 0..1 
        /// </summary>
        Linear, 

        /// <summary>
        /// linear stepping from 0..1..0 
        /// </summary>
        Linear2Ways,
    }

    public class ValueAnimation
    {
        public readonly GameObject GameObject; 

        /// <summary>
        ///  
        /// </summary>
        /// <param name="parent">parent go to add this animation to</param>
        /// <param name="getValue"></param>
        /// <param name="totalSeconds"></param>
        public ValueAnimation(GameObject parent, Action<float> getValue, float totalSeconds, StepFunction stepFunction, bool disposeOnStop = false, bool loop = false)
        {
            GameObject = parent; 
            Running = true; 
            StepAction = getValue; 
            Step = 0; 
            RTotalSeconds = 1f / totalSeconds;
            DisposeOnStop = disposeOnStop;
            Loop = loop;
            Forward = true;
            
            switch(stepFunction)
            {
                case GameObjects.StepFunction.Linear:
                    StepFunction = (float deltaTime) =>
                    {
                        Step = MathF.Min(1, MathF.Max(0, Step + deltaTime * this.RTotalSeconds));
                        if (Step > 1)
                        {
                            Step = (Step >= 1) & this.Loop ? 0 : 1f;
                            Running = (Step < 1) | Loop;
                        }
                    };
                    break;

                case GameObjects.StepFunction.Linear2Ways: 
                    StepFunction = (float deltaTime) =>
                    {
                        if (Forward)
                        {
                            Step = MathF.Min(1, MathF.Max(0, Step + deltaTime * this.RTotalSeconds * 2));
                            if (Step >= 1)
                            {
                                Forward = false;
                                Step = Math.Min(1f, Step - (Step - 1f)); 
                            }
                        }
                        else
                        {
                            Step = MathF.Min(1, MathF.Max(0, Step - deltaTime * this.RTotalSeconds * 2));
                            if (Step <= 0)
                            {
                                Step = Math.Max(Step + Step, 0);
                                Forward = true;
                                Running = Loop;
                            }
                        }
                    };
                    break;
            }

            if (parent != null)
            {
                parent.Animations.Add(this);
            }
        }

        public Action<float> StepAction;
        public Action<float> StepFunction;
        public Func<ValueAnimation> OnStopFunction; 

        /// <summary>
        /// step from 0..1, 1 = @totalSeconds 
        /// </summary>
        [Range(0f, 1f)]
        public float Step;

        /// <summary>
        /// reciprocal of duration of animation in seconds 
        /// </summary>
        protected readonly float RTotalSeconds;

        /// <summary>
        /// true if animation steps forward (StepFunction reverse) 
        /// </summary>
        public bool Forward = true; 

        /// <summary>
        /// true if running 
        /// </summary>
        public bool Running;

        /// <summary>
        /// if true animation is looped 
        /// </summary>
        public readonly bool Loop = false;

        /// <summary>
        /// if true then the animation is removed from parent on stop and disposed 
        /// </summary>
        public readonly bool DisposeOnStop;

        /// <summary>
        /// total duration in seconds 
        /// </summary>
        public float TotalSeconds => 1 / RTotalSeconds;

        public ValueAnimation OnStop(Func<ValueAnimation> onStopHandler)
        {
            OnStopFunction = onStopHandler;
            return this; 
        }

        /// <summary>
        /// reset to initial state
        /// </summary>
        public void Reset() 
        { 
            Step = 0;
            Running = false;
        }

        public void Play()
        {
            Running = true;
        }

        public void Stop()
        {
            Running = false; 
        }

        public void Update(float deltaTime)
        {
            if (Running)
            {
                StepFunction(deltaTime);
                StepAction(Step);

                if (!Running & GameObject != null)
                {
                    if(OnStopFunction != null)
                    {
                        OnStopFunction(); 
                    }
                    if (DisposeOnStop)
                    {
                        GameObject.Animations.Remove(this);
                    }
                }
            }
        }
    }
}
