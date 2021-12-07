﻿using FontStashSharp;

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


        public int TextShadowDistance { get; set; } = UISettings.ActiveSettings.TextShadowDistance;

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

                Width = (int)textSize.X;
                Height = (int)textSize.Y;

                if (TextAnchor != LabelTextAnchorInfo.NONE)
                {
                    X = (int)AnchorPoint.X;
                    Y = (int)AnchorPoint.Y;

                    if ((TextAnchor & LabelTextAnchorInfo.HORIZONTAL_CENTER) == LabelTextAnchorInfo.HORIZONTAL_CENTER)
                    {
                        X = (int)(AnchorPoint.X - textSize.X / 2);
                    }
                    else if ((TextAnchor & LabelTextAnchorInfo.RIGHT) == LabelTextAnchorInfo.RIGHT)
                    {
                        X = (int)AnchorPoint.X;
                    }
                    else if ((TextAnchor & LabelTextAnchorInfo.LEFT) == LabelTextAnchorInfo.LEFT)
                    {
                        X = (int)(AnchorPoint.X - textSize.X);
                    }

                    if ((TextAnchor & LabelTextAnchorInfo.VERTICAL_CENTER) == LabelTextAnchorInfo.VERTICAL_CENTER)
                    {
                        Y = (int)(AnchorPoint.Y - textSize.Y / 2);
                    }
                    else if ((TextAnchor & LabelTextAnchorInfo.TOP) == LabelTextAnchorInfo.TOP)
                    {
                        Y = (int)(AnchorPoint.Y - textSize.Y);
                    }
                    else if ((TextAnchor & LabelTextAnchorInfo.BOTTOM) == LabelTextAnchorInfo.BOTTOM)
                    {
                        Y = (int)AnchorPoint.Y;
                    }
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
                case "TextShadowDistance":
                    if (this.TryGet(property, out int i))
                        TextShadowDistance = i;
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
                DrawStringWithShadow(Text, GetFont(), Vector2.Zero, TextColor, 1.0f, TextShadowDistance);
        }
    }

    /// <summary>
    /// An enum for determining which part of a text is anchored to a specific point.
    /// </summary>
    public enum LabelTextAnchorInfo
    {
        NONE = 0,

        /// <summary>
        /// The text is anchored to be to the left of the given point.
        /// </summary>
        LEFT = 1,

        /// <summary>
        /// The text is anchored to be to the right of the given point.
        /// </summary>
        RIGHT = 2,

        /// <summary>
        /// The text is horizontally centered on the given point.
        /// </summary>
        HORIZONTAL_CENTER = 4,

        /// <summary>
        /// The text is anchored to be just above the given point.
        /// </summary>
        TOP = 8,

        /// <summary>
        /// The text is anchored to be just below the given point.
        /// </summary>
        BOTTOM = 16,

        /// <summary>
        /// The text is vertical centered on the given point.
        /// </summary>
        VERTICAL_CENTER = 32,

        /// <summary>
        /// The text is both horizontally and vertically centered on the given point.
        /// </summary>
        CENTER = HORIZONTAL_CENTER | VERTICAL_CENTER
    }
}
