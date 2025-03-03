﻿using System;
using System.Collections.Generic;

namespace RSDKv3_4
{
    public class Animation : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public class AnimationEntry : ICloneable
        {
            public object Clone()
            {
                return this.MemberwiseClone();
            }

            public class Frame : ICloneable
            {
                public object Clone()
                {
                    return this.MemberwiseClone();
                }

                /// <summary>
                /// the spritesheet index
                /// </summary>
                public byte sheet = 0;
                /// <summary>
                /// the hitbox to use for this frame
                /// </summary>
                public byte hitbox = 0;
                /// <summary>
                /// the XPos on the sheet
                /// </summary>
                public byte sprX = 0;
                /// <summary>
                /// the YPos on the sheet
                /// </summary>
                public byte sprY = 0;
                /// <summary>
                /// the frame's width
                /// </summary>
                public byte width = 0;
                /// <summary>
                /// the frame's height
                /// </summary>
                public byte height = 0;
                /// <summary>
                /// the X offset of the frame
                /// </summary>
                public sbyte pivotX = 0;
                /// <summary>
                /// the Y offset of the frame
                /// </summary>
                public sbyte pivotY = 0;

                public Frame() { }

                public Frame(Reader reader)
                {
                    Read(reader);
                }

                public void Read(Reader reader)
                {
                    sheet = reader.ReadByte();
                    hitbox = reader.ReadByte();
                    sprX = reader.ReadByte();
                    sprY = reader.ReadByte();
                    width = reader.ReadByte();
                    height = reader.ReadByte();
                    pivotX = reader.ReadSByte();
                    pivotY = reader.ReadSByte();
                }

                public void Write(Writer writer)
                {
                    writer.Write(sheet);
                    writer.Write(hitbox);
                    writer.Write(sprX);
                    writer.Write(sprY);
                    writer.Write(width);
                    writer.Write(height);
                    writer.Write(pivotX);
                    writer.Write(pivotY);
                }

            }

            public enum RotationStyles
            {
                None,
                Full,
                Snap_45Deg,
                StaticRotation,
            }

            /// <summary>
            /// the name of the animation
            /// </summary>
            public string name = "New Animation";
            /// <summary>
            /// a list of frames in the animation
            /// </summary>
            public List<Frame> frames = new List<Frame>();
            /// <summary>
            /// what frame to loop from
            /// </summary>
            public byte loopIndex = 0;
            /// <summary>
            /// the speed of the animation
            /// </summary>
            public byte speed = 0;
            /// <summary>
            /// the rotation style of the animation
            /// </summary>
            public RotationStyles rotationStyle = RotationStyles.None;

            /// <summary>
            /// the speed of the animation, represented as a float, with 0 being the minimum speed and 1.0 being the maximum speed
            /// the speed value represents how much to incriment the anim timer per frame
            /// E.G: 1.0 will change the anim frame every in-game frame, while 0.5 will change it every 2 in-game frames
            /// </summary>
            public float speedF
            {
                get { return speed / 240.0f; }
                set { speed = (byte)(value < 0 ? 0 : value > 1.0 ? 0xF0 : (value * 240)); }
            }

            public AnimationEntry() { }

            public AnimationEntry(Reader reader)
            {
                Read(reader);
            }

            public void Read(Reader reader)
            {
                name = reader.ReadString();
                short frameCount = reader.ReadByte();
                speed = reader.ReadByte();
                loopIndex = reader.ReadByte();
                rotationStyle = (RotationStyles)reader.ReadByte();

                frames.Clear();
                for (int f = 0; f < frameCount; ++f)
                    frames.Add(new Frame(reader));
            }

            public void Write(Writer writer)
            {
                writer.Write(name);
                writer.Write((byte)frames.Count);
                writer.Write(speed);
                writer.Write(loopIndex);
                writer.Write((byte)rotationStyle);

                foreach (Frame frame in frames)
                    frame.Write(writer);
            }
        }

        public class Hitbox
        {
            public struct HitboxInfo
            {
                public sbyte left;
                public sbyte top;
                public sbyte right;
                public sbyte bottom;
            }

            public enum HitboxIDs
            {
                Outer_Floor,
                Inner_Floor,
                Outer_LWall,
                Inner_LWall,
                Outer_Roof,
                Inner_Roof,
                Outer_RWall,
                Inner_RWall,
                Count
            }

            public HitboxInfo[] Hitboxes = new HitboxInfo[(int)HitboxIDs.Count];

            public Hitbox() { }

            public Hitbox(Reader reader)
            {
                Read(reader);
            }

            public void Read(Reader reader)
            {
                for (int h = 0; h < (int)HitboxIDs.Count; h++)
                {
                    Hitboxes[h].left = reader.ReadSByte();
                    Hitboxes[h].top = reader.ReadSByte();
                    Hitboxes[h].right = reader.ReadSByte();
                    Hitboxes[h].bottom = reader.ReadSByte();
                }
            }

            public void Write(Writer writer)
            {
                for (int h = 0; h < (int)HitboxIDs.Count; h++)
                {
                    writer.Write(Hitboxes[h].left);
                    writer.Write(Hitboxes[h].top);
                    writer.Write(Hitboxes[h].right);
                    writer.Write(Hitboxes[h].bottom);
                }
            }
        }

        /// <summary>
        /// a list of all the spritesheets to be loaded, each sheet path is relative to "Data/Sprites/"
        /// </summary>
        public List<string> spriteSheets = new List<string>();
        /// <summary>
        /// the list of Animations in the file
        /// </summary>
        public List<AnimationEntry> animations = new List<AnimationEntry>();
        /// <summary>
        /// the list of the hitboxes that the animations can use
        /// </summary>
        public List<Hitbox> hitboxes = new List<Hitbox>();

        public Animation() { }

        public Animation(string filename) : this(new Reader(filename)) { }

        public Animation(System.IO.Stream stream) : this(new Reader(stream)) { }

        public Animation(Reader reader)
        {
            Read(reader);
        }

        public void Read(Reader reader)
        {
            // SpriteSheets
            byte spriteSheetCount = reader.ReadByte();
            spriteSheets.Clear();
            for (int s = 0; s < spriteSheetCount; ++s)
                spriteSheets.Add(reader.ReadString());

            // Animations
            byte animationCount = reader.ReadByte();
            animations.Clear();
            for (int a = 0; a < animationCount; ++a)
                animations.Add(new AnimationEntry(reader));

            // Hitboxes
            byte hitboxCount = reader.ReadByte();
            hitboxes.Clear();
            for (int h = 0; h < hitboxCount; ++h)
                hitboxes.Add(new Hitbox(reader));

            reader.Close();
        }

        public void Write(string filename)
        {
            using (Writer writer = new Writer(filename))
                Write(writer);
        }

        public void Write(System.IO.Stream stream)
        {
            using (Writer writer = new Writer(stream))
                Write(writer);
        }

        public void Write(Writer writer)
        {
            // SpriteSheets
            writer.Write((byte)spriteSheets.Count);
            foreach (string sheet in spriteSheets)
                writer.WriteStringRSDK(sheet);

            // Animations
            writer.Write((byte)animations.Count);
            foreach (AnimationEntry anim in animations)
                anim.Write(writer);

            // Hitboxes
            writer.Write((byte)hitboxes.Count);
            foreach (Hitbox hitbox in hitboxes)
                hitbox.Write(writer);

            writer.Close();
        }
    }
}
