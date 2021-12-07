using FontStashSharp;

using Microsoft.Xna.Framework;

using Rampastring.Tools;

using System;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A static label control.
    /// </summary>
    public class XNALabel : XNAControl
    {
        public XNALabel(WindowManager windowManager) : base(windowManager)
        {
        }

        private Color? _textColor;

        public Color TextColor
        {
            get
            {
                return _textColor ?? UISettings.ActiveSettings.TextColor;
            }
            set { _textColor = value; }
        }


        private Vector2 _anchorPoint = Vector2.Zero;

        /// <summary>
        /// Determines the point that the text is placed around
        /// depending on TextAnchor.
        /// </summary>
        public Vector2 AnchorPoint
        {
            get => _anchorPoint;
            set
            {
                _anchorPoint = value;
                RefreshClientRectangle();
            }
        }

        /// <summary>
        /// Determines the position of the label's text relative to AnchorPoint.
        /// </summary>
        public LabelTextAnchorInfo TextAnchor { get; set; }

        protected override void OnTextChanged(string v)
        {
            base.OnTextChanged(v);
            RefreshClientRectangle();
        }

        private void RefreshClientRectangle()
        {
            if (!string.IsNullOrEmpty(base.Text))
            {
                Vector2 textSize = Renderer.GetTextDimensions(Text, GetFont());

                switch (TextAnchor)
                {
                    case LabelTextAnchorInfo.CENTER:
                        this.SetClientRectangle((int)(AnchorPoint.X - textSize.X / 2),
                            (int)(AnchorPoint.Y - textSize.Y / 2), (int)textSize.X, (int)textSize.Y);
                        break;
                    case LabelTextAnchorInfo.RIGHT:
                        this.SetClientRectangle((int)AnchorPoint.X, (int)AnchorPoint.Y, (int)textSize.X, (int)textSize.Y);
                        break;
                    case LabelTextAnchorInfo.LEFT:
                        this.SetClientRectangle((int)(AnchorPoint.X - textSize.X),
                            (int)AnchorPoint.Y, (int)textSize.X, (int)textSize.Y);
                        break;
                    case LabelTextAnchorInfo.NONE:
                        this.SetClientRectangle(X, Y, (int)textSize.X, (int)textSize.Y);
                        break;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void ParseLocaleStringsFromStringManager()
        {
            base.ParseLocaleStringsFromStringManager();
            RefreshClientRectangle();
        }

        protected override void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case nameof(TextColor):
                    if (this.TryGet(property, out Color c))
                        TextColor = c;
                    return;
                case nameof(AnchorPoint):
                    if (this.TryGet(property, out Point p))
                        AnchorPoint = new Vector2(p.X, p.Y);
                    return;
                case nameof(TextAnchor):
                    if (this.TryGet(property, out var s)
                        && Enum.TryParse(s, out LabelTextAnchorInfo info))
                        TextAnchor = info;
                    return;
            }
            base.ParseAttributeFromUIConfigurations(property, type);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawLabel();

            base.Draw(gameTime);
        }

        protected void DrawLabel()
        {
            if (!string.IsNullOrEmpty(Text))
                DrawStringWithShadow(Text, GetFont(), Vector2.Zero, TextColor);
        }
    }

    /// <summary>
    /// An enum for determining which part of a text is anchored to a specific point.
    /// </summary>
    public enum LabelTextAnchorInfo
    {
        NONE,
        LEFT,
        CENTER,
        RIGHT
    }
}
