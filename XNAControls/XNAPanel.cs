﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;

using System;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNAPanel : XNAControl
    {
        public XNAPanel(WindowManager windowManager) : base(windowManager)
        {
            SkipParseTextFromStringManager = true;
            VirtualProperties.Add("Padding", typeof(string));
        }

        public PanelBackgroundImageDrawMode PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        public virtual Texture2D BackgroundTexture { get; set; }

        private Color? _borderColor;

        public Color BorderColor
        {
            get
            {
                if (_borderColor.HasValue)
                    return _borderColor.Value;

                return UISettings.ActiveSettings.PanelBorderColor;
            }
            set { _borderColor = value; }
        }

        public bool DrawBorders { get; set; } = true;

        /// <summary>
        /// If this is set, the XNAPanel will render itself on a separate render target.
        /// After the rendering is complete, it'll set this render target to be the
        /// primary render target.
        /// </summary>
        //public RenderTarget2D OriginalRenderTarget { get; set; }

        //RenderTarget2D renderTarget;

        Texture2D BorderTexture { get; set; }

        /// <summary>
        /// The panel's transparency changing rate per 100 milliseconds.
        /// If the panel is transparent, it'll become non-transparent at this rate.
        /// </summary>
        public float AlphaRate = 0.0f;

        public override void Initialize()
        {
            base.Initialize();

            BorderTexture = AssetLoader.CreateTexture(Color.White, 1, 1);

            //renderTarget = new RenderTarget2D(GraphicsDevice, 
            //    WindowManager.Instance.RenderResolutionX, 
            //    WindowManager.Instance.RenderResolutionY);
        }
        protected override void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case nameof(BorderColor):
                    if (this.TryGet<Color>(property, out var color))
                        BorderColor = color;
                    return;
                case "DrawMode":
                case nameof(PanelBackgroundDrawMode):
                    if (this.TryGet(property, out var value))
                    {
                        if (value == "Tiled")
                            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
                        else if (value == "Centered")
                            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;
                        else
                            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                    }
                    return;
                case nameof(AlphaRate):
                    if (this.TryGet(property, out float f))
                        AlphaRate = f;
                    return;
                case nameof(BackgroundTexture):
                    if (this.TryGet(property, out Texture2D t))
                    {
                        BackgroundTexture = t;
                        if (t.Name.Contains(":"))
                            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                    }
                    return;
                case nameof(DrawBorders):
                    if (this.TryGet(property, out bool b))
                        DrawBorders = b;
                    return;
                case "Padding":
                    if (this.TryGet(property, out var s))
                    {
                        string[] parts = s.Split(',');
                        int left = int.Parse(parts[0]);
                        int top = int.Parse(parts[1]);
                        int right = int.Parse(parts[2]);
                        int bottom = int.Parse(parts[3]);
                        this.SetClientRectangle(X - left, Y - top,
                            Width + left + right, Height + top + bottom);
                        foreach (XNAControl child in Children)
                        {
                            child.SetClientRectangle(child.X + left,
                                child.Y + top, child.Width, child.Height);
                        }
                    }

                    return;
            }
            base.ParseAttributeFromUIConfigurations(property, type);
        }

        public override void Update(GameTime gameTime)
        {
            Alpha += AlphaRate * (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 100.0);

            base.Update(gameTime);
        }

        protected void DrawPanel()
        {
            Color color = RemapColor;

            if (BackgroundTexture != null)
            {
                if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.TILED)
                {
                    if (Renderer.CurrentSettings.SamplerState != SamplerState.LinearWrap &&
                        Renderer.CurrentSettings.SamplerState != SamplerState.PointWrap)
                    {
                        //Renderer.PushSettings(new SpriteBatchSettings(Renderer.CurrentSettings.SpriteSortMode,
                        //    Renderer.CurrentSettings.BlendState, SamplerState.LinearWrap));

                        //DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);

                        //Renderer.PopSettings();
                        // ^ the above should work, but actually doesn't for some reason -
                        // the texture is just scaled instead
                        // it should have much higher performance than repeating the texture manually

                        for (int x = 0; x < Width; x += BackgroundTexture.Width)
                        {
                            for (int y = 0; y < Height; y += BackgroundTexture.Height)
                            {
                                if (x + BackgroundTexture.Width < Width)
                                {
                                    if (y + BackgroundTexture.Height < Height)
                                    {
                                        DrawTexture(BackgroundTexture, new Rectangle(x, y,
                                            BackgroundTexture.Width, BackgroundTexture.Height), color);
                                    }
                                    else
                                    {
                                        DrawTexture(BackgroundTexture,
                                            new Rectangle(0, 0, BackgroundTexture.Width, Height - y),
                                            new Rectangle(x, y,
                                            BackgroundTexture.Width, Height - y), color);
                                    }
                                }
                                else if (y + BackgroundTexture.Height < Height)
                                {
                                    DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, Width - x, BackgroundTexture.Height),
                                        new Rectangle(x, y,
                                        Width - x, BackgroundTexture.Height), color);
                                }
                                else
                                {
                                    DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, Width - x, Height - y),
                                        new Rectangle(x, y,
                                        Width - x, Height - y), color);
                                }
                            }
                        }
                    }
                    else
                    {
                        DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);
                    }
                }
                else if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.CENTERED)
                {
                    int x = (Width - BackgroundTexture.Width) / 2;
                    int y = (Height - BackgroundTexture.Height) / 2;

                    // Calculate texture source rectangle
                    int sourceBeginX = x >= 0 ? 0 : -x;
                    int sourceBeginY = y >= 0 ? 0 : -y;

                    // Calculate draw destination rectangle
                    int destBeginX = x >= 0 ? x : 0;
                    int destBeginY = y >= 0 ? y : 0;

                    // Width and height is shared between both rectangles
                    int drawWidth = x >= 0 ? BackgroundTexture.Width : Width;
                    int drawHeight = y >= 0 ? BackgroundTexture.Height : Height;

                    DrawTexture(BackgroundTexture,
                        new Rectangle(sourceBeginX, sourceBeginY, drawWidth, drawHeight),
                        new Rectangle(destBeginX, destBeginY, drawWidth, drawHeight), color);
                }
                else // if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.STRECHED)
                {
                    DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);
                }
            }
        }

        protected void DrawPanelBorders()
        {
            DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            base.Draw(gameTime);

            if (DrawBorders)
                DrawPanelBorders();
        }
    }

    public enum PanelBackgroundImageDrawMode
    {
        /// <summary>
        /// The texture is tiled to fill the whole surface of the panel.
        /// </summary>
        TILED,

        /// <summary>
        /// The texture is stretched to fill the whole surface of the panel.
        /// </summary>
        STRETCHED,

        /// <summary>
        /// The texture is drawn once, centered on the panel.
        /// If the texture is too large for the panel, parts
        /// that would end up outside of the panel are cut off.
        /// </summary>
        CENTERED
    }
}
