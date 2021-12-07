using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNATrackbar : XNAPanel
    {
        public XNATrackbar(WindowManager windowManager) : base(windowManager)
        {

        }

        public event EventHandler ValueChanged;

        public int MinValue { get; set; } = 0;

        public int MaxValue { get; set; } = 10;

        int value = 0;
        public int Value
        {
            get { return value; }
            set
            {
                int oldValue = this.value;

                if (value > MaxValue)
                    this.value = MaxValue;
                else if (value < MinValue)
                    this.value = MinValue;
                else
                    this.value = value;

                if (oldValue != this.value)
                    ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public EnhancedSoundEffect ClickSound { get; set; }

        public Texture2D ButtonTexture { get; set; }

        private bool isHeldDown = false;

        public override void Initialize()
        {
            base.Initialize();

            if (ButtonTexture == null)
            {
                Logger.Log($"WARN: {Name}.{nameof(ButtonTexture)} are not set!");
                ButtonTexture = AssetLoader.CreateTexture(new Color(255, 54, 244), 100, 100);
            }

            //if (Height == 0)
            //    Height = ButtonTexture.Height;
        }

        protected override void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case "MinValue":
                    if (this.TryGet(property, out int i))
                        MinValue = i;
                    return;
                case "MaxValue":
                    if (this.TryGet(property, out i))
                        MaxValue = i;
                    return;
                case "Value":
                    if (this.TryGet(property, out i))
                        Value = i;
                    return;
                case "ClickSound":
                    if (this.TryGet(property, out EnhancedSoundEffect ef))
                        ClickSound = ef;
                    return;
                case nameof(ButtonTexture):
                    if (this.TryGet(property, out Texture2D t))
                    {
                        ButtonTexture = t;
                        if (Height == 0)
                            Height = t.Height;
                    }
                    return;
            }
            base.ParseAttributeFromUIConfigurations(property, type);
        }

        public override void AddChild(XNAControl child)
        {
            base.AddChild(child);
        }

        /// <summary>
        /// Scrolls the scrollbar if the user presses the mouse left button
        /// while moving the cursor over the scrollbar.
        /// </summary>
        protected override void OnMouseOnControl()
        {
            base.OnMouseOnControl();

            if (Cursor.LeftPressedDown)
            {
                isHeldDown = true;
                // It's fair to assume that dragged trackbars are selected
                WindowManager.SelectedControl = this;
            }
        }

        protected override void OnLeftClick()
        {
            isHeldDown = true;
            Scroll();

            base.OnLeftClick();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (isHeldDown)
            {
                if (!Cursor.LeftDown)
                {
                    isHeldDown = false;
                    return;
                }

                Scroll();
            }
        }

        private void Scroll()
        {
            int xOffset = GetCursorPoint().X;

            int tabCount = MaxValue - MinValue + 1;

            double pixelsPerTab = Width / (double)tabCount;

            int currentTab = 0;

            for (int i = 0; i <= tabCount; i++)
            {
                if (i * pixelsPerTab < xOffset)
                {
                    currentTab = i;
                }
                else
                {
                    int newValue = currentTab + MinValue;

                    if (Value != newValue)
                        ClickSound?.Play();

                    Value = newValue;

                    return;
                }
            }

            if (Value != MaxValue)
                ClickSound?.Play();

            Value = MaxValue;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            int tabIndex = Value - MinValue;

            int tabCount = MaxValue - MinValue;

            double pixelsPerTab = (Width - ButtonTexture.Width) / (double)tabCount;

            double tabLocationX = tabIndex * pixelsPerTab;

            //if (tabIndex == 0)
            //    tabLocationX += ButtonTexture.Width / 2;
            //else if (tabIndex == tabCount)
            //    tabLocationX -= ButtonTexture.Width / 2;

            DrawTexture(ButtonTexture,
                new Rectangle((int)(tabLocationX), 0, ButtonTexture.Width, Height),
                Color.White);
        }
    }
}
