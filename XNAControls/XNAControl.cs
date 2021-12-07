using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using FontStashSharp;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI.Input;

using Shimakaze;

// !!DO NOT USING THIS NAMESPACE!!
// using System.Diagnostics.Tracing;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// The base class for a XNA-based UI control.
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public partial class XNAControl : DrawableGameComponent
    {
        private bool _ignoreInputOnFrame = false;
        private bool _isChangingSize = false;
        private bool CursorOnControl = false;
        private bool isActive = false;
        private const double DOUBLE_CLICK_TIME = 1.0;
        private ControlDrawMode drawMode = ControlDrawMode.NORMAL;
        private float alpha = 1.0f;
        private List<XNAControl> _drawList = new List<XNAControl>();
        private List<XNAControl> _updateList = new List<XNAControl>();
        private readonly List<XNAControl> _childAddQueue = new List<XNAControl>();
        private readonly List<XNAControl> _childRemoveQueue = new List<XNAControl>();
        private readonly List<XNAControl> _children = new List<XNAControl>();
        [Event(EventSummary = "Raised when the control's parent is changed.", PropertySummary = "Gets or sets the parent of this control.")]
        private XNAControl _parent;
        [Event(SkipEvent = true, SkipMethod = true, MethodName = "OnClientRectangleUpdated", PropertySummary = "The X-coordinate of the control relative to its parent's location.")]
        private int _x;
        [Event(SkipEvent = true, SkipMethod = true, MethodName = "OnClientRectangleUpdated", PropertySummary = "The Y-coordinate of the control relative to its parent's location.")]
        private int _y;
        [Event(SkipEvent = true, SkipMethod = true, MethodName = "OnSizeChanged", PropertySummary = "The width of the control.")]
        private int _width;
        [Event(SkipEvent = true, SkipMethod = true, MethodName = "OnSizeChanged", PropertySummary = "The height of the control.")]
        private int _height;
        private int _scaling = 1;
        private int _initScaling;

        private KeyValuePair<string, SpriteFontBase> _fontCache = new KeyValuePair<string, SpriteFontBase>(string.Empty, null);
        [Event]
        private string _font;
        [Event]
        private int _fontSize;
        [Event(IsVirtualProperty = true, PropertySummary = "Raised whenever the text of the text box is changed, by the user or programmatically.")]
        protected string _text;

        public int ScaledWidth => Width * Scaling;

        public int ScaledHeight => Height * Scaling;

        /// <summary>
        /// Shortcut for accessing Bottom.
        /// </summary>
        public int Bottom => Y + Height;

        /// <summary>
        /// Shortcut for accessing Right.
        /// </summary>
        public int Right => X + Width;


        /// <summary>
        /// Creates a new control instance.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNAControl(WindowManager windowManager) : base(windowManager.Game)
        {
            WindowManager = windowManager;
        }


        /// <summary>
        /// Holds a reference to the cursor.
        /// </summary>
        protected Cursor Cursor => WindowManager.Cursor;

        /// <summary>
        /// Holds a reference to the keyboard.
        /// </summary>
        protected RKeyboard Keyboard => WindowManager.Keyboard;

        /// <summary>
        /// Raised when the mouse cursor enters the control's area.
        /// </summary>
        public event EventHandler MouseEnter;

        /// <summary>
        /// Raised once when the left mouse button is pressed down while the
        /// cursor is inside the control's area.
        /// </summary>
        public event EventHandler MouseLeftDown;

        /// <summary>
        /// Raised once when the right mouse button is pressed down while the
        /// cursor is inside the control's area.
        /// </summary>
        public event EventHandler MouseRightDown;

        /// <summary>
        /// Raised when the mouse cursor leaves the control's area.
        /// </summary>
        public event EventHandler MouseLeave;

        /// <summary>
        /// Raised when the mouse cusor moves while inside the control's area.
        /// </summary>
        public event EventHandler MouseMove;

        /// <summary>
        /// Raised each frame when the mouse cursor is inside the control's area.
        /// </summary>
        public event EventHandler MouseOnControl;

        /// <summary>
        /// Raised when the scroll wheel is used while the cursor is inside
        /// the control.
        /// </summary>
        public event EventHandler MouseScrolled;

        /// <summary>
        /// Raised when the left mouse button is clicked (pressed and released)
        /// while the cursor is inside the control's area.
        /// </summary>
        public event EventHandler LeftClick;

        /// <summary>
        /// Raised when the left mouse button is clicked twice in a short
        /// time-frame while the cursor is inside the control's area.
        /// </summary>
        public event EventHandler DoubleLeftClick;

        /// <summary>
        /// Raised when the right mouse button is clicked (pressed and released)
        /// while the cursor is inside the control's area.
        /// </summary>
        public event EventHandler RightClick;

        /// <summary>
        /// Raised when the control's client rectangle is changed.
        /// </summary>
        public event EventHandler ClientRectangleUpdated;

        /// <summary>
        /// Raised when the control is selected or un-selected.
        /// </summary>
        public event EventHandler SelectedChanged;


        /// <summary>
        /// The non-scaled display rectangle of the control inside its parent.
        /// </summary>
        [Obsolete]
        public Rectangle ClientRectangle
        {
            get
            {
                return new Rectangle(_x, _y, _width, _height);
            }

            set
            {
                _x = value.X;
                _y = value.Y;
                bool isSizeChanged = value.Width != _width || value.Height != _height;
                if (isSizeChanged)
                {
                    _width = value.Width;
                    _height = value.Height;
                    OnSizeChanged();
                }

                OnClientRectangleUpdated();
            }
        }

        public void SetSize(int width, int height)
        {
            bool isSizeChanged = width != _width || height != _height;
            if (isSizeChanged)
            {
                _width = width;
                _height = height;
                OnSizeChanged();
            }
        }

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
            OnClientRectangleUpdated();
        }

        public bool SkipParseTextFromStringManager { get; set; }

        public bool SkipParseFromStringManager { get; set; }

        /// <summary>
        /// Called when the control's size is changed.
        /// </summary>
        protected virtual void OnSizeChanged()
        {
            if (!IsChangingSize && IsInitialized && DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
            {
                RefreshRenderTarget();
            }

            OnClientRectangleUpdated();
        }
        // Event Relay Method
        private void OnSizeChanged(int i) => OnSizeChanged();

        /// <summary>
        /// Called when the control's client rectangle is changed.
        /// </summary>
        protected virtual void OnClientRectangleUpdated()
        {
            ClientRectangleUpdated?.Invoke(this, EventArgs.Empty);
        }
        // Event Relay Method
        private void OnClientRectangleUpdated(int i) => OnClientRectangleUpdated();

        private void CheckForRenderAreaChange()
        {
            if (DrawMode != ControlDrawMode.UNIQUE_RENDER_TARGET)
                return;

            if (RenderTarget == null || RenderTarget.Width != Width || RenderTarget.Height != Height)
            {
                RefreshRenderTarget();
            }

            _children.ForEach(c => c.CheckForRenderAreaChange());
        }



        /// <summary>
        /// Gets the window manager associated with this control.
        /// </summary>
        public WindowManager WindowManager { get; private set; }

        /// <summary>
        /// Gets or sets the name of this control. The name is only an identifier
        /// and does not affect functionality.
        /// </summary>
        public string Name { get; set; }

        public XNAControl RootParent
        {
            get
            {
                if (Parent == null)
                    return null;

                XNAControl rootParent = Parent;

                while (rootParent.Parent != null)
                {
                    rootParent = rootParent.Parent;
                }

                return rootParent;
            }
        }

        /// <summary>
        /// A read-only list of the control's children.
        /// Call the AddChild method to add children to the control.
        /// </summary>
        public ReadOnlyCollection<XNAControl> Children => new ReadOnlyCollection<XNAControl>(_children);

        /// <summary>
        /// Set if the control is detached from its parent.
        /// A detached control's mouse input is handled independently
        /// from its parent, ie. it can grow beyond its parent's area
        /// rectangle and still handle input correctly.
        /// </summary>
        public bool Detached { get; private set; } = false;

        public Color RemapColor { get; set; } = Color.White;

        public virtual float Alpha
        {
            get
            {
                return alpha;
            }

            set
            {
                if (value > 1.0f)
                    alpha = 1.0f;
                else if (value < 0.0)
                    alpha = 0.0f;
                else
                    alpha = value;
            }
        }

        public int CursorTextureIndex { get; set; }

        public object Tag { get; set; }

        public bool Killed { get; set; }

        /// <summary>
        /// Determines whether the control should block other controls on the screen
        /// from being interacted with.
        /// </summary>
        public bool Focused { get; set; }

        /// <summary>
        /// Determines whether this control is able to handle input.
        /// If set to false, input management will ignore this control.
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a bool that determines whether this control is the current focus of the mouse cursor.
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (Parent != null && !Detached)
                    return Parent.IsActive && isActive;

                return isActive;
            }

            set
            {
                isActive = value;
            }
        }

        public bool IgnoreInputOnFrame
        {
            get
            {
                if (Parent == null)
                    return _ignoreInputOnFrame;
                else
                    return _ignoreInputOnFrame || Parent.IgnoreInputOnFrame;
            }

            set
            {
                _ignoreInputOnFrame = true;
            }
        }

        /// <summary>
        /// The draw mode of the control.
        /// Cannot be changed after the control's <see cref="Initialize"/>
        /// method has been run.
        /// </summary>
        public ControlDrawMode DrawMode
        {
            get
            {
                return drawMode;
            }

            set
            {
                if (IsInitialized)
                {
                    throw new InvalidOperationException("DrawMode cannot be " +
                        "changed after a control has been initialized.");
                }

                drawMode = value;
            }
        }

        /// <summary>
        /// If set to true and the control has
        /// <see cref="DrawMode"/> == <see cref="ControlDrawMode.UNIQUE_RENDER_TARGET"/>,
        /// the control won't try to update its render target when its size is changed.
        /// </summary>
        public bool IsChangingSize
        {
            get => _isChangingSize || (Parent != null && Parent.IsChangingSize);

            set
            {
                _isChangingSize = value;
                if (!_isChangingSize)
                    CheckForRenderAreaChange();
            }
        }

        public int Scaling
        {
            get => _scaling;

            set
            {
                if (DrawMode != ControlDrawMode.UNIQUE_RENDER_TARGET)
                {
                    throw new InvalidOperationException("Scaling cannot be " +
                        "used when the control has no unique render target.");
                }

                if (IsInitialized && value < _initScaling)
                {
                    throw new InvalidOperationException("Scaling cannot be " +
                        "lowered below the initial scaling multiplier after control initialization.");
                }

                if (value < 1)
                {
                    throw new InvalidOperationException("Scale factor cannot be below one.");
                }

                _scaling = value;
            }
        }

        /// <summary>
        /// Whether this control should allow input to pass through to controls
        /// that come after this in the control hierarchy when the control
        /// itself is the focus of input, but none of its children are.
        /// Useful for controls that act as composite for other controls.
        /// </summary>
        public bool InputPassthrough { get; protected set; } = false;

        private Point _drawPoint;

        /// <summary>
        /// Draws a texture relative to the control's location.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="rectangle">The rectangle where to draw the texture
        /// relative to the control.</param>
        /// <param name="color">The remap color.</param>
        protected void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
        {
            Renderer.DrawTexture(texture, new Rectangle(_drawPoint.X + rectangle.X,
                _drawPoint.Y + rectangle.Y, rectangle.Width, rectangle.Height), color);
        }

        /// <summary>
        /// Draws a texture relative to the control's location.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="point">The point where to draw the texture
        /// relative to the control.</param>
        /// <param name="color">The remap color.</param>
        protected void DrawTexture(Texture2D texture, Point point, Color color) =>
            Renderer.DrawTexture(texture, new Rectangle(_drawPoint.X + point.X, _drawPoint.Y + point.Y, texture.Width, texture.Height), color);

        /// <summary>
        /// Draws a texture relative to the control's location
        /// within the used render target.
        /// </summary>
        protected void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
        {
            Rectangle destRect = new Rectangle(_drawPoint.X + destinationRectangle.X,
                _drawPoint.Y + destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height);

            Renderer.DrawTexture(texture, sourceRectangle, destRect, color);
        }

        /// <summary>
        /// Draws a texture relative to the control's location.
        /// </summary>
        protected void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
            Renderer.DrawTexture(texture,
                new Vector2(location.X + _drawPoint.X, location.Y + _drawPoint.Y),
                rotation, origin, scale, color);
        }

        /// <summary>
        /// Draws a string relative to the control's location.
        /// </summary>
        protected void DrawString(string text, SpriteFontBase font, Vector2 location, Color color, float scale = 1)
        {
            Renderer.DrawString(text, font,
                new Vector2(location.X + _drawPoint.X, location.Y + _drawPoint.Y), color, scale);
        }

        /// <summary>
        /// Draws a string with a shadow, relative to the control's location.
        /// </summary>
        /// <param name="text">The string to draw.</param>
        /// <param name="font">The font to use for drawing the string.</param>
        /// <param name="location">The location of the text to draw, relative to the control's location.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="shadowDistance">How many distance units (typically pixels) the text shadow is offset from the text.</param>
        public void DrawStringWithShadow(string text, SpriteFontBase font, Vector2 location, Color color, float scale = 1.0f, float shadowDistance = 1.0f)
        {
            Renderer.DrawStringWithShadow(text, font,
                new Vector2(location.X + _drawPoint.X, location.Y + _drawPoint.Y), color, scale, shadowDistance);
        }

        /// <summary>
        /// Draws a rectangle's borders relative to the control's location
        /// with the given color and thickness.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="color">The color.</param>
        /// <param name="thickness">The thickness of the rectangle's borders.</param>
        protected void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
        {
            Renderer.DrawRectangle(new Rectangle(rect.X + _drawPoint.X,
                rect.Y + _drawPoint.Y, rect.Width, rect.Height), color, thickness);
        }

        /// <summary>
        /// Fills the control's drawing area with the given color.
        /// </summary>
        /// <param name="color">The color to fill the area with.</param>
        protected void FillControlArea(Color color)
        {
            FillRectangle(new Rectangle(0, 0, Width, Height), color);
        }

        /// <summary>
        /// Fills a rectangle relative to the control's location with the given color.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="color">The color to fill the rectangle with.</param>
        protected void FillRectangle(Rectangle rect, Color color)
        {
            Renderer.FillRectangle(new Rectangle(rect.X + _drawPoint.X,
                rect.Y + _drawPoint.Y, rect.Width, rect.Height), color);
        }

        /// <summary>
        /// Draws a line relative to the control's location.
        /// </summary>
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="thickness">The thickness of the line.</param>
        protected void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Renderer.DrawLine(new Vector2(start.X + _drawPoint.X, start.Y + _drawPoint.Y),
                new Vector2(end.X + _drawPoint.X, end.Y + _drawPoint.Y), color, thickness);
        }

        /// <summary>
        /// Called when the mouse cursor enters the control's client rectangle.
        /// </summary>
        protected virtual void OnMouseEnter() => MouseEnter?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the mouse cursor leaves the control's client rectangle.
        /// </summary>
        protected virtual void OnMouseLeave() => MouseLeave?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called once when the left mouse button is pressed down while the cursor
        /// is on the control.
        /// </summary>
        protected virtual void OnMouseLeftDown() => MouseLeftDown?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called once when the right mouse button is pressed down while the cursor
        /// is on the control.
        /// </summary>
        protected virtual void OnMouseRightDown() => MouseRightDown?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the left mouse button has been
        /// clicked on the control's client rectangle.
        /// </summary>
        protected virtual void OnLeftClick()
        {
            WindowManager.SelectedControl = this;

            LeftClick?.Invoke(this, EventArgs.Empty);

            if (timeSinceLastLeftClick < TimeSpan.FromSeconds(DOUBLE_CLICK_TIME))
            {
                OnDoubleLeftClick();
                return;
            }

            timeSinceLastLeftClick = TimeSpan.Zero;
        }

        /// <summary>
        /// Called when the left mouse button has been
        /// clicked twice on the control's client rectangle.
        /// </summary>
        protected virtual void OnDoubleLeftClick() => DoubleLeftClick?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the right mouse button has been
        /// clicked on the control's client rectangle.
        /// </summary>
        protected virtual void OnRightClick() => RightClick?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the mouse moves on the control's client rectangle.
        /// </summary>
        protected virtual void OnMouseMove() => MouseMove?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called on each frame while the mouse is on the control's
        /// client rectangle.
        /// </summary>
        /// <param name="eventArgs">Mouse event arguments.</param>
        protected virtual void OnMouseOnControl() => MouseOnControl?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the scroll wheel has been scrolled on the
        /// control's client rectangle.
        /// </summary>
        protected virtual void OnMouseScrolled() => MouseScrolled?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the control's status as the selected (last-clicked)
        /// control has been changed.
        /// </summary>
        public virtual void OnSelectedChanged() => SelectedChanged?.Invoke(this, EventArgs.Empty);

        private void DrawInternal_UniqueRenderTarget(GameTime gameTime)
        {
            if (RenderTarget == null)
                RefreshRenderTarget();

            _drawPoint = Point.Zero;
            RenderTargetStack.PushRenderTarget(RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            Draw(gameTime);
            RenderTargetStack.PopRenderTarget();
            Rectangle rect = RenderRectangle();
            if (Scaling > 1 && Renderer.CurrentSettings.SamplerState != SamplerState.PointClamp)
            {
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp));
                DrawUniqueRenderTarget(rect);
                Renderer.PopSettings();
            }
            else
            {
                DrawUniqueRenderTarget(rect);
            }
        }

        /// <summary>
        /// Draws the control when it is detached from its parent.
        /// </summary>
        private void DrawInternal_Detached(GameTime gameTime)
        {
            int totalScaling = GetTotalScalingRecursive();
            if (totalScaling > 1)
            {
                // We have to use an unique render target for scaling
                RenderTargetStack.PushRenderTarget(RenderTargetStack.DetachedScaledControlRenderTarget);
                Draw(gameTime);
                RenderTargetStack.PopRenderTarget();
                Rectangle renderRectangle = RenderRectangle();
                if (Renderer.CurrentSettings.SamplerState != SamplerState.PointClamp)
                {
                    Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp));
                    DrawDetachedScaledTexture(renderRectangle, totalScaling);
                    Renderer.PopSettings();
                }
                else
                {
                    DrawDetachedScaledTexture(renderRectangle, totalScaling);
                }

                return;
            }

            Draw(gameTime);
        }

        private void DrawUniqueRenderTarget(Rectangle renderRectangle)
        {
            Renderer.DrawTexture(RenderTarget, new Rectangle(0, 0, Width, Height),
                new Rectangle(renderRectangle.X, renderRectangle.Y, ScaledWidth, ScaledHeight), Color.White * Alpha);
        }

        private void DrawDetachedScaledTexture(Rectangle renderRectangle, int totalScaling)
        {
            Renderer.DrawTexture(RenderTargetStack.DetachedScaledControlRenderTarget,
            renderRectangle,
            new Rectangle(renderRectangle.X, renderRectangle.Y, Width * totalScaling, Height * totalScaling), Color.White * Alpha);
        }

        private TimeSpan timeSinceLastLeftClick = TimeSpan.Zero;
        private bool isLeftPressedOn = false;
        private bool isRightPressedOn = false;

        private bool isIteratingChildren = false;

        /// <summary>
        /// Whether a child of this control handled input during the ongoing frame.
        /// Used for input pass-through.
        /// </summary>
        internal bool ChildHandledInput = false;

        /// <summary>
        /// The render target of the control
        /// in unique render target mode.
        /// </summary>
        protected RenderTarget2D RenderTarget { get; set; }

        /// <summary>
        /// Determines whether the control's <see cref="Initialize"/> method
        /// has been called yet.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Checks if the last parent of this control is active.
        /// </summary>
        public bool IsLastParentActive()
        {
            if (Parent != null)
                return Parent.IsLastParentActive();

            return isActive;
        }

        /// <summary>
        /// Checks whether a condition applies to this control and all of its parents.
        /// </summary>
        /// <param name="func">The condition.</param>
        public bool AppliesToSelfAndAllParents(Func<XNAControl, bool> func)
        {
            return func(this) && (Parent == null || Parent.AppliesToSelfAndAllParents(func));
        }

        /// <summary>
        /// Gets the cursor's location relative to this control's location.
        /// </summary>
        /// <returns>A point that represents the cursor's location relative to this control's location.</returns>
        public Point GetCursorPoint()
        {
            Point windowPoint = GetWindowPoint();
            int totalScaling = GetTotalScalingRecursive();
            return new Point((Cursor.Location.X - windowPoint.X) / totalScaling, (Cursor.Location.Y - windowPoint.Y) / totalScaling);
        }

        /// <summary>
        /// Gets the location of the control's top-left corner within the game window.
        /// Use for input handling; for rendering, use <see cref="GetRenderPoint"/> instead.
        /// </summary>
        public Point GetWindowPoint()
        {
            Point p = new Point(X, Y);

            if (Parent != null)
            {
                int parentTotalScaling = Parent.GetTotalScalingRecursive();
                p = new Point(p.X * parentTotalScaling, p.Y * parentTotalScaling);

                return PlatformUtils.SumPoints(p, Parent.GetWindowPoint());
            }

            return p;
        }

        public Point GetSizePoint()
        {
            int totalScaling = GetTotalScalingRecursive();
            return new Point(Width * totalScaling, Height * totalScaling);
        }

        public int GetTotalScalingRecursive()
        {
            if (Parent != null)
                return Scaling * Parent.GetTotalScalingRecursive();

            return Scaling;
        }

        /// <summary>
        /// Gets the control's client area within the game window.
        /// Use for input handling; for rendering, use <see cref="RenderRectangle"/> instead.
        /// </summary>
        public Rectangle GetWindowRectangle()
        {
            Point p = GetWindowPoint();
            Point size = GetSizePoint();
            return new Rectangle(p.X, p.Y, size.X, size.Y);
        }

        /// <summary>
        /// Returns the draw area of the control relative to the used render target.
        /// </summary>
        public Rectangle RenderRectangle()
        {
            Point p = GetRenderPoint();
            return new Rectangle(p.X, p.Y, Width, Height);
        }

        /// <summary>
        /// Gets the location of the control's top-left corner within the current render target.
        /// </summary>
        public Point GetRenderPoint()
        {
            Point p = new Point(X, Y);

            if (Parent != null)
            {
                if (Detached)
                    return GetWindowPoint();

                if (Parent.DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
                    return p;

                return PlatformUtils.SumPoints(p, Parent.GetRenderPoint());
            }

            return p;
        }

        /// <summary>
        /// Centers the control on the middle of its parent's client rectangle.
        /// </summary>
        public void CenterOnParent()
        {
            if (Parent == null)
            {
                WindowManager.CenterControlOnScreen(this);
                return;
            }

            this.SetClientRectangle((Parent.Width - ScaledWidth) / 2,
                (Parent.Height - ScaledHeight) / 2, Width, Height);
        }

        /// <summary>
        /// Centers the control horizontally on the middle of its parent's client rectangle.
        /// </summary>
        public void CenterOnParentHorizontally()
        {
            if (Parent == null)
            {
                // TODO WindowManager.CenterControlOnScreenHorizontally();
                return;
            }

            this.SetClientRectangle((Parent.Width - ScaledWidth) / 2,
                Y, Width, Height);
        }

        /// <summary>
        /// Centers the control vertically in proportion to another control.
        /// Assumes that this control and the other control share the same parent control.
        /// </summary>
        /// <param name="control">The other control.</param>
        public void CenterOnControlVertically(XNAControl control)
        {
            Y = control.Y - (Height - control.Height) / 2;
        }

        /// <summary>
        /// Detaches the control from its parent.
        /// See <see cref="Detached"/>.
        /// </summary>
        public void Detach()
        {
            if (Detached)
                throw new InvalidOperationException("The control is already detached!");

            Detached = true;
            WindowManager.AddControl(this);
        }

        /// <summary>
        /// Attaches the control to its parent.
        /// </summary>
        public void Attach()
        {
            Detached = false;
            WindowManager.RemoveControl(this);
        }

        private readonly object locker = new object();

        private List<Callback> Callbacks = new List<Callback>();

        /// <summary>
        /// Schedules a delegate to be executed on the next game loop frame,
        /// on the main game thread.
        /// </summary>
        /// <param name="d">The delegate.</param>
        /// <param name="args">The arguments to be passed on to the delegate.</param>
        public void AddCallback(Delegate d, params object[] args)
        {
            lock (locker)
                Callbacks.Add(new Callback(d, args));
        }

        /// <summary>
        /// Adds a child to the control.
        /// In case the control is currently being updated, schedules the child
        /// to be added at the end of the current frame.
        /// </summary>
        /// <param name="child">The child control.</param>
        public virtual void AddChild(XNAControl child)
        {
            if (child == null)
                throw new ArgumentNullException("child");

            if (isIteratingChildren)
                _childAddQueue.Add(child);
            else
                AddChildImmediate(child);
        }

        /// <summary>
        /// Adds a child control to the control without calling the child's Initialize method.
        /// In case the control is currently being updated, schedules the child
        /// to be added at the end of the current frame.
        /// </summary>
        /// <param name="child">The child control.</param>
        public void AddChildWithoutInitialize(XNAControl child)
        {
            if (child == null)
                throw new ArgumentNullException("child");

            if (isIteratingChildren)
            {
                throw new NotImplementedException("AddChildWithoutInitialize cannot currently be called" +
                    " while the control is iterating through its children.");
            }
            else
                AddChildImmediateWithoutInitialize(child);
        }

        /// <summary>
        /// Immediately adds a child control to the control.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildImmediate(XNAControl child)
        {
            InitChild(child);

            //Debug.Assert(!child.IsInitialized);
            if (!child.IsInitialized)
                child.Initialize();
            _children.Add(child);
            ReorderControls();
        }

        /// <summary>
        /// Immediately adds a child control to the control without calling the child's Initialize method.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildImmediateWithoutInitialize(XNAControl child)
        {
            InitChild(child);
            _children.Add(child);
            ReorderControls();
        }

        /// <summary>
        /// Adds a child control to the control, making the added child
        /// the "first child" of this control.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildToFirstIndexImmediate(XNAControl child)
        {
            InitChild(child);
            child.Initialize();
            _children.Insert(0, child);
            ReorderControls();
        }

        private void InitChild(XNAControl child)
        {
            if (child.Parent != null)
                throw new InvalidOperationException("Child controls cannot be shared between controls. Child control name: " + child.Name);

            child.Parent = this;
            child.UpdateOrderChanged += Child_UpdateOrderChanged;
            child.DrawOrderChanged += Child_DrawOrderChanged;
        }

        private void Child_DrawOrderChanged(object sender, EventArgs e)
        {
            _drawList = _children.OrderBy(c => c.DrawOrder).ToList();
        }

        private void Child_UpdateOrderChanged(object sender, EventArgs e)
        {
            _updateList = _children.OrderBy(c => c.UpdateOrder).Reverse().ToList();
        }

        /// <summary>
        /// Removes a child from the control.
        /// </summary>
        /// <param name="child">The child control to remove.</param>
        public void RemoveChild(XNAControl child)
        {
            if (isIteratingChildren)
                _childRemoveQueue.Add(child);
            else
                RemoveChildImmediate(child);
        }

        /// <summary>
        /// Immediately removes a child from the control.
        /// </summary>
        /// <param name="child">The child control to remove.</param>
        private void RemoveChildImmediate(XNAControl child)
        {
            if (_children.Remove(child))
            {
                child.UpdateOrderChanged -= Child_UpdateOrderChanged;
                child.DrawOrderChanged -= Child_DrawOrderChanged;
                child.Parent = null;
                ReorderControls();
            }
        }

        private void ReorderControls()
        {
            // Controls that are updated first should be drawn last
            // (on top of the other controls).
            // It's weird for the updateorder and draworder to behave differently,
            // but at this point we don't have a choice because of backwards compatibility.
            _updateList = _children.OrderBy(c => c.UpdateOrder).Reverse().ToList();
            _drawList = _children.OrderBy(c => c.DrawOrder).ToList();
        }

        /// <summary>
        /// Initializes the control.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            foreach (var child in Children)
            {
                //Debug.Assert(!child.IsInitialized);
                if (!child.IsInitialized)
                    child.Initialize();
            }

            Initialized?.Invoke(this, EventArgs.Empty);

            IsInitialized = true;
            _initScaling = _scaling;
        }

        public event EventHandler Initialized;

        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            if (Visible)
            {
                if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET && RenderTarget == null)
                    RenderTarget = GetRenderTarget();
            }
            else
            {
                if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET && RenderTarget != null && FreeRenderTarget())
                    RenderTarget = null;
            }

            base.OnVisibleChanged(sender, args);
        }

        /// <summary>
        /// Called for a control with an unique render target when its Visible= is set to false.
        /// Can be used to free up the render target in derived classes.
        /// Returns true if the render target should be cleared after this call, false otherwise.
        /// </summary>
        protected virtual bool FreeRenderTarget()
        {
            return false;
        }

        private void RefreshRenderTarget()
        {
            if (RenderTarget != null)
            {
                if (!FreeRenderTarget())
                {
                    RenderTarget.Dispose();
                }
            }

            RenderTarget = GetRenderTarget();
            if (RenderTarget == null)
                throw new InvalidOperationException("GetRenderTarget did not return a render target.");
        }

        protected virtual RenderTarget2D GetRenderTarget()
        {
            return new RenderTarget2D(GraphicsDevice,
                GetRenderTargetWidth(), GetRenderTargetHeight(), false,
                SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        protected virtual int GetRenderTargetWidth() => Width <= 0 ? 2 : Width;

        protected virtual int GetRenderTargetHeight() => Height <= 0 ? 2 : Height;

        [Obsolete]
        public virtual void GetAttributes(IniFile iniFile)
        {
            IsChangingSize = true;

            foreach (XNAControl child in Children)
                child.GetAttributes(iniFile);

            List<string> keys = iniFile.GetSectionKeys(Name);

            if (keys != null)
            {
                foreach (string key in keys)
                    ParseAttributeFromINI(iniFile, key, iniFile.GetStringValue(Name, key, String.Empty));
            }

            GetAttributes();
            IsChangingSize = false;
        }

        public virtual void GetAttributes()
        {
            var properties = GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Where(i => i.CanWrite).ToArray();
            foreach (var property in properties)
                ParseAttributeFromUIConfigurations(property.Name, property.PropertyType);

            foreach (var property in VirtualProperties)
                ParseAttributeFromUIConfigurations(property.Key, property.Value);

            if (Children.Any())
            {
                foreach (var child in Children)
                {
                    child.GetAttributes();
                }
            }

            if (!string.IsNullOrEmpty(Name))
                ParseLocaleStringsFromStringManager();
        }

        protected Dictionary<string, Type> VirtualProperties { get; } = new Dictionary<string, Type>
        {
            { "Location", typeof(Point) },
            { "Size", typeof(Point) }
        };

        [Obsolete]
        public virtual void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "DrawOrder":
                    DrawOrder = Int32.Parse(value);
                    return;

                case "UpdateOrder":
                    UpdateOrder = Int32.Parse(value);
                    return;

                case "Size":
                    string[] size = value.Split(',');
                    this.SetClientRectangle(X, Y,
                        int.Parse(size[0]), int.Parse(size[1]));
                    return;

                case "Width":
                    Width = int.Parse(value);
                    return;

                case "Height":
                    Height = int.Parse(value);
                    return;

                case "Location":
                    string[] location = value.Split(',');
                    this.SetClientRectangle(int.Parse(location[0]), int.Parse(location[1]),
                        Width, Height);
                    return;

                case "X":
                    X = int.Parse(value);
                    return;

                case "Y":
                    Y = int.Parse(value);
                    return;

                case "RemapColor":
                    string[] colors = value.Split(',');
                    RemapColor = AssetLoader.GetColorFromString(value);
                    return;

                case "Text":
                    Text = value.Replace("@", Environment.NewLine);
                    return;

                case "Visible":
                    Visible = Conversions.BooleanFromString(value, true);
                    Enabled = Visible;
                    return;

                case "Enabled":
                    Enabled = Conversions.BooleanFromString(value, true);
                    return;

                case "DistanceFromRightBorder":
                    if (Parent != null)
                    {
                        this.SetClientRectangle(Parent.Width - Width - Conversions.IntFromString(value, 0), Y,
                            Width, Height);
                    }
                    return;

                case "DistanceFromBottomBorder":
                    if (Parent != null)
                    {
                        this.SetClientRectangle(X, Parent.Height - Height - Conversions.IntFromString(value, 0),
                            Width, Height);
                    }
                    return;

                case "FillWidth":
                    if (Parent != null)
                    {
                        this.SetClientRectangle(X, Y,
                            Parent.Width - X - Conversions.IntFromString(value, 0), Height);
                    }
                    else
                    {
                        this.SetClientRectangle(X, Y,
                            WindowManager.RenderResolutionX - X - Conversions.IntFromString(value, 0),
                            Height);
                    }
                    return;

                case "FillHeight":
                    if (Parent != null)
                    {
                        this.SetClientRectangle(X, Y,
                            Width, Parent.Height - Y - Conversions.IntFromString(value, 0));
                    }
                    else
                    {
                        this.SetClientRectangle(X, Y,
                            Width, WindowManager.RenderResolutionY - Y - Conversions.IntFromString(value, 0));
                    }
                    return;
            }
        }

        /// <summary>
        /// Disables and hides the control.
        /// </summary>
        public void Disable()
        {
            Enabled = false;
            Visible = false;
        }

        /// <summary>
        /// Enables and shows the control.
        /// </summary>
        public void Enable()
        {
            Enabled = true;
            Visible = true;
        }

        /// <summary>
        /// Destroys the control and all child controls to free up resources.
        /// </summary>
        public virtual void Kill()
        {
            foreach (XNAControl child in Children)
                child.Kill();

            Killed = true;
        }

        public virtual void RefreshSize()
        {
            foreach (XNAControl child in Children)
                child.RefreshSize();
        }

        /// <summary>
        /// Updates the control's logic and handles input.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            Rectangle rectangle = GetWindowRectangle();

            timeSinceLastLeftClick += gameTime.ElapsedGameTime;

            int callbackCount = Callbacks.Count;

            if (callbackCount > 0)
            {
                lock (locker)
                {
                    for (int i = 0; i < callbackCount; i++)
                        Callbacks[i].Invoke();

                    // Do not clear the list; another thread could theoretically add an
                    // item after we get the callback count, but before we lock
                    Callbacks.RemoveRange(0, callbackCount);
                }
            }

            if (IgnoreInputOnFrame)
            {
                _ignoreInputOnFrame = false;
                return;
            }

            XNAControl activeChild = null;

            if (Cursor.IsOnScreen && IsActive && rectangle.Contains(Cursor.Location))
            {
                if (!CursorOnControl)
                {
                    CursorOnControl = true;
                    OnMouseEnter();
                }

                isIteratingChildren = true;

                var activeChildEnumerator = _updateList.GetEnumerator();

                while (activeChildEnumerator.MoveNext())
                {
                    XNAControl child = activeChildEnumerator.Current;

                    if (child.Visible && !child.Detached && (child.Focused || (child.InputEnabled &&
                        child.GetWindowRectangle().Contains(Cursor.Location) && activeChild == null)))
                    {
                        child.IsActive = true;
                        activeChild = child;
                        WindowManager.SetActiveControl(child);
                        break;
                    }
                }

                isIteratingChildren = false;

                Cursor.TextureIndex = CursorTextureIndex;

                OnMouseOnControl();

                if (Cursor.HasMoved)
                    OnMouseMove();

                bool handleClick = activeChild == null;

                if (!isLeftPressedOn && Cursor.LeftPressedDown)
                {
                    isLeftPressedOn = true;
                    OnMouseLeftDown();
                }
                else if (isLeftPressedOn && Cursor.LeftClicked)
                {
                    if (handleClick)
                        OnLeftClick();

                    isLeftPressedOn = false;
                }

                if (!isRightPressedOn && Cursor.RightPressedDown)
                {
                    isRightPressedOn = true;
                    OnMouseRightDown();
                }
                else if (isRightPressedOn && Cursor.RightClicked)
                {
                    if (handleClick)
                        OnRightClick();

                    isRightPressedOn = false;
                }

                if (Cursor.ScrollWheelValue != 0)
                {
                    OnMouseScrolled();
                }
            }
            else if (CursorOnControl)
            {
                OnMouseLeave();

                CursorOnControl = false;
                isRightPressedOn = false;
            }
            else
            {
                if (isLeftPressedOn && Cursor.LeftClicked)
                    isLeftPressedOn = false;

                if (isRightPressedOn && Cursor.RightClicked)
                    isRightPressedOn = false;
            }

            isIteratingChildren = true;

            var enumerator = _updateList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var child = enumerator.Current;

                if (child != activeChild && !child.Detached)
                    child.IsActive = false;

                if (child.Enabled)
                {
                    child.Update(gameTime);
                }
            }

            isIteratingChildren = false;

            foreach (var child in _childAddQueue)
                AddChildImmediate(child);

            _childAddQueue.Clear();

            foreach (var child in _childRemoveQueue)
                RemoveChildImmediate(child);

            _childRemoveQueue.Clear();

            ChildHandledInput = activeChild != null;
        }

        /// <summary>
        /// Draws the control.
        /// DO NOT call manually unless you know what you're doing.
        /// </summary>
        internal void DrawInternal(GameTime gameTime)
        {
            if (!Visible)
                return;

            if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
            {
                DrawInternal_UniqueRenderTarget(gameTime);
            }
            else
            {
                _drawPoint = GetRenderPoint();

                if (Detached)
                {
                    DrawInternal_Detached(gameTime);
                }
                else
                {
                    Draw(gameTime);
                }
            }
        }

        /// <summary>
        /// Draws the control and its child controls.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            DrawChildren(gameTime);
        }

        /// <summary>
        /// Draws the control's child controls.
        /// </summary>
        protected void DrawChildren(GameTime gameTime)
        {
            var enumerator = _drawList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (current.Visible && !current.Detached)
                    current.DrawInternal(gameTime);
            }
        }

        protected virtual void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case nameof(Visible):
                    if (this.TryGet(property, out bool b))
                    {
                        Visible = b;
                        Enabled = b;
                    }
                    return;

                case nameof(DrawOrder):
                    if (this.TryGet(property, out int i))
                        DrawOrder = i;
                    return;

                case nameof(Enabled):
                    if (this.TryGet(property, out b))
                        Enabled = b;
                    return;

                case nameof(UpdateOrder):
                    if (this.TryGet(property, out i))
                        UpdateOrder = i;
                    return;

                case nameof(FontSize):
                    if (this.TryGet(property, out i))
                        FontSize = i;
                    return;

                case nameof(X):
                    if (this.TryGet(property, out i))
                        X = i;
                    return;

                case nameof(Y):
                    if (this.TryGet(property, out i))
                        Y = i;
                    return;

                case nameof(Width):
                    if (this.TryGet(property, out i))
                        Width = i;
                    return;

                case nameof(Height):
                    if (this.TryGet(property, out i))
                        Height = i;
                    return;

                case "Position":
                case "Location":
                    if (this.TryGet(property, out Point point))
                        this.SetPosition(ref point);
                    return;

                case "Size":
                    if (this.TryGet(property, out point))
                        this.SetSize(ref point);
                    return;
            }
        }

        protected string GetUIString(string property, string prefix = "UI")
            => StringManager.GetString(string.Join(".", new[] { prefix, Name, property }));

        protected string GetUIStringEx(string property, string prefix = "UI")
        {
            string origin = string.Join(".", new[] { prefix, Name, property });
            string value = GetUIString(property, prefix);
            if (value != origin)
                return value;

            string key = origin;

            Type type = GetType();

            while (value == key)
            {
                key = string.Join(".", new[] { prefix, type.Name, property });
                value = StringManager.GetString(key);
                if (type == typeof(XNAControl))
                    break;

                type = type.BaseType;
            }
            return value != key ? value : origin;
        }

        protected static string Format(string format, params string[] args) => args != null ? string.Format(format, args) : format;

        protected virtual void ParseLocaleStringsFromStringManager()
        {
            if (SkipParseFromStringManager)
                return;
            if (!SkipParseTextFromStringManager)
                Text = GetUIString(nameof(Text));
            Font = GetUIStringEx(nameof(Font));

            if (int.TryParse(GetUIStringEx(nameof(FontSize)), out var i))
                FontSize = i;
        }


        public virtual SpriteFontBase GetFont()
        {
            var key = $"{Font}:{FontSize}";
            if (key != _fontCache.Key)
                _fontCache = new KeyValuePair<string, SpriteFontBase>(key, GetFont(Font, FontSize));

            return _fontCache.Value;
        }

        public static SpriteFontBase GetFont(string font, int size) => AssetLoader.FontManager[font].GetFont(size);

        private string GetDebuggerDisplay() => $"{Name} : {GetType().Name}";
    }
}
