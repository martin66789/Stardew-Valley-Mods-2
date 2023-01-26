﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MPInfo {
    public class PlayerInfoBox : IClickableMenu {
        public Farmer Who { get; }

        public static Texture2D? Crown { get; set; }

        private static Texture2D Texture => Game1.mouseCursors;
        private static Rectangle SourceRectIconBackground => new(293, 360, 24, 24);
        private static Rectangle SourceRectIconBackgroundSelf => new(163, 399, 24, 24);

        private static Rectangle[] SourceRectInfoDisplay => new[]
        {
            new Rectangle(317, 361, 3, 22), //Left
            new Rectangle(320, 361, 2, 22), //Middle (Expands based on given info)
            new Rectangle(322, 361, 7, 22)  //Right
        };

        private static Rectangle[] SourceRectIconPassOut => new[]
        {
            new Rectangle(195, 408, 4, 8), //Vertical
            new Rectangle(193, 410, 8, 4), //Horizontal
            new Rectangle(194, 409, 1, 1)  //Corner
        };

        private static Rectangle SourceRectIconEnergy => new(0, 428, 10, 10);
        private static Rectangle SourceRectIconHealth => new(0, 438, 10, 10);
        private static Rectangle SourceRectIconSkull => new(140, 428, 10, 10);
        private static Rectangle SourceRectIconCrown => new(0, 0, 9, 7);

        private readonly Config Config;
        private string hoverText = "";

        public PlayerInfoBox(int x, int y, Farmer who, Config config) {
            this.Who = who;
            this.Config = config;
            this.xPositionOnScreen = x;
            this.yPositionOnScreen = y;
            this.width = 96 + 12 + 112 + 28;
            this.height = 96;
        }

        public override void performHoverAction(int x, int y) {
            base.performHoverAction(x, y);
            if (new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, 96, 96).Contains(x, y))
                hoverText = $"{Who.Name}{(Game1.player.UniqueMultiplayerID == this.Who.UniqueMultiplayerID ? " (Me)" : (Game1.serverHost.Value.UniqueMultiplayerID == this.Who.UniqueMultiplayerID ? " (Host)" : ""))}";
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
            if (oldBounds != newBounds) {
                Debounce(() => {
                    int index = 0;
                    foreach (var pib in Game1.onScreenMenus.Where(x => x is PlayerInfoBox).OfType<PlayerInfoBox>()) {
                        pib.xPositionOnScreen = 32;
                        pib.yPositionOnScreen = (Game1.uiViewport.Height - 32 - 96) - (112 * index);
                        index++;
                    }
                });
            }
        }

        private static Action Debounce(Action func, int milliseconds = 300) {
            CancellationTokenSource? cancelTokenSource = null;

            return () => {
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();
                Task.Delay(milliseconds, cancelTokenSource.Token)
                    .ContinueWith(t => {
                        if (t.IsCompletedSuccessfully) {
                            func();
                        }
                    }, TaskScheduler.Default);
            };
        }

        public override void draw(SpriteBatch b) {
            if (!this.Config.Enabled || (!this.Config.ShowSelf && Who.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID))
                return;

            base.draw(b);
            var yPositionOnScreen = 0;
            b.Draw(Texture, new(xPositionOnScreen, yPositionOnScreen), Who == Game1.player ? SourceRectIconBackgroundSelf : SourceRectIconBackground, Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
            b.Draw(Texture, new(xPositionOnScreen + 96, yPositionOnScreen + 4), SourceRectInfoDisplay[0], Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
            b.Draw(Texture, new(xPositionOnScreen + 96 + 12, yPositionOnScreen + 4), SourceRectInfoDisplay[1], Color.White, 0.0f, Vector2.Zero, new Vector2(56f, 4f), SpriteEffects.None, 0.88f);
            b.Draw(Texture, new(xPositionOnScreen + 96 + 12 + 112, yPositionOnScreen + 4), SourceRectInfoDisplay[2], Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);

            FarmerRenderer.isDrawingForUI = true;
            Who.FarmerRenderer.drawMiniPortrat(b, new(xPositionOnScreen + 16, yPositionOnScreen + 15), 0.89f, 4f, 0, Who);
            if (Who.health <= 0) //Icon to display being knocked out
            {
                b.Draw(Game1.fadeToBlackRect, new(xPositionOnScreen + 12, yPositionOnScreen + 12, 72, 72), new(0, 0, 1, 1), Color.Black * 0.6f, 0.0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                b.Draw(Texture, new(xPositionOnScreen + 32, yPositionOnScreen + 39), SourceRectIconSkull, Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
            }
            if (Who.passedOut || Who.FarmerSprite.isPassingOut()) //Icon to display passing out
            {
                b.Draw(Game1.fadeToBlackRect, new(xPositionOnScreen + 12, yPositionOnScreen + 12, 72, 72), new(0, 0, 1, 1), Color.Black * 0.6f, 0.0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                b.Draw(Texture, new(xPositionOnScreen + 40, yPositionOnScreen + 39), SourceRectIconPassOut[0], Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                b.Draw(Texture, new(xPositionOnScreen + 32, yPositionOnScreen + 47), SourceRectIconPassOut[1], Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                for (int i = 0; i < 2; i++) //Yes the corners are necessary
                    for (int j = 0; j < 2; j++)
                        b.Draw(Texture, new(xPositionOnScreen + 36 + (20 * j), yPositionOnScreen + 43 + (20 * i)), SourceRectIconPassOut[2], Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
            }
            if (this.Config.ShowHostCrown && Crown is not null && Game1.MasterPlayer.UniqueMultiplayerID == Who.UniqueMultiplayerID)
                b.Draw(Crown, new(xPositionOnScreen - 16, yPositionOnScreen + 16), SourceRectIconCrown, Color.White, -.8f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
            FarmerRenderer.isDrawingForUI = false;

            b.Draw(Texture, new(xPositionOnScreen + 96 + 4, yPositionOnScreen + 4 + 26), SourceRectIconHealth, Color.White, 0.0f, Vector2.Zero, 2f, SpriteEffects.None, 0.89f);
            b.DrawString(Game1.smallFont, $"{Who.health}/{Who.maxHealth}", new(xPositionOnScreen + 96 + 8 + 24, yPositionOnScreen + 4 + 20), (Who.health <= (float)(Who.maxHealth / 10)) ? Color.Red : (Who.health <= (float)(Who.maxHealth / 5) ? Color.Yellow : Game1.textColor), 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.89f);
            b.Draw(Texture, new(xPositionOnScreen + 96 + 4, yPositionOnScreen + 4 + 50), SourceRectIconEnergy, Color.White, 0.0f, Vector2.Zero, 2f, SpriteEffects.None, 0.89f);
            b.DrawString(Game1.smallFont, $"{Math.Round(Who.stamina)}/{Who.maxStamina}", new(xPositionOnScreen + 96 + 8 + 24, yPositionOnScreen + 4 + 44), (Who.stamina <= (float)(Who.maxStamina.Value / 10)) ? Color.Red : (Who.stamina <= (float)(Who.maxStamina.Value / 5) ? Color.Yellow : Game1.textColor), 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.89f);
            if (!string.IsNullOrWhiteSpace(hoverText)) {
                drawHoverText(b, hoverText, Game1.smallFont);
                hoverText = "";
            }
        }
    }
}