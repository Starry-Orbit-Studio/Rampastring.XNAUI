﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// A toggleable sound effect that can also have a defined priority
    /// and a decay rate for the priority.
    /// </summary>
    public sealed class EnhancedSoundEffect : IDisposable
    {
        /// <summary>
        /// Creates a new prioritized sound. Loads the specified sound asset.
        /// </summary>
        /// <param name="assetName">The asset name of the sound file to load.</param>
        private EnhancedSoundEffect(string assetName) : this(AssetLoader.LoadSound(assetName))
        {
        }

        /// <summary>
        /// Creates a new enhanced sound. Uses the given sound assert.
        /// </summary>
        /// <param name="soundEffect">The sound effect.</param>
        private EnhancedSoundEffect(SoundEffect soundEffect)
        {
            this.soundEffect = soundEffect;
        }

        /// <summary>
        /// Creates a new enhanced sound. Loads the specified sound asset.
        /// </summary>
        /// <param name="assetName">The asset name of the sound file to load.</param>
        /// <param name="priority">The priority of this sound</param>
        /// <param name="priorityDecayRate">The priority decay rate of this sound.</param>
        /// <param name="repeatPrevention">If set above zero, will prevent the sound from being played again
        /// for the specified number of seconds after it has been played.</param>
        private EnhancedSoundEffect(string assetName, double priority, double priorityDecayRate, float repeatPrevention) : this(assetName)
        {
            Priority = priority;
            PriorityDecayRate = priorityDecayRate;
            RepeatPrevention = repeatPrevention;
        }

        private SoundEffect soundEffect;
        private DateTime lastPlayTime;

        /// <summary>
        /// Gets or sets a bool that determines whether this sound is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The priority of this sound.
        /// While this sound is playing, sounds with less priority than this sound's
        /// priority will not be played.
        /// </summary>
        public double Priority { get; set; }

        /// <summary>
        /// The decay rate of this sound's priority.
        /// The sound's priority will be reduced by the specified number each second.
        /// </summary>
        public double PriorityDecayRate { get; set; }

        /// <summary>
        /// If set above zero, will prevent the sound from being played again
        /// for the specified number of seconds after it has been played.
        /// </summary>
        public float RepeatPrevention { get; set; }

        /// <summary>
        /// Plays this sound if it's enabled.
        /// </summary>
        public void Play()
        {
            if (!Enabled || soundEffect == null)
                return;

            if (RepeatPrevention > 0f)
            {
                var dtn = DateTime.Now;

                if ((dtn - lastPlayTime).TotalSeconds < RepeatPrevention)
                    return;

                lastPlayTime = dtn;
            }

            SoundPlayer.Play(this);
        }

        internal SoundEffectInstance CreateSoundInstance()
        {
            if (soundEffect == null)
                return null;

            return soundEffect.CreateInstance();
        }

        /// <summary>
        /// Disposes the sound effect.
        /// </summary>
        public void Dispose()
        {
            soundEffect?.Dispose();
        }

        private static readonly Dictionary<string, EnhancedSoundEffect> _cached = new Dictionary<string, EnhancedSoundEffect>(StringComparer.OrdinalIgnoreCase);
        public static EnhancedSoundEffect GetOrCreate(string assetName) => GetOrCreate(assetName, default, default, default);
        public static EnhancedSoundEffect GetOrCreate(string assetName, double priority, double priorityDecayRate, float repeatPrevention)
        {
            assetName = assetName
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            var key = $"{assetName};{priority};{priorityDecayRate};{repeatPrevention}";

            if (!_cached.TryGetValue(key, out EnhancedSoundEffect result))
            {
                _cached.Add(key, result = new EnhancedSoundEffect(assetName, priority, priorityDecayRate, repeatPrevention));
            }

            return result;
        }
    }

    sealed class PrioritizedSoundInstance : IDisposable
    {
        public PrioritizedSoundInstance(SoundEffectInstance soundInstance,
            double priority, double priorityDecayRate)
        {
            SoundInstance = soundInstance;
            Priority = priority;
            PriorityDecayRate = priorityDecayRate;
        }

        public SoundEffectInstance SoundInstance { get; private set; }

        public double Priority { get; private set; }

        public double PriorityDecayRate { get; private set; }

        /// <summary>
        /// Updates the priority of the sound. Returns true if the sound effect is
        /// still playing, otherwise false.
        /// </summary>
        /// <param name="gameTime">Tells how much time has passed since the previous frame.</param>
        /// <returns>True if the sound effect is still playing, otherwise false.</returns>
        public bool Update(GameTime gameTime)
        {
            Priority = Priority - PriorityDecayRate * gameTime.ElapsedGameTime.TotalSeconds;

            if (SoundInstance.State != SoundState.Playing)
                return false;

            return true;
        }

        public void Dispose()
        {
            SoundInstance.Dispose();
        }
    }
}
