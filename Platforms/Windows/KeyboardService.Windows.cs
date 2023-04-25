using Microsoft.Maui;

namespace NSS.GameObjects.PlatformServices
{
    public partial class InputService
    {
        public partial GameInputState GetInputState()
        {                  
            return new GameInputState()
            {
                Left = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_LEFT), 
                Up = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_UP),
                Right = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_RIGHT),
                Down = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_DOWN),
                Escape = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_ESCAPE),
                SpaceBar = CustomWindows.KeyboardUtils.IsKeyDown(CustomWindows.VirtualKeyStates.VK_SPACE),
            };
        }
    }  
}
 
