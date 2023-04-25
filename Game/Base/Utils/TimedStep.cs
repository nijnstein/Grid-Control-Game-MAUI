
namespace NSS
{
    public class TimedStep
    {
        public TimedStep(float interval, float step, Func<int, float, float> valueUpdate)
        {
            Interval = interval;
            Step = step;
            Timer = 0;
            Counter = 0;
            ValueUpdateAction = valueUpdate;
        }
        
        public TimedStep Update(float delta)
        {
            Timer += delta; 
            if(Interval < Timer)
            {
                Counter++;
                while (Interval < Timer)
                {
                    // prevent queueing updates 
                    Timer -= Interval;
                }
                Value = ValueUpdateAction(Counter, Step); 
                return this; 
            }
            else
            {
                Value = 0; 
            }
            return this; 
        }

        public float Interval;
        public float Timer;  
        
        public int Counter;

        public float Step; 
        public float Value;

        public Func<int, float, float> ValueUpdateAction = (counter, step) => counter * step;
    }
}
