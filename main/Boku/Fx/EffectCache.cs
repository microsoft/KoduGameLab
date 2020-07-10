// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from SimWorld namespace

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Fx
{
    public class EffectCache<EffectParamsEnum, EffectTechsEnum> : EffectCacheWithTechs
    {
        public EffectCache() : base( new Type[]{ typeof(EffectParamsEnum) }, new Type[]{ typeof(EffectTechsEnum) } ) { }
    }

    public class EffectCache<EffectParamsEnum> : EffectCacheWithParams
    {
        public EffectCache() : base( typeof(EffectParamsEnum) ) { }
    }

    public class EffectCacheWithTechs : EffectCacheWithParams
    {
        protected string[] effectTechNames;
        protected int effectTechsLength;

        /// <summary>
        /// Create an EffectCache that will use the given enum types to automatically
        /// load EffectParameters and EffectTechniques using the names from the enums.
        /// No loading is done, however, until Load() is called.
        /// </summary>
        public EffectCacheWithTechs(Type[] effectParamEnums, Type[] effectTechEnums)
            : base(effectParamEnums)
        {
            var tempNames = new List<KeyValuePair<int, string>>();
            var maxValue = 0;
            for (int i = 0; i < effectTechEnums.Length; i++)
            {
                var values = Enum.GetValues(effectTechEnums[i]) as int[];
                var names = Enum.GetNames(effectTechEnums[i]);
                var length = names.Length;
                for (int j = 0; j < length; j++)
                {
                    tempNames.Add(new KeyValuePair<int, string>(values[j], names[j]));
                    maxValue = Math.Max(maxValue, values[j]);
                }
            }
            effectTechsLength = maxValue + 1;

            effectTechNames = new string[effectTechsLength];

            for (int i = 0; i < tempNames.Count; i++)
                effectTechNames[tempNames[i].Key] = tempNames[i].Value;
        }
        
        /// <summary>
        /// Load the parameters and techniques (even though no technique
        /// extension is given, we have the EffectTechs enum so we'll go
        /// ahead an assume it is safe to load those.)
        /// </summary>
        /// <param name="effect"></param>
        public override void Load(Effect effect)
        {
            /* Since we've been given the enums for the EffectTechs, it
             * is safe to assume that we want to load our techniques
             * even if no technique extension is given.
             */
            LoadTechniques(effect, "");
            base.Load(effect);
        }

        /// <summary>
        /// Load all our special and regular techniques
        /// </summary>
        protected override void LoadTechniques(Effect effect, string techniqueExt)
        {
            ReserveTechniques(effectTechsLength);

            LoadSpecialTechniques(effect, techniqueExt);

            for (int i = 0; i < effectTechsLength; i++)
            {
                var techName = effectTechNames[i];
                if (!String.IsNullOrEmpty(techName))
                {
                    LoadTechnique(effect, i, techName, techniqueExt);
                }
            }
        }
    }

    public class EffectCacheWithParams : EffectCache
    {
        protected string[] effectParamNames;
        protected int effectParamsLength;

        protected override int NumParams
        {
            get { return effectParamsLength; }
        }
        protected override string ParamName(int idx)
        {
            return effectParamNames[idx];
        }

        /// <summary>
        /// Create an EffectCache that will use the given enum types to automatically
        /// load EffectParameters using the names from the enums. No loading is done, 
        /// however, until Load() is called.
        /// </summary>
        public EffectCacheWithParams(params Type[] effectParamEnums)
        {
            var tempNames = new List<KeyValuePair<int, string>>();
            var maxValue = 0;
            for (int i = 0; i < effectParamEnums.Length; i++)
            {
                var values = Enum.GetValues(effectParamEnums[i]) as int[];
                var names = Enum.GetNames(effectParamEnums[i]);
                var length = names.Length;
                for (int j = 0; j < length; j++)
                {
                    tempNames.Add(new KeyValuePair<int, string>(values[j], names[j]));
                    maxValue = Math.Max(maxValue, values[j]);
                }
            }
            effectParamsLength = maxValue + 1;

            effectParamNames = new string[effectParamsLength];

            for (int i = 0; i < tempNames.Count; i++)
                effectParamNames[tempNames[i].Key] = tempNames[i].Value;
        }
    }

    abstract public class EffectCache
    {
        #region MEMBERS
        private EffectTechnique[] techniques = null;
        private EffectParameter[] parameters = null;
        #endregion MEMBERS

        #region PUBLIC
        /// <summary>
        /// No technique extension, only parameters loaded.
        /// </summary>
        /// <param name="effect"></param>
        virtual public void Load(Effect effect)
        {
            LoadParameters(effect);
        }
        /// <summary>
        /// Load up all special effect techniques plus TexturedColorPass+ext and NonTexturedColorPass+ext.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="techniqueExt"></param>
        virtual public void Load(Effect effect, string techniqueExt)
        {
            LoadTechniques(effect, techniqueExt);
            LoadParameters(effect);
        }
        /// <summary>
        /// Load up all parameters, plus all special effect techniques.
        /// Reserve space for totalNumTech tech, but don't load them,
        /// assumes they'll be set explicitly with LoadTechnique(...). 
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="techniqueExt"></param>
        /// <param name="totalNumTech"></param>
        virtual public void Load(Effect effect, string techniqueExt, int totalNumTech)
        {
            ReserveTechniques(totalNumTech);
            if (totalNumTech > 0)
            {
                LoadSpecialTechniques(effect, techniqueExt);
            }
            LoadParameters(effect);
        }
        /// <summary>
        /// Throw it all away
        /// </summary>
        virtual public void UnLoad()
        {
            techniques = null;
            parameters = null;
        }

        /// <summary>
        /// Look up the proper technique for the InGame.RenderEffects pass.
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="textured"></param>
        /// <returns></returns>
        public EffectTechnique Technique(InGame.RenderEffect pass, bool textured)
        {
            if (pass == InGame.RenderEffect.Normal)
            {
                return techniques[(int)pass + (textured ? 1 : 0)];
            }
            return techniques[(int)pass];
        }
        /// <summary>
        /// Look up the technique
        /// </summary>
        /// <param name="pass">Must be either an int or an enum</param>
        /// <typeparam name="EffectParamsEnum">Must be either int or an enum</typeparam>
        public virtual EffectTechnique Technique(int pass)
        {
            return techniques[pass];
        }

        /// <summary>
        /// Look up the parameter
        /// </summary>
        /// <param name="idx">Must be either an int or an enum</param>
        /// <typeparam name="EffectParamsEnum">Must be either int or an enum</typeparam>
        public virtual EffectParameter Parameter(int idx)
        {
            return parameters[idx];
        }

        #region ABSTRACT
        /// <summary>
        /// Derived class must provide the number of parameters.
        /// </summary>
        abstract protected int NumParams
        {
            get;
        }
        /// <summary>
        /// Derived class must map parameter index into name used in effect.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        abstract protected string ParamName(int idx);
        #endregion ABSTRACT

        #endregion PUBLIC

        #region INTERNAL
        ///
        /// Internals follow
        /// 

        /// <summary>
        /// Load up all parameters based on overridden param funcs
        /// </summary>
        /// <param name="effect"></param>
        protected void LoadParameters(Effect effect)
        {
            int numParams = NumParams;
            parameters = new EffectParameter[numParams];
            for (int i = 0; i < numParams; ++i)
            {
                var paramName = ParamName(i);
                if (!String.IsNullOrEmpty(paramName))
                {
                    parameters[i] = effect.Parameters[paramName];
                    Debug.Assert(parameters[i] != null, String.Format("Parameter '{0}' not found in shader", ParamName(i)));
                }
            }
        }

        /// <summary>
        /// Find the techique named (name+ext). If that doesn't exist, fall
        /// back to the extension-less version (just name). Means only techniques
        /// that actually do something need to be defined, e.g. yes ShadowPassWithFlex,
        /// but no ShadowPassFoliage.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="name"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        static protected EffectTechnique FindTechnique(Effect effect, string name, string ext)
        {
            EffectTechnique tech = null;
            if (BokuSettings.Settings.PreferReach)
            {
                tech = effect.Techniques[name + ext + "_SM2"];
            }
            else
            {
                tech = effect.Techniques[name + ext + "_SM3"];
            }
            if (tech == null)
            {
                tech = effect.Techniques[name + ext];
                if ((tech == null) && ext.EndsWith("_SURF"))
                {
                    string stripExt = ext.Remove(ext.Length - "_SURF".Length, "_SURF".Length);
                    tech = effect.Techniques[name + stripExt];
                }
                if (tech == null)
                {
                    tech = effect.Techniques[name];
                }
            }
            return tech;
        }

        /// <summary>
        /// Load up the standard system effect modes, using the specified extention.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="techniqueExt"></param>
        protected void LoadSpecialTechniques(Effect effect, string techniqueExt)
        {
            int specialEffectCnt = (int)InGame.RenderEffect.Normal;
            for (int effectIdx = 0; effectIdx < specialEffectCnt; ++effectIdx)
            {
                InGame.RenderEffect pass = (InGame.RenderEffect)effectIdx;

                techniques[effectIdx] = FindTechnique(effect, pass.ToString(), techniqueExt);
            }
        }

        /// <summary>
        /// Load up special effects and the two standard (textured or not) with 
        /// specified extension.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="techniqueExt"></param>
        protected virtual void LoadTechniques(Effect effect, string techniqueExt)
        {
            ReserveTechniques((int)InGame.RenderEffect.Normal + 2);

            LoadSpecialTechniques(effect, techniqueExt);

            techniques[(int)InGame.RenderEffect.Normal] = FindTechnique(effect, "NonTexturedColorPass", techniqueExt);
            techniques[(int)InGame.RenderEffect.Normal + 1] = FindTechnique(effect, "TexturedColorPass", techniqueExt);
        }

        /// <summary>
        /// Allocate the technique list
        /// </summary>
        /// <param name="num"></param>
        protected void ReserveTechniques(int num)
        {
            if (num > 0)
                techniques = new EffectTechnique[num];
            else
                techniques = null;
        }
        /// <summary>
        /// Load an explicitly named technique into a specific slot.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="idx"></param>
        /// <param name="technique"></param>
        protected void LoadTechnique(Effect effect, int idx, string technique)
        {
            techniques[idx] = effect.Techniques[technique];
        }
        /// <summary>
        /// Load an explicitly named technique + extension into a specific slot.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="idx"></param>
        /// <param name="technique"></param>
        protected void LoadTechnique(Effect effect, int idx, string technique, string ext)
        {
            techniques[idx] = FindTechnique(effect, technique, ext);
        }
        #endregion INTERNAL
    }
}
