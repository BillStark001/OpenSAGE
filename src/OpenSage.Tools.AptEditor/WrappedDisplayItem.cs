﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.FileFormats.Apt;
using OpenSage.FileFormats.Apt.Characters;
using OpenSage.FileFormats.Apt.FrameItems;
using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Tools.AptEditor
{
    public class WrappedDisplayItem : DisplayItem
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public DisplayItem Item { get; }
        public new AptContext Context => Item.Context;
        public new Character Character => Item.Character;
        public new ASObject ScriptObject => Item.ScriptObject;
        public WrappedDisplayItem(Character character, AptContext context, SpriteItem parent)
        {
            Visible = true;
            Item = AddDisposable<DisplayItem>(character switch
            {
                Playable _ => new SpriteItem(),
                Button _ => new ButtonItem(),
                _ => new RenderItem(),
            });
            Item.Transform = ItemTransform.None;
            Item.Create(character, context, parent);
            Transform = Item.Transform;
            Name = $"(Wrapped){Item.Name}";
            // ScriptObject = Item.ScriptObject;
        }

        public override void Update(TimeInterval gt)
        {
            base.Update(gt);
        }

        public override void EnqueueActions(TimeInterval gt)
        {
            Item.EnqueueActions(gt);
        }

        public override void Create(Character character, AptContext context, SpriteItem? parent = null)
        {
            throw new NotSupportedException();
        }

        protected override void RenderImpl(AptRenderingContext renderingContext)
        {
            Item.Render(renderingContext);
        }

        // Play frames without executing actions, since currently we can't handle all actions properly anyway.
        // TODO: may cause a huge perfoemance loss
        public void PlayToFrameNoActions(int frameNumber)
        {
            if (!(Item is SpriteItem))
            {
                return;
            }
            // reset to initial state
            Reset(Item);

            for (var i = 0; i <= frameNumber; ++i)
            {
                UpdateNextFrameNoActions();
            }
        }

        public void UpdateNextFrameNoActions()
        {
            if (Item is SpriteItem sprite)
            {
                UpdateNextFrameNoActions(sprite);
            }
        }

        private static void Reset(DisplayItem display)
        {
            // reset to initial state
            display.Create(display.Character, display.Context, display.Parent);
            if (display is SpriteItem sprite)
            {
                // stop the sprite, otherwise OpenSage may execute some
                // actionscript which we might not be able to handle yet
                sprite.Stop();

                // reset all subitems
                foreach (var item in sprite.Content.Items.Values)
                {
                    Reset(item);
                }
            }
        }

        // try to execute actions!
        private static void UpdateNextFrameNoActions(SpriteItem sprite)
        {
            if (!((Playable) sprite.Character).Frames.Any())
            {
                Logger.Warn("Detected playable without any frames!");
                return;
            }

            //get the current frame
            var frame = GetFrames(sprite)[sprite.CurrentFrame];

            //process all frame items, except labels and actions
            foreach (var item in frame.FrameItems)
            {
                switch (item)
                {
                    case FrameLabel _:
                        break;
                    default:
                        sprite.HandleFrameItem(item);
                        break;
                }
            }

            sprite.NextFrame();
            //reset to the start, we are looping by default
            if (sprite.CurrentFrame >= GetFrames(sprite).Count)
            {
                sprite.GotoFrame(0);
            }

            //update all subItems
            foreach (var item in sprite.Content.Items.Values)
            {
                switch (item)
                {
                    case SpriteItem childSprite:
                        UpdateNextFrameNoActions(childSprite);
                        break;
                    case ButtonItem button:
                    case RenderItem render:
                        // currently these item's Update does nothing
                        item.Update(new TimeInterval(sprite.Context.MsPerFrame, 0));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            sprite.Stop();
        }

        private static List<Frame> GetFrames(SpriteItem sprite)
        {
            return ((Playable) sprite.Character).Frames;
        }

        public override DisplayItem GetMouseFocus(Vector2 mousePos)
        {
            var r = Item.GetMouseFocus(mousePos);
            // Logger.Info(r == null ? "null" : $"{r.GetHashCode()}|{r.Name}|{r.ToString()}");
            return r;
        }

        public override bool HandleEvent(ClipEventFlags flags)
        {
            return Item.HandleEvent(flags);
        }
    }
}