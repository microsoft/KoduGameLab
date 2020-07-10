// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Base;
using Boku.Common.ParticleSystem;
using Boku.Programming;
using Boku.Fx;
using Boku.Audio;

namespace Boku.Common
{
    using ScoreMap = Dictionary<int, Scoreboard.Score>;

    [Flags]
    public enum ScoreResetFlags
    {
        Score = 1,                                  // The actually value of the score.  Is reset to 0.
        Active = 2,                                 // For color scores only, is this displayed on screen?
        Visibility = 4,                             // Loud or quiet.  Should never (?) be reset.  Should be part of level state.
        SkipPersistent = 8,                         // Used when reseting between linked levels to skip reseting Score part.
        All = Score | Active,
        AllSkipPersistent = All | SkipPersistent,
    }


    public enum ScoreBucket
    {
        NotApplicable, // assumed to occupy the zero slot.
        ColorFirst = Classification.ColorInfo.First,
        ColorLast = Classification.ColorInfo.Last,
        ScoreA,
        ScoreB,
        ScoreC,
        ScoreD,
        ScoreE,
        ScoreF,
        ScoreG,
        ScoreH,
        ScoreI,
        ScoreJ,
        ScoreK,
        ScoreL,
        ScoreM,
        ScoreN,
        ScoreO,
        ScoreP,
        ScoreQ,
        ScoreR,
        ScoreS,
        ScoreT,
        ScoreU,
        ScoreV,
        ScoreW,
        ScoreX,
        ScoreY,
        ScoreZ,
    }

    /// <summary>
    /// The set of global score registers.
    /// Private (local) scores are stored in a ScoreSet.
    /// </summary>
    public static class Scoreboard
    {
        class CharRenderTarget
        {
            public char ch;
            public int charWidth;
            public int charHeight;

            // Even though we only write to this RenderTarget at startup, we hold onto
            // it for the entire time Kodu is running to ensure its underlying texture
            // isn't at risk of being released. We encountered this issue a while back
            // in another area of the project when running on the 360.
            public RenderTarget2D surface;

            public static CharRenderTarget Create(char ch)
            {
                int shadowOffset = Scoreboard.ShadowOffset;

                CharRenderTarget target = new CharRenderTarget();

                target.ch = ch;
                target.charWidth = shadowOffset + (int)ScoreBoardFont().MeasureString(String.Format("{0}", ch)).X;
                target.charHeight = shadowOffset + ScoreBoardFont().LineSpacing;

                target.CreateRenderTargets(KoiLibrary.GraphicsDevice);

                return target;
            }

            public void UnloadContent()
            {
                ReleaseRenderTargets();
            }

            public void DeviceReset(GraphicsDevice device)
            {
                //ReleaseRenderTargets();
                CreateRenderTargets(device);
            }

            private void ReleaseRenderTargets()
            {
                SharedX.RelRT("Scoreboard", surface);
                DeviceResetX.Release(ref surface);
            }

            private void CreateRenderTargets(GraphicsDevice device)
            {
                int shadowOffset = Scoreboard.ShadowOffset;

                int surfaceWidth = MyMath.GetNextPowerOfTwo(charWidth);
                int surfaceHeight = MyMath.GetNextPowerOfTwo(charHeight);

                string str = String.Format("{0}", ch);

                if (surface == null || surface.IsDisposed || surface.GraphicsDevice.IsDisposed)
                {
                    surface = new RenderTarget2D(
                        KoiLibrary.GraphicsDevice,
                        surfaceWidth,
                        surfaceHeight,
                        false,
                        SurfaceFormat.Color,
                        DepthFormat.None);
                    SharedX.GetRT("Scoreboard", surface);
                }

                InGame.SetRenderTarget(surface);
                InGame.Clear(Color.Transparent);

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                try
                {
                    try
                    {
                        batch.Begin();
                        
                        TextHelper.DrawString(
                            ScoreBoardFont,
                            str,
                            new Vector2(shadowOffset + (surfaceWidth - charWidth) / 2, shadowOffset + (surfaceHeight - charHeight) / 2),
                            Color.Black);
                        TextHelper.DrawString(
                            ScoreBoardFont,
                            str,
                            new Vector2((surfaceWidth - charWidth) / 2, (surfaceHeight - charHeight) / 2),
                            Color.White);
                    }
                    catch(Exception e)
                    {
                        if (e != null)
                        {
                        }
                    }
                    finally
                    {
                        batch.End();
                    }
                }
                catch
                {
                }
                finally
                {
                    InGame.RestoreRenderTarget();
                }

            }
        }

        #region Public Types
        public class Score
        {
            private int curr;
            private bool active;    // True if score should be displayed on screen.
            private bool persist;
            private ScoreVisibility visibility;
            
            private bool labeled = false;
            private string label = "";

            private const ScoreVisibility kDefaultVisibility = ScoreVisibility.Loud;

            public int Prev;

            // For layout
            public int order;

            /// <summary>
            /// Gets or sets the current value of this score.
            /// </summary>
            public int Curr
            {
                get { return curr; }
                set { Prev = curr; curr = value; }
            }

            /// <summary>
            /// True if this score should display on the scoreboard
            /// </summary>
            public bool Active
            {
                get { return active; }
                set { active = value; Scoreboard.ReindexScores(); }
            }

            /// <summary>
            /// Specifies the type of effect this score presents upon changing.
            /// </summary>
            public ScoreVisibility Visibility
            {
                get { return visibility; }
                set 
                {
                    if (visibility != value)
                    {
                        visibility = value; 
                        Scoreboard.ReindexScores(); 
                        InGame.IsLevelDirty = true;
                    }
                }
            }

            public string Label
            {
                get { return label; }
                set
                {
                    if (label != value)
                    {
                        label = value;
                        InGame.IsLevelDirty = true;
                    }
                }
            }

            public bool Labeled
            {
                get { return labeled; }
                set
                {
                    if (labeled != value)
                    {
                        labeled = value; 
                        InGame.IsLevelDirty = true;
                    }
                }
            }

            /// <summary>
            /// Specifies whether the score persists to next level.
            /// </summary>
            public bool PersistFlag
            {
                get { return persist; }
                set 
                {
                    if (persist != value)
                    {
                        persist = value; 
                        InGame.IsLevelDirty = true;
                    }
                }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public Score()
            {
                Reset(ScoreResetFlags.All);
            }

            /// <summary>
            /// Resets some or all properties of this score.
            /// </summary>
            /// <param name="flags"></param>
            public void Reset(ScoreResetFlags flags)
            {
                //if skip persistent flag is set, we skip resetting any value
                if ((0 != (flags & ScoreResetFlags.SkipPersistent)) && persist)
                {
                    return;
                }
                
                //check if score should be reset
                if (0 != (flags & ScoreResetFlags.Score))
                {
                    // Note: the below code is a bug since it will set Prev to 0 and
                    // then set Curr to 0.  Since Curr is an accessor it first sets
                    // Prev to Curr's current value before setting curr to 0.
                    // Side-effects...
                    //Curr = Prev = 0;
                    Curr = 0;
                    Prev = 0;
                }

                //check if Active flag should be reset
                if (0 != (flags & ScoreResetFlags.Active))
                {
                    Active = false;
                }

                //check if Visibility should be reset
                if (0 != (flags & ScoreResetFlags.Visibility))
                {
                    Visibility = kDefaultVisibility;
                }
            }
        }
        #endregion

        #region Private Constants
        
        private const float kLoudEffectSeconds = 1f;
        private const float kQuietEffectSeconds = 0f;
        private const float kFlyingEffectSeconds = 2f;
        private const float kFloatUpHeight = 3f;

        #endregion

        #region Private Types
        private class ScoreEffect
        {
            public Vector3 start;
            public Vector3 end;
            public Vector3 curr;
            public double spawnTime;
            public float alpha;
            public Classification.Colors color;
            public CharRenderTarget[] crts;
            public GameThing thing;

            // Cached information about the thing in case it goes inactive while score is flying.
            public Vector3 thingPosition;
            public float thingBoundingRadius;
        }

        #region Effect Cache
        private enum EffectParams
        {
            WorldViewProj,
            ScoreTexture,
            ScoreSize,
            ScoreColor,
            ScoreColorDarken,
            ScoreAlpha,
        }
        #endregion
        #endregion

        #region Private Static Members

        private static ScoreMap scores = new ScoreMap();
        private static List<ScoreEffect> flyingEffects = new List<ScoreEffect>();
        private static List<ScoreEffect> quietEffects = new List<ScoreEffect>();
        private static Stack<ScoreEffect> freeEffects = new Stack<ScoreEffect>();
        private static Dictionary<char, CharRenderTarget> charRenderTargets = new Dictionary<char, CharRenderTarget>();

        private static Effect effect;
        private static EffectCache effectCache;
        private static VertexBuffer vertexBuf;
        private static GetFont ScoreBoardFont = SharedX.GetGameFont24Bold;

        private static int ShadowOffset { get { return 1; } }

        private static int y_margin;

        #endregion

        #region Accessors
        public static Effect Effect
        {
            get { return effect; }
        }
        #endregion

        #region Public

        public static GetFont GetFont()
        {
            return ScoreBoardFont;
        }

        static Scoreboard()
        {
            for (ScoreBucket bucket = ScoreBucket.ColorFirst; bucket <= ScoreBucket.ScoreZ; ++bucket)
            {
                Score score = new Score();
                scores.Add((int)bucket, score);
            }
        }

        /// <summary>
        /// Resets some or all properties on all scores.
        /// </summary>
        /// <param name="flags"></param>
        public static void Reset(ScoreResetFlags flags)
        {
            ScoreMap.ValueCollection.Enumerator it = scores.Values.GetEnumerator();

            while (it.MoveNext())
            {
                it.Current.Reset(flags);
            }

            ReleaseScoreEffects();
        }

        /// <summary>
        /// Resets some or all properties of the score corresponding to the given bucket.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="flags"></param>
        public static void ResetScore(ScoreBucket bucket, ScoreResetFlags flags)
        {
            if (bucket == ScoreBucket.NotApplicable)
                Reset(flags);
            else if (scores.ContainsKey((int)bucket))
                scores[(int)bucket].Reset(flags);
        }

        /// <summary>
        /// Bring scoreboard up to date so that previous values equal current ones.
        /// </summary>
        public static void FreshenScores()
        {
            ScoreMap.ValueCollection.Enumerator it = scores.Values.GetEnumerator();
            while (it.MoveNext())
            {
                if (it.Current.Prev != it.Current.Curr)
                    it.Current.Prev = it.Current.Curr;
            }
        }

        /// <summary>
        /// Make a copy of the current state of the scoreboard.
        /// </summary>
        /// <param name="scoresCopy"></param>
        public static void Snapshot(Dictionary<int, Scoreboard.Score> scoresCopy)
        {
            ScoreMap.Enumerator it = scores.GetEnumerator();
            while (it.MoveNext())
            {
                if (!scoresCopy.ContainsKey(it.Current.Key))
                    scoresCopy.Add(it.Current.Key, new Score());
                scoresCopy[it.Current.Key].Curr = it.Current.Value.Curr;
                scoresCopy[it.Current.Key].Prev = it.Current.Value.Prev;
            }
        }

        public static int GetScore(GameActor actor, ScoreBucketFilter filter)
        {
            int result = 0;

            if (filter.isPrivate)
            {
                Debug.Assert(actor != null, "Can't have private scores without an actor");
                result = actor.localScores.GetScore(filter.bucket);
            }
            else
            {
                result = scores[(int)filter.bucket].Curr;
            }

            return result;
        }

        public static int GetPrevScore(GameActor actor, ScoreBucketFilter filter)
        {
            int result = 0;

            if (filter.isPrivate)
            {
                Debug.Assert(actor != null, "Can't have private scores without an actor");
                result = actor.localScores.GetPrevScore(filter.bucket);
            }
            else
            {
                result = scores[(int)filter.bucket].Prev;
            }

            return result;
        }

        public static int GetScore(GameActor actor, ScoreBucketModifier modifier)
        {
            int result = 0;

            if (modifier.isPrivate)
            {
                Debug.Assert(actor != null, "Can't have private scores without an actor");
                result = actor.localScores.GetScore(modifier.bucket);
            }
            else
            {
                result = scores[(int)modifier.bucket].Curr;
            }

            return result;
        }

        /// <summary>
        /// Get the current value of a score.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        public static int GetGlobalScore(ScoreBucket bucket)
        {
            return scores[(int)bucket].Curr;
        }

        /// <summary>
        /// Get the value of the score in the previous frame.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        static int GetPrevGlobalScore(ScoreBucket bucket)
        {
            return scores[(int)bucket].Prev;
        }

        public static bool IsColorBucket(ScoreBucket bucket)
        {
            return bucket >= ScoreBucket.ColorFirst && bucket <= ScoreBucket.ColorLast;
        }

        /// <summary>
        /// Set the current value of a score.
        /// </summary>
        /// <param name="bucket">The bucket of the score register</param>
        /// <param name="value">The value the score should become</param>
        /// <param name="targetThing">The thing from which the score effect should originate</param>
        public static void SetScore(ScoreBucket bucket, int value, GameThing targetThing)
        {
            Score score = scores[(int)bucket];

            score.Curr = value;

            int delta = score.Curr - score.Prev;

            if (delta != 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                if (score.Visibility == ScoreVisibility.Loud && IsColorBucket(bucket))
                {
                    if (delta > 0)
                    {
                        Foley.PlayScore(null);
                    }
                    else
                    {
                        Foley.PlayScoreDown(null);
                    }

                    if (targetThing != null && !(targetThing is NullActor))
                    {
                        ScoreEffect scoreEffect = AllocScoreEffect();
                        scoreEffect.thing = targetThing;
                        scoreEffect.thingPosition = targetThing.Movement.Position;
                        scoreEffect.thingBoundingRadius = targetThing.BoundingSphere.Radius;
                        scoreEffect.color = (Classification.Colors)bucket;
                        scoreEffect.start = scoreEffect.curr =
                            targetThing.Movement.Position +
                            targetThing.BoundingSphere.Center +
                            new Vector3(0, 0, targetThing.BoundingSphere.Radius);
                        scoreEffect.end = scoreEffect.start + new Vector3(0, 0, kFloatUpHeight);
                        scoreEffect.alpha = 1f;

                        setScoreCrtList.Clear();

                        /*
                        // Convert score delta to chars without creating a temporary string so that we don't
                        // create junk to be garbage collected (preserving XBOX perf on score-heavy levels).

                        if (delta > 0)
                            setScoreCrtList.Add(charRenderTargets['+']);
                        else if (delta < 0)
                            setScoreCrtList.Add(charRenderTargets['-']);

                        delta = Math.Abs(delta);
                        delta = Math.Min(delta, 1000);

                        int thou = (delta % 10000) / 1000;
                        int hund = (delta % 1000) / 100;
                        int tens = (delta % 100) / 10;
                        int ones = (delta % 10);

                        if (thou > 0)
                            setScoreCrtList.Add(charRenderTargets[(char)('0' + thou)]);
                        if (hund > 0 || thou > 0)
                            setScoreCrtList.Add(charRenderTargets[(char)('0' + hund)]);
                        if (tens > 0 || hund > 0 || thou > 0)
                            setScoreCrtList.Add(charRenderTargets[(char)('0' + tens)]);

                        setScoreCrtList.Add(charRenderTargets[(char)('0' + ones)]);

                        */

                        // Not as memory clean but overcomes the limitations.
                        // Score values are no longer limited to +- 1000
                        // We don't throw when exceeding max int.
                        string deltaStr = delta.ToString();
                        setScoreCrtList.Clear();
                        foreach(char c in deltaStr)
                        {
                            setScoreCrtList.Add(charRenderTargets[c]);
                        }

                        scoreEffect.crts = setScoreCrtList.ToArray();

                        QueueLoudEffect(scoreEffect);
                    }
                    else
                    {
                        ScoreEffect scoreEffect = AllocScoreEffect();
                        scoreEffect.color = (Classification.Colors)bucket;
                        QueueQuietEffect(scoreEffect);
                    }
                }
                else if (score.Visibility == ScoreVisibility.Quiet)
                {
                    ScoreEffect scoreEffect = AllocScoreEffect();
                    scoreEffect.color = (Classification.Colors)bucket;
                    QueueQuietEffect(scoreEffect);
                }
            }
        }

        // A variable local to SetScore. Do not use.
        static List<CharRenderTarget> setScoreCrtList = new List<CharRenderTarget>();

        /// <summary>
        /// Query the score effect setting of a particular register.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ScoreVisibility GetVisibility(Classification.Colors color)
        {
            return scores[(int)color].Visibility;
        }

        /// <summary>
        /// Set the score effect of a particular register.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="visibility"></param>
        public static void SetVisibility(Classification.Colors color, ScoreVisibility visibility)
        {
            scores[(int)color].Visibility = visibility;
        }

        //Returns the corresponding scoreboard score object by color
        public static Scoreboard.Score GetScoreboardScore( Classification.Colors color )
        {
            return scores.ContainsKey((int)color) ? scores[(int)color] : null;
        }

        /// <summary>
        /// Query the score persist setting of a particular register.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool GetPersistFlag(Classification.Colors color)
        {
            return scores[(int)color].PersistFlag;
        }

        /// <summary>
        /// Set the score persist setting of a particular register.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="visibility"></param>
        public static void SetPersistFlag(Classification.Colors color, bool persist)
        {
            scores[(int)color].PersistFlag = persist;
        }

        /// <summary>
        /// Activate a particular register.
        /// </summary>
        /// <param name="color"></param>
        public static void Activate(ScoreBucket bucket)
        {
            scores[(int)bucket].Active = true;
        }

        #endregion

        #region Internal

        internal static void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\ScoreEffect");
                ShaderGlobals.RegisterEffect("ScoreEffect", effect);
            }
        }

        internal static void InitDeviceResources(GraphicsDevice device)
        {
            if (charRenderTargets.Count > 0)
                return;

            effectCache = new EffectCache<EffectParams>();
            effectCache.Load(Effect);

            vertexBuf = new VertexBuffer(device, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            VertexPositionTexture[] verts = new VertexPositionTexture[4] {
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 1)),
            };
            vertexBuf.SetData<VertexPositionTexture>(verts);

            y_margin = 0;

            char[] crtChars = new char[] { '-', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            foreach (char crtChar in crtChars)
            {
                CharRenderTarget crt = CharRenderTarget.Create(crtChar);
                charRenderTargets.Add(crtChar, crt);
            }
        }

        internal static void UnloadContent()
        {
            ReleaseScoreEffects();
            DeviceResetX.Release(ref vertexBuf);
            DeviceResetX.Release(ref effect);

            foreach (CharRenderTarget crt in charRenderTargets.Values)
            {
                crt.UnloadContent();
            }
            charRenderTargets.Clear();
        }

        internal static void DeviceReset(GraphicsDevice device)
        {
            foreach (CharRenderTarget crt in charRenderTargets.Values)
            {
                try
                {
                    crt.DeviceReset(device);
                }
                catch { }
            }
        }

        internal static void Update(Camera camera)
        {
            UpdateQuietEffects(camera);
            UpdateFlyingEffects(camera);
        }

        internal static void Render(Camera camera)
        {
            Vector3 forward = Vector3.Normalize(new Vector3(camera.ViewDir.X, camera.ViewDir.Y, 0));
            RenderQuietEffects(camera, forward);
            RenderFlyingEffects(camera, forward);
            RenderScores();
        }

        internal static void RenderEffects(Camera camera)
        {
            Vector3 forward = Vector3.Normalize(new Vector3(camera.ViewDir.X, camera.ViewDir.Y, 0));
            RenderQuietEffects(camera, forward);
            RenderFlyingEffects(camera, forward);
        }

        internal static void ReindexScores()
        {
            ScoreMap.ValueCollection.Enumerator it = scores.Values.GetEnumerator();

            int index = 0;
            while (it.MoveNext())
            {
                Score score = it.Current;

                if (score.Active && score.Visibility != ScoreVisibility.Off)
                {
                    score.order = index++;
                }
                else
                {
                    score.order = 0;
                }
            }
        }

        #endregion

        #region Private

        private static void UpdateQuietEffects(Camera camera)
        {
            double currTime = Time.GameTimeTotalSeconds;

            for (int i = 0; i < quietEffects.Count; ++i)
            {
                ScoreEffect scoreEffect = quietEffects[i];

                float timeElapsed = (float)(currTime - scoreEffect.spawnTime);
                if (timeElapsed >= kQuietEffectSeconds)
                {
                    quietEffects.RemoveAt(--i + 1);
                    ReleaseScoreEffect(scoreEffect);
                    continue;
                }

                Score score = scores[(int)scoreEffect.color];
                Viewport viewport = KoiLibrary.GraphicsDevice.Viewport;
                Vector3 pos = new Vector3(
                    (float)viewport.Width * 0.9f,
                    y_margin + ScoreBoardFont().LineSpacing * score.order,
                    1f);
                scoreEffect.curr = scoreEffect.start = scoreEffect.end = viewport.Unproject(
                    pos,
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);
            }
        }

        private static void UpdateFlyingEffects(Camera camera)
        {
            double currTime = Time.GameTimeTotalSeconds;

            for (int i = 0; i < flyingEffects.Count; ++i)
            {
                ScoreEffect scoreEffect = flyingEffects[i];

                float timeElapsed = (float)(currTime - scoreEffect.spawnTime);
                if (timeElapsed > kFlyingEffectSeconds)
                {
                    flyingEffects.RemoveAt(--i + 1);
                    ReleaseScoreEffect(scoreEffect);
                    continue;
                }

                Score score;
                scores.TryGetValue((int)scoreEffect.color, out score);

                Viewport viewport = KoiLibrary.GraphicsDevice.Viewport;

                float pct = timeElapsed / kFlyingEffectSeconds;

                // If the thing went to the recycle bin, remove our reference to it.
                if (scoreEffect.thing != null && scoreEffect.thing.CurrentState == GameThing.State.Inactive)
                    scoreEffect.thing = null;

                // If we still have a reference to the thing, update the source position of the flying score's path.
                if (scoreEffect.thing != null)
                    scoreEffect.thingPosition = scoreEffect.thing.Movement.Position;

                Vector3 pt0 = scoreEffect.thingPosition + new Vector3(0, 0, scoreEffect.thingBoundingRadius * 2);

                Vector3 pt1 = new Vector3(
                    (float)viewport.Width * 0.5f,
                    (float)viewport.Height * 0.3f,
                    0.75f);
                pt1 = viewport.Unproject(
                    pt1,
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);

                Vector3 pt2 = new Vector3(
                    (float)viewport.Width * 0.6f,
                    (float)viewport.Height * 0.1f,
                    0.5f);
                pt2 = viewport.Unproject(
                    pt1,
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);

                Vector3 pt3 = new Vector3(
                    (float)viewport.Width * 0.9f,
                    y_margin + ScoreBoardFont().LineSpacing * score.order,
                    0.5f);
                pt3 = viewport.Unproject(
                    pt3,
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);

                scoreEffect.curr = MyMath.CubicBezier(pt0, pt1, pt2, pt3, pct);

                scoreEffect.alpha = MyMath.Clamp((1f - pct) * 5, 0, 1);
            }
        }

        private static ScoreEffect AllocScoreEffect()
        {
            ScoreEffect effect;

            if (freeEffects.Count > 0)
                effect = freeEffects.Pop();
            else
                effect = new ScoreEffect();

            return effect;
        }

        private static void ReleaseScoreEffect(ScoreEffect scoreEffect)
        {
            scoreEffect.thing = null;

            freeEffects.Push(scoreEffect);
        }

        private static void ReleaseScoreEffects()
        {
            while (quietEffects.Count > 0)
            {
                ScoreEffect scoreEffect = quietEffects[0];
                ReleaseScoreEffect(scoreEffect);
                quietEffects.RemoveAt(0);
            }

            while (flyingEffects.Count > 0)
            {
                ScoreEffect scoreEffect = flyingEffects[0];
                ReleaseScoreEffect(scoreEffect);
                flyingEffects.RemoveAt(0);
            }
        }

        private static void QueueFlyingEffect(ScoreEffect scoreEffect)
        {
            scoreEffect.spawnTime = Time.GameTimeTotalSeconds;
            scoreEffect.start = scoreEffect.curr = scoreEffect.end;
            scoreEffect.end = Vector3.Zero;
            flyingEffects.Add(scoreEffect);
        }

        private static void QueueQuietEffect(ScoreEffect scoreEffect)
        {
            scoreEffect.spawnTime = Time.GameTimeTotalSeconds;
            quietEffects.Add(scoreEffect);
        }

        private static void QueueLoudEffect(ScoreEffect scoreEffect)
        {
            scoreEffect.spawnTime = Time.GameTimeTotalSeconds;
            //loudEffects.Add(scoreEffect);
            flyingEffects.Add(scoreEffect);
        }

        private static void RenderScores()
        {
            // Render the score registers in the order they appear in the CardSpace.xml file.
            ScoreMap.Enumerator it = scores.GetEnumerator();

            while (it.MoveNext())
            {
                Score score = it.Current.Value;

                if (score.Active && score.Visibility != ScoreVisibility.Off && IsColorBucket((ScoreBucket)it.Current.Key))
                {
                    RenderScore(score, (Classification.Colors)it.Current.Key);
                }
            }
        }

        private static void RenderScore(Score score, Classification.Colors color)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            Color fore = Classification.XnaColor(color);
            Color back = fore == Color.Black ? Color.White : Color.Black;

            string str = score.Labeled ? score.Label +": " : ""; // <----  Should include label
            str += score.Curr.ToString();
            

            int width = 8 + (int)ScoreBoardFont().MeasureString(str).X;
            int x = (int)((float)KoiLibrary.GraphicsDevice.Viewport.Width - width);
            int y = y_margin + score.order * ScoreBoardFont().LineSpacing + (int)BokuGame.ScreenPosition.Y;

            batch.Begin();
            TextHelper.DrawStringWithShadow(ScoreBoardFont, batch, x, y, str, fore, back, false);
            batch.End();
        }

        private static void RenderQuietEffects(Camera camera, Vector3 forward)
        {
            RenderEffects(quietEffects, camera, forward);
        }

        private static void RenderFlyingEffects(Camera camera, Vector3 forward)
        {
            RenderEffects(flyingEffects, camera, forward);
        }

        private static void RenderEffects(List<ScoreEffect> scoreEffects, Camera camera, Vector3 forward)
        {
            if (scoreEffects.Count > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                Effect.CurrentTechnique = Effect.Techniques[0];

                ShaderGlobals.FixExplicitBloom(0.15f);
                for (int i = 0; i < Effect.CurrentTechnique.Passes.Count; ++i)
                {
                    EffectPass pass = Effect.CurrentTechnique.Passes[i];

                    pass.Apply();
                    for (int j = 0; j < scoreEffects.Count; ++j)
                    {
                        ScoreEffect scoreEffect = scoreEffects[j];
                        RenderEffect(scoreEffect, camera, forward);
                    }
                }
                ShaderGlobals.ReleaseExplicitBloom();
            }
        }

        static Vector4 kScoreColorDarken = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
        static float kEffectScalar = 0.0025f;
        private static void RenderEffect(ScoreEffect scoreEffect, Camera camera, Vector3 forward)
        {
            if (scoreEffect.crts == null)
                return;

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            Vector3 diff = scoreEffect.curr - camera.ActualFrom;
            float dist = diff.Length();

            Parameter(EffectParams.ScoreAlpha).SetValue(scoreEffect.alpha);
            Parameter(EffectParams.ScoreColor).SetValue(Classification.ColorVector4(scoreEffect.color));
            Parameter(EffectParams.ScoreColorDarken).SetValue(kScoreColorDarken);

            device.SetVertexBuffer(vertexBuf);
            device.Indices = SharedX.QuadIndexBuff;

            Matrix translation = new Matrix();
            Matrix world = Matrix.CreateBillboard(
                scoreEffect.curr,
                camera.ActualFrom,
                camera.ViewUp,
                forward);

            float distanceScalar = kEffectScalar * dist;
            float offset = 0;

            for (int i = 0; i < scoreEffect.crts.Length; ++i)
            {
                CharRenderTarget crt = scoreEffect.crts[i];

                if (i > 0)
                    offset += crt.charWidth / 2;

                translation = Matrix.CreateTranslation(world.Left * offset * distanceScalar);
                Matrix worldViewProj = world * translation * camera.ViewMatrix * camera.ProjectionMatrix;

                Vector2 scoreSize = new Vector2(crt.surface.Width, crt.surface.Height) * distanceScalar;

                Parameter(EffectParams.WorldViewProj).SetValue(worldViewProj);
                Parameter(EffectParams.ScoreSize).SetValue(scoreSize);
                Parameter(EffectParams.ScoreTexture).SetValue(crt.surface);

                Effect.CurrentTechnique.Passes[0].Apply();

                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);

                offset += crt.charWidth / 2;
            }
        }

        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }

        #endregion
    }
}
