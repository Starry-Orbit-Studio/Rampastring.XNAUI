using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Rampastring.Tools;

namespace Rampastring.XNAUI.PlatformSpecific
{
    internal class WindowsGameWindowManager : IGameWindowManager
    {
        private Game _game;
        private GameWindow _window;
        public WindowsGameWindowManager(Game game)
        {
            _game = game;
            _window = game.Window;
        }

        private bool closingPrevented = false;

        public event EventHandler GameWindowClosing;


        /// <summary>
        /// Centers the game window on the screen.
        /// </summary>
        public void CenterOnScreen()
        {
            SDL2.SDL.SDL_GetCurrentDisplayMode(0, out var dm);
            int x = (dm.w - _game.Window.ClientBounds.Width) / 2;
            int y = (dm.h - _game.Window.ClientBounds.Height) / 2;

            SDL2.SDL.SDL_SetWindowPosition(_window.Handle, x, y);
        }

        /// <summary>
        /// Enables or disables borderless windowed mode.
        /// </summary>
        /// <param name="value">A boolean that determines whether borderless 
        /// windowed mode should be enabled.</param>
        public void SetBorderlessMode(bool value)
        {
            _game.Window.IsBorderlessEXT = value;
        }

        /// <summary>
        /// Minimizes the game window.
        /// </summary>
        public void MinimizeWindow()
        {
            if (_window == null)
                return;

            SDL2.SDL.SDL_MinimizeWindow(_window.Handle);
        }

        /// <summary>
        /// Maximizes the game window.
        /// </summary>
        public void MaximizeWindow()
        {
            if (_window == null)
                return;

            SDL2.SDL.SDL_RestoreWindow(_window.Handle);
        }

        /// <summary>
        /// Hides the game window.
        /// </summary>
        public void HideWindow()
        {
            if (_window == null)
                return;

            SDL2.SDL.SDL_HideWindow(_window.Handle);
        }

        /// <summary>
        /// Shows the game window.
        /// </summary>
        public void ShowWindow()
        {
            if (_window == null)
                return;

            SDL2.SDL.SDL_ShowWindow(_window.Handle);
        }

        /// <summary>
        /// Flashes the game window on the taskbar.
        /// </summary>
        public void FlashWindow()
        {
            if (_window == null)
                return;

            //WindowFlasher.FlashWindowEx(_window);
        }

        /// <summary>
        /// Sets the icon of the game window to an icon that exists on a specific
        /// file path.
        /// </summary>
        /// <param name="path">The path to the icon file.</param>
        public void SetIcon(string path)
        {
            if (_window == null)
                return;

            var ico = new Bitmap(path);
            var p_bmp = ico.GetHbitmap();

            var surface = SDL2.SDL.SDL_CreateRGBSurfaceFrom(p_bmp, ico.Width, ico.Height, 24, 32, 0x0f00, 0x00f0, 0x000f, 0xf000);
            SDL2.SDL.SDL_SetWindowIcon(_window.Handle, surface);
            SDL2.SDL.SDL_FreeSurface(surface);
        }

        /// <summary>
        /// Returns the IntPtr handle of the game window on Windows.
        /// On other platforms, returns IntPtr.Zero.
        /// </summary>
        public IntPtr GetWindowHandle()
        {
            if (_window == null)
                return IntPtr.Zero;

            return _window.Handle;
        }

        /// <summary>
        /// Enables or disables the "control box" (minimize/maximize/close buttons) for the game form.
        /// </summary>
        /// <param name="value">True to enable the control box, false to disable it.</param>
        public void SetControlBox(bool value)
        {
            if (_window == null)
                return;

            //_window.ControlBox = value;
        }

        /// <summary>
        /// Prevents the user from closing the game form by Alt-F4.
        /// </summary>
        public void PreventClosing()
        {
            if (_window == null)
                return;

            //if (!closingPrevented)
            //    _window.FormClosing += _window_FormClosing;
            closingPrevented = true;
        }

        /// <summary>
        /// Allows the user to close the game form by Alt-F4.
        /// </summary>
        public void AllowClosing()
        {
            if (_window == null)
                return;

            //_window.FormClosing -= _window_FormClosing;
            closingPrevented = false;
        }

        public bool HasFocus()
        {
            if (_window == null)
                return _game.IsActive;

            var flag = SDL2.SDL.SDL_GetWindowFlags(GetWindowHandle());
            return (flag & 0x00000200) == 0x00000200;
        }
    }
}