using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Programming;
using Boku.SimWorld.Terra;
using Boku.UI;

namespace Boku.Programming
{
    public delegate void ReflexUseEvent();


    /// <summary>
    /// Describes a reflex as a set of upids.
    /// </summary>
    public class ReflexDesc
    {
        [XmlArray]
        [XmlArrayItem(Type = typeof(string), ElementName = "upid")]
        public string[] upids;

        public ReflexDesc()
        {
        }

        public ReflexDesc(string[] arr)
        {
            upids = arr;
        }
    }

    /// <summary>
    /// Class containing the data needed for the Micro Bit Pattern tile.
    /// LEDs is an array of booleans, one for each LED in the grid.
    /// Brightness is the brightness of all the on LEDs in the pattern.
    /// Duration is the time in seconds to show this pattern.
    /// </summary>
    public class MicroBitPattern : ICloneable
    {
        [XmlArray]
        public bool[] LEDs;
        [XmlAttribute]
        public int Brightness;
        [XmlAttribute]
        public float Duration;

        public MicroBitPattern()
        {
            LEDs = new bool[25];
            Brightness = 255;
            Duration = 1.0f;
        }

        public object Clone()
        {
            MicroBitPattern other = new MicroBitPattern();
            other.LEDs = new bool[25];
            for (int i = 0; i < 25; i++ )
            {
                other.LEDs[i] = LEDs[i];
            }
            other.Brightness = Brightness;
            other.Duration = Duration;
            return other;
        }
    }

    /// <summary>
    /// Represents a program row without bindings to an actor or a task.
    /// Used for things like row copy/paste.
    public class ReflexData
    {
        [XmlIgnore]
        public Sensor Sensor;
        [XmlIgnore]
        public Selector Selector;
        [XmlIgnore]
        public Actuator Actuator;
        [XmlIgnore]
        public List<Filter> Filters = new List<Filter>();
        [XmlIgnore]
        public List<Modifier> Modifiers = new List<Modifier>();
        [XmlAttribute]
        public int MaterialType = TerrainMaterial.EmptyMatIdx;
        [XmlAttribute]
        public int WaterType = -1;
        [XmlAttribute]
        public int Indentation = 0;


        // Can the following object and world parameters be put into just one or two values?
        // Only one at a time is ever used so we really don't need them all but I'm afraid
        // that it would be a back-compat nightmare to switch over.
        //
        // We can't break back compat but for the future we'll use the following generic
        // variables to the the parameter values for Settings tiles.

        // Generic parameters
        [XmlAttribute]
        public int ParamInt = 0;

        [XmlAttribute]
        public float ParamFloat = 0;

        [XmlAttribute]
        public string ParamString = null;

        // Object parameters
        [XmlAttribute]
        public bool ReScaleEnabled = false;

        [XmlAttribute]
        public float ReScale = 1.0f;

        [XmlAttribute]
        public float HoldDistance = 1.0f;

        [XmlAttribute]
        public float MoveSpeedTileModifier = 1.0f;

        [XmlAttribute]
        public float TurnSpeedTileModifier = 1.0f;

        [XmlAttribute]
        public int MaxHitpoints = 50;


        // World parameters
        [XmlAttribute]
        public bool WorldLightChangeEnabled = false;

        [XmlAttribute]
        public int WorldLightChangeIndex = 0;

        [XmlAttribute]
        public bool WorldSkyChangeEnabled = false;

        [XmlAttribute]
        public int WorldSkyChangeIndex = 0;

        [XmlAttribute]
        public int SetWaterTypeIndex = -1;

        //
        private string _sayString;
        private List<string> _sayStrings = new List<string>();                      // Strings for "say" verb.

        public string sayString                                                     // Text associated with 'say' verb.
        {
            get { return _sayString; }
            set { _sayString = value; TextHelper.SplitMessage(value, 10000, SharedX.GetGameFont20, true, _sayStrings); }
        }    
        public int sayMode = 1;                                                     // Display mode.  0==fullscreen, 1==thought balloon sequential, 2==thought balloon random
        public TextHelper.Justification sayJustification = TextHelper.Justification.Left;
        [XmlIgnore]
        public int sayLine = 0;                                                     // For sequential display of multi-line thought balloon text.  Also used by random to   
                                                                                    // not show the same line twice in a row.

        /// <summary>
        /// Strings that 'say' verb uses.
        /// </summary>
        [XmlIgnore]
        public List<string> sayStrings
        {
            get { return _sayStrings; }
        }


        private string _saidString;
        private List<string> _saidStrings = new List<string>();                      // Strings for "said" filter.

        public string saidString                                                     // Text associated with 'said' filter.
        {
            get { return _saidString; }
            set 
            { 
                _saidString = value; 
                TextHelper.SplitMessage(value, 10000, SharedX.GetGameFont20, true, _saidStrings); 
                for(int i=0; i<_saidStrings.Count; i++)
                {
                    _saidStrings[i] = _saidStrings[i].Trim();
                }
            }
        }
        public int saidMode = 1;                                                    // Trigger on text said at beginning (0) or end (1) of thought balloon life time.
        public TextHelper.Justification saidJustification = TextHelper.Justification.Left;


        /// <summary>
        /// Strings that 'said' filter is listening for.
        /// </summary>
        [XmlIgnore]
        public List<string> saidStrings
        {
            get { return _saidStrings; }
        }

        /// <summary>
        /// If this reflex has modifiers with Micro Bit light patterns this list will contain them.
        /// </summary>
        [XmlArray]
        [XmlArrayItem(typeof(MicroBitPattern))]
        public List<MicroBitPattern> microbitPatterns;

        [XmlIgnore]
        public ModifierParams modifierParams = new ModifierParams();

        public string sensorUpid
        {
            get
            {
                if (Sensor != null && Sensor != CardSpace.Cards.NullSensor)
                {
                    return Sensor.upid;
                }
                return null;
            }
            set
            {
                Sensor = CardSpace.Cards.GetSensor(value);
            }
        }
        public string selectorUpid
        {
            get
            {
                if (Selector != null && Selector != CardSpace.Cards.NullSelector)
                {
                    return Selector.upid;
                }
                return null;
            }
            set
            {
                Selector = CardSpace.Cards.GetSelector(value);
            }
        }
        public string actuatorUpid
        {
            get
            {
                if (Actuator != null && Actuator != CardSpace.Cards.NullActuator)
                {
                    return Actuator.upid;
                }
                return null;
            }
            set
            {
                Actuator = CardSpace.Cards.GetActuator(value);
            }
        }
        [XmlArray]
        [XmlArrayItem(Type = typeof(string), ElementName = "upid")]
        public string[] filterUpids
        {
            get
            {
                List<string> result = new List<string>();
                for (int i = 0; i < Filters.Count; ++i)
                {
                    if (!(Filters[i] is NullFilter))
                        result.Add(Filters[i].upid);
                }
                return result.ToArray();
            }
            set
            {
                Filters.Clear();
                for (int i = 0; i < value.Length; ++i)
                {
                    Filter filter = CardSpace.Cards.GetFilter(value[i]);
                    if (filter != null && filter != CardSpace.Cards.NullFilter)
                        Filters.Add(filter);
                }
            }
        }
        [XmlArray]
        [XmlArrayItem(Type = typeof(string), ElementName = "upid")]
        public string[] modifierUpids
        {
            get
            {
                List<string> result = new List<string>();
                for (int i = 0; i < Modifiers.Count; ++i)
                {
                    if (!(Modifiers[i] is NullModifier))
                        result.Add(Modifiers[i].upid);
                }
                return result.ToArray();
            }
            set
            {
                Modifiers.Clear();
                for (int i = 0; i < value.Length; ++i)
                {
                    Modifier modifier = CardSpace.Cards.GetModifier(value[i]);
                    if (modifier != null && modifier != CardSpace.Cards.NullModifier)
                        Modifiers.Add(modifier);
                }
            }
        }

        public ReflexData()
        {
            //sayString = Strings.Localize("programming.defaultSayString");
            sayString = "";
        }

        public ReflexData(int noInit)
        {
        }

        public ReflexData(ReflexDesc desc)
            : this()
        {
            FromDesc(desc);
        }

        public void MakeValid()
        {
            if (Sensor == null || Sensor == CardSpace.Cards.NullSensor)
            {
                Filters.Clear();
                Sensor = null;
            }

            if (Actuator == null || Actuator == CardSpace.Cards.NullActuator)
            {
                Modifiers.Clear();
                Selector = null;
                Actuator = null;
            }

            if (Selector == null || Selector == CardSpace.Cards.NullSelector)
            {
                Selector = null;
            }

            MakeValid_Scratch.Clear();
            for (int i = 0; i < Filters.Count; ++i)
            {
                MakeValid_Scratch.Add(Filters[i]);
            }

            Filters.Clear();
            for (int i = 0; i < MakeValid_Scratch.Count; ++i)
            {
                Filter filter = MakeValid_Scratch[i] as Filter;
                if (filter.ReflexCompatible(actor:null, reflex:this, replacedElement:null, allowArchivedCategories:true))
                {
                    Filters.Add(filter);
                }
                else
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditObject && 
                        InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.MouseEdit &&
                        InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.TouchEdit)
                    {
                        Debug.Assert(false, "Tile was deemed not compatible.  Figure out why.");
                    }
                }
            }

            MakeValid_Scratch.Clear();
            for (int i = 0; i < Modifiers.Count; ++i)
            {
                MakeValid_Scratch.Add(Modifiers[i]);
            }

            Modifiers.Clear();
            for (int i = 0; i < MakeValid_Scratch.Count; ++i)
            {
                Modifier modifier = MakeValid_Scratch[i] as Modifier;
                if (modifier.ReflexCompatible(actor:null, reflex:this, replacedElement:null, allowArchivedCategories:true))
                {
                    Modifiers.Add(modifier);
                }
                else
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditObject && InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.MouseEdit)
                    {
                        Debug.Assert(false, "Tile was deemed not compatible.  Figure out why.");
                    }
                }
            }

            if (Sensor != null)
            {
                Sensor sensor = Sensor;
                Sensor = null;
                if (sensor.ReflexCompatible(actor: null, reflex: this, replacedElement: null, allowArchivedCategories: true))
                {
                    Sensor = sensor;
                }
                else
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditObject && InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.MouseEdit)
                    {
                        Debug.Assert(false, "Tile was deemed not compatible.  Figure out why.");
                    }
                }

            }
            if (Selector != null)
            {
                Selector selector = Selector;
                Selector = null;
                if (selector.ReflexCompatible(actor: null, reflex: this, replacedElement: null, allowArchivedCategories: true))
                {
                    Selector = selector;
                }
                else
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditObject && InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.MouseEdit)
                    {
                        Debug.Assert(false, "Tile was deemed not compatible.  Figure out why.");
                    }
                }
            }
            if (Actuator != null)
            {
                Actuator actuator = Actuator;
                Actuator = null;
                if (actuator.ReflexCompatible(actor: null, reflex: this, replacedElement: null, allowArchivedCategories: true))
                {
                    Actuator = actuator;
                }
                else
                {
                    if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditObject && InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.MouseEdit)
                    {
                        Debug.Assert(false, "Tile was deemed not compatible.  Figure out why.");
                    }
                }
            }
        }   // end of MakeValid()

        List<ProgrammingElement> MakeValid_Scratch = new List<ProgrammingElement>();

        public void Clear()
        {
            Sensor = null;
            Selector = null;
            Actuator = null;
            Filters.Clear();
            Modifiers.Clear();

            sayString = null;
            sayLine = 0;
            sayMode = 1;
            saidString = null;
            saidMode = 0;
        }

        /// <summary>
        /// Returns true if the ReflexData is empty.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            bool result = Sensor == null &&
                            Selector == null &&
                            Actuator == null &&
                            Filters.Count == 0 &&
                            Modifiers.Count == 0;

            return result;
        }

        public void FromDesc(ReflexDesc desc)
        {
            Clear();

            for (int i = 0; i < desc.upids.Length; ++i)
            {
                string str = desc.upids[i];

                if (str.StartsWith(Atom.upidSensor))
                {
                    Sensor = CardSpace.Cards.GetSensor(str);
                }
                else if (str.StartsWith(Atom.upidSelector))
                {
                    Selector = CardSpace.Cards.GetSelector(str);
                }
                else if (str.StartsWith(Atom.upidActuator))
                {
                    Actuator = CardSpace.Cards.GetActuator(str);
                }
                else if (str.StartsWith(Atom.upidFilter))
                {
                    Filters.Add(CardSpace.Cards.GetFilter(str));
                }
                else if (str.StartsWith(Atom.upidModifier))
                {
                    Modifiers.Add(CardSpace.Cards.GetModifier(str));
                }
            }

            MakeValid();
        }

        public ReflexDesc ToDesc()
        {
            ReflexDesc desc = new ReflexDesc();

            List<string> upids = new List<string>();

            if (Sensor != null)
                upids.Add(sensorUpid);
            foreach (string filterUpid in filterUpids)
                upids.Add(filterUpid);
            if (Actuator != null)
                upids.Add(actuatorUpid);
            if (Selector != null)
                upids.Add(selectorUpid);
            foreach (string modifierUpid in modifierUpids)
                upids.Add(modifierUpid);

            desc.upids = upids.ToArray();

            return desc;
        }

        public bool ActorCompatible(GameActor actor)
        {
            if (actor == null)
                return true;
            if (Sensor != null && !Sensor.ActorCompatible(actor))
                return false;
            if (Selector != null && !Selector.ActorCompatible(actor))
                return false;
            if (Actuator != null && !Actuator.ActorCompatible(actor))
                return false;
            for (int i = 0; i < Filters.Count; ++i)
                if (!Filters[i].ActorCompatible(actor))
                    return false;
            for (int i = 0; i < Modifiers.Count; ++i)
                if (!Modifiers[i].ActorCompatible(actor))
                    return false;
            return true;
        }

        public bool HasModifier(string upid)
        {
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.upid == upid)
                    return true;
            }
            return false;
        }

        public T GetModifier<T>(string upid) where T : Modifier
        {
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.upid == upid)
                {
                    return modifier as T;
                }
            }
            return default(T);
        }

        public int GetModifierCount(string upid)
        {
            int count = 0;
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.upid == upid)
                    count += 1;
            }
            return count;
        }

        /// <summary>
        /// Returns the index of the specified modifier.
        /// Returns -1 if not found.
        /// </summary>
        /// <param name="upid"></param>
        /// <returns></returns>
        public int GetModifierIndex(string upid)
        {
            int index = -1;
            for (int i = 0; i < Modifiers.Count; i++)
            {
                if (Modifiers[i].upid == upid)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public Modifier GetModifierByType(Type modType)
        {
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.GetType() == modType)
                    return modifier;
            }
            return null;
        }

        List<Modifier> GetModifiersByType_list = new List<Modifier>();
        public List<Modifier> GetModifiersByType(Type modType)
        {
            GetModifiersByType_list.Clear();
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.GetType() == modType)
                    GetModifiersByType_list.Add(modifier);
            }
            return GetModifiersByType_list;
        }

        public int GetModifierCountByType(Type modType)
        {
            int count = 0;
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier.GetType() == modType)
                    count += 1;
            }
            return count;
        }

        /// <summary>
        /// Is there a filter of this type in this reflex?
        /// </summary>
        /// <param name="upid"></param>
        /// <returns></returns>
        public bool FilterExists(string upid)
        {
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].upid == upid)
                {
                    return true;
                }
            }
            return false;
        }

        public int GetFilterCount(string upid)
        {
            int count = 0;
            foreach (Filter filter in Filters)
            {
                if (filter.upid == upid)
                    count += 1;
            }
            return count;
        }

        public int GetFilterCountByType(Type filType)
        {
            int count = 0;
            foreach (Filter filter in Filters)
            {
                if (filter.GetType() == filType)
                    count += 1;
            }
            return count;
        }

        public Filter GetFilterByType(Type type)
        {
            foreach (Filter filter in Filters)
            {
                if (filter.GetType() == type)
                    return filter;
            }
            return null;
        }

        public int GetNonHiddenDefaultFilterCount()
        {
            int result = 0;
            foreach (Filter filter in Filters)
            {
                if (!filter.hiddenDefault)
                {
                    result++;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if the given upid exists anywhere in the reflex.
        /// </summary>
        /// <param name="upid"></param>
        /// <returns></returns>
        public bool HasTile(string upid)
        {
            // sensor
            if (sensorUpid == upid)
                return true;

            // filters
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].upid == upid)
                {
                    return true;
                }
            }

            // actuator
            if (actuatorUpid == upid)
                return true;

            // selector
            if (selectorUpid == upid)
                return true;

            // modifers
            for (int i = 0; i < Modifiers.Count; i++)
            {
                if (Modifiers[i].upid == upid)
                {
                    return true;
                }
            }

            return false;
        }   // end of HasTile()

        public static ReflexData DeepCopy(ReflexData srcData)
        {
            ReflexData dstData = new ReflexData(0);
            
            dstData.sensorUpid = srcData.sensorUpid;
            dstData.actuatorUpid = srcData.actuatorUpid;
            dstData.selectorUpid = srcData.selectorUpid;
            dstData.filterUpids = srcData.filterUpids;
            dstData.modifierUpids = srcData.modifierUpids;

            dstData.ParamInt = srcData.ParamInt;
            dstData.ParamFloat = srcData.ParamFloat;

            dstData.MaterialType = srcData.MaterialType;
            dstData.WaterType = srcData.WaterType;
            dstData.sayMode = srcData.sayMode;
            dstData.sayJustification = srcData.sayJustification;
            dstData._sayString = srcData._sayString;
            dstData._sayStrings = new List<string>(srcData._sayStrings);
            dstData.saidMode = srcData.saidMode;
            dstData.saidJustification = srcData.saidJustification;
            dstData._saidString = srcData._saidString;
            dstData._saidStrings = new List<string>(srcData._saidStrings);
            dstData.Indentation = srcData.Indentation;
            dstData.ReScaleEnabled = srcData.ReScaleEnabled;
            dstData.ReScale = srcData.ReScale;
            dstData.HoldDistance = srcData.HoldDistance;
            dstData.MaxHitpoints = srcData.MaxHitpoints;
            dstData.MoveSpeedTileModifier = srcData.MoveSpeedTileModifier;
            dstData.TurnSpeedTileModifier = srcData.TurnSpeedTileModifier;
            dstData.WorldLightChangeEnabled = srcData.WorldLightChangeEnabled;
            dstData.WorldLightChangeIndex = srcData.WorldLightChangeIndex;
            dstData.WorldSkyChangeEnabled = srcData.WorldSkyChangeEnabled;
            dstData.WorldSkyChangeIndex = srcData.WorldSkyChangeIndex;
            dstData.SetWaterTypeIndex = srcData.SetWaterTypeIndex;

            if (srcData.microbitPatterns == null)
            {
                dstData.microbitPatterns = null;
            }
            else
            {
                if (dstData.microbitPatterns == null)
                {
                    dstData.microbitPatterns = new List<MicroBitPattern>();
                    foreach (MicroBitPattern pattern in srcData.microbitPatterns)
                    {
                        dstData.microbitPatterns.Add(pattern.Clone() as MicroBitPattern);
                    }
                }
            }

            return dstData;
        }   // end of DeepCopy()

        public bool IsMovement()
        {
            if (!(Actuator is MovementActuator))
                return false;

            MovementActuator moveAct = Actuator as MovementActuator;

            return true;
        }

        public bool IsShoot()
        {
            if (!(Actuator is VerbActuator))
                return false;

            VerbActuator verbAct = Actuator as VerbActuator;

            return (verbAct.Verb == GameThing.Verbs.Shoot || verbAct.Verb == GameThing.Verbs.Shoot2 || verbAct.Verb == GameThing.Verbs.Launch);
        }

        internal void CompactModifiers()
        {
            for (int i = 0; i < Modifiers.Count; ++i)
            {
                if (Modifiers[i] == null)
                {
                    Modifiers.RemoveAt(i);
                    i -= 1;
                }
            }
        }
    }


    /// <summary>
    /// This represents a single part of many that make up a task to accomplish
    /// It is called a reflex as it represents a sub-part of a greater behavior that the task represents
    /// All reflexes are considered to be run in parrallel
    /// </summary>
    public class Reflex : ArbitraryComparable
    {

#if DEBUG
        /// <summary>
        /// Makes finding/reading reflexes in the debugger easier.
        /// </summary>
        [XmlIgnore]
        public string AAADebugString
        {
            get
            {
                string name = null;

                name = Indentation.ToString() + " : ";

                if (data.Sensor != null)
                    name += data.Sensor.label + " ";
                else
                    name += "always ";

                for (int i = 0; i < data.Filters.Count; i++)
                {
                    name += data.Filters[i].label + " ";
                }

                if (data.Actuator != null)
                    name += data.Actuator.label + " ";

                if (data.Selector != null)
                    name += data.Selector.label + " ";

                for (int i = 0; i < data.Modifiers.Count; i++)
                {
                    name += data.Modifiers[i].label + " ";
                }

                name += UniqueNum.ToString();

                return name;
            }
        }
#endif


        private ReflexData data = new ReflexData();

        [XmlIgnore]
        public bool leftMouseButtonPresent;

        [XmlIgnore]
        public bool rightMouseButtonPresent;

        [XmlIgnore]
        public ReflexData Data
        {
            set { data = value; }
            get { return data; }
        }

        public string sensorUpid
        {
            get { return data.sensorUpid; }
            set { data.sensorUpid = value; }
        }
        public string selectorUpid
        {
            get { return data.selectorUpid; }
            set { data.selectorUpid = value; }
        }
        public string actuatorUpid
        {
            get { return data.actuatorUpid; }
            set { data.actuatorUpid = value; }
        }
        public int ParamInt
        {
            get { return data.ParamInt; }
            set { data.ParamInt = value; }
        }
        public float ParamFloat
        {
            get { return data.ParamFloat; }
            set { data.ParamFloat = value; }
        }
        public int MaterialType
        {
            get { return data.MaterialType; }
            set { data.MaterialType = value; }
        }
        public int WaterType
        {
            get { return data.WaterType; }
            set { data.WaterType = value; }
        }
        public int Indentation
        {
            get { return data.Indentation; }
            set 
            {
                if (value != data.Indentation)
                {
                    data.Indentation = value;
                    // If we've changed the indentation level, we need to 
                    // call TaskFixup() which will be sure the parent reflexes
                    // are set correctly for any indented reflex.
                    TaskFixup();
                }
            }
        }
        public bool ReScaleEnabled
        {
            get { return data.ReScaleEnabled; }
            set { data.ReScaleEnabled = value; }
        }
        public float ReScale
        {
            get { return data.ReScale; }
            set { data.ReScale = value; }
        }
        public float HoldDistance
        {
            get { return data.HoldDistance; }
            set { data.HoldDistance = value; }
        }
        public int MaxHipoints
        {
            get { return data.MaxHitpoints; }
            set { data.MaxHitpoints = value; }
        }
        public float MoveSpeedTileModifier
        {
            get { return data.MoveSpeedTileModifier; }
            set { data.MoveSpeedTileModifier = value; }
        }
        public float TurnSpeedTileModifier
        {
            get { return data.TurnSpeedTileModifier; }
            set { data.TurnSpeedTileModifier = value; }
        }
        public bool WorldLightChangeEnabled
        {
            get { return data.WorldLightChangeEnabled; }
            set { data.WorldLightChangeEnabled = value; }
        }

        public int WorldLightChangeIndex
        {
            get { return data.WorldLightChangeIndex; }
            set { data.WorldLightChangeIndex = value; }
        }
        public bool WorldSkyChangeEnabled
        {
            get { return data.WorldSkyChangeEnabled; }
            set { data.WorldSkyChangeEnabled = value; }
        }

        public int WorldSkyChangeIndex
        {
            get { return data.WorldSkyChangeIndex; }
            set { data.WorldSkyChangeIndex = value; }
        }

        public int SetWaterTypeIndex
        {
            get { return data.SetWaterTypeIndex; }
            set { data.SetWaterTypeIndex = value; }
        }

        [XmlArray]
        [XmlArrayItem(Type = typeof(string), ElementName = "upid")]
        public string[] filterUpids
        {
            get { return data.filterUpids; }
            set { data.filterUpids = value; }
        }
        [XmlArray]
        [XmlArrayItem(Type = typeof(string), ElementName = "upid")]
        public string[] modifierUpids
        {
            get { return data.modifierUpids; }
            set { data.modifierUpids = value; }
        }

        public string SayString
        {
            get { return data.sayString; }
            set { data.sayString = value; }
        }

        public int SayMode
        {
            get { return data.sayMode; }
            set { data.sayMode = value; }
        }

        public TextHelper.Justification SayJustification
        {
            get { return data.sayJustification; }
            set { data.sayJustification = value; }
        }

        public string SaidString
        {
            get { return data.saidString; }
            set { data.saidString = value; }
        }

        public int SaidMode
        {
            get { return data.saidMode; }
            set { data.saidMode = value; }
        }

        public TextHelper.Justification SaidJustification
        {
            get { return data.saidJustification; }
            set { data.saidJustification = value; }
        }

        [XmlIgnore]
        public List<string> SayStrings
        {
            get { return data.sayStrings; }
        }

        [XmlIgnore]
        public List<string> SaidStrings
        {
            get { return data.saidStrings; }
        }

        [XmlArray]
        [XmlArrayItem(typeof(MicroBitPattern))]
        public List<MicroBitPattern> MicrobitPatterns
        {
            get { return data.microbitPatterns; }
            set { data.microbitPatterns = value; }
        }

        protected Sensor sensor
        {
            get { return data.Sensor; }
            set { data.Sensor = value; }
        }

        protected Selector selector
        {
            get { return data.Selector; }
            set { data.Selector = value; }
        }

        protected Actuator actuator
        {
            get { return data.Actuator; }
            set { data.Actuator = value; }
        }

        protected List<Filter> rawfilters
        {
            get { return data.Filters; }
            set { data.Filters = value; RebuildProcessFilters();  }
        }

        protected List<Modifier> modifiers
        {
            get { return data.Modifiers; }
            set { data.Modifiers = value; }
        }

        protected List<Filter> processFilters = new List<Filter>();

        protected Selector selectorActive;
        protected Selector selectorHidden;

        protected Task task;


        [XmlIgnore]
        public SensorTargetSet targetSet = new SensorTargetSet();

        [XmlIgnore]
        public bool actedOn;    // set to true if any effector generated by this reflex was acted on.

        [XmlIgnore]
        public bool hasMeFilter;    // Does this reflex have a "me" filter?

        [XmlIgnore]
        public bool hasDeadFilter;      // Does this reflex have a "dead" filter?

        [XmlIgnore]
        public bool hasSquashedFilter;  // Does this reflex have a "squashed" filter?

        [XmlIgnore]
        public bool hasMissileFilter;   // Does this reflex have a "missile" filter?

        [XmlIgnore]
        public bool hasGUIButtonFilter;   // Does this reflex have a "GUI button" filter?

        [XmlIgnore]
        public bool hasClassificationFitler;    // Does this reflex have a classification filter?  (is is looking for an actor)

        [XmlIgnore]
        protected Vector3? mousePosition = null;       // Mouse position in world coords.

        [XmlIgnore]
        protected GameActor mouseActor = null;         // Actor clicked on my mouse.

        [XmlIgnore]
        protected Vector3? touchPosition = null;       // Touch position in world coords.

        [XmlIgnore]
        protected GameActor touchActor = null;         // Actor touched by a gesture.

        /// <summary>
        /// Mouse position in world coords.  Note that this
        /// is nullable so will be null if not valid.
        /// </summary>
        [XmlIgnore]
        public Vector3? MousePosition
        {
            get { return mousePosition; }
            set { mousePosition = value; }
        }

        /// <summary>
        /// Actor clicked on by mouse.
        /// </summary>
        [XmlIgnore]
        public GameActor MouseActor
        {
            get { return mouseActor; }
            set { mouseActor = value; }
        }

        /// <summary>
        /// Touch position in world coords.  Note that this
        /// is nullable so will be null if not valid.
        /// </summary>
        [XmlIgnore]
        public Vector3? TouchPosition
        {
            get { return touchPosition; }
            set { touchPosition = value; }
        }

        /// <summary>
        /// Actor touched by a gesture.
        /// </summary>
        [XmlIgnore]
        public GameActor TouchActor
        {
            get { return touchActor; }
            set { touchActor = value; }
        }

        public Reflex()
        {
        }

        public Reflex(Task parent)
        {
            this.task = parent;
        }

        [XmlIgnore]
        public Task Task
        {
            get { return this.task; }
            set { this.task = value; }
        }

        /// <summary>
        /// Parent used for indented reflexes.
        /// </summary>
        [XmlIgnore]
        public Reflex Parent;


        public ReflexData Copy()
        {
            ReflexData clip = new ReflexData();

            if (sensor != CardSpace.Cards.NullSensor)
                clip.Sensor = sensor;
            else
                clip.Sensor = null;

            if (actuator != CardSpace.Cards.NullActuator)
                clip.Actuator = actuator;
            else
                clip.Actuator = null;

            if (selector != CardSpace.Cards.NullSelector)
                clip.Selector = selector;
            else
                clip.Selector = null;

            for (int i = 0; i < rawfilters.Count; ++i)
            {
                Filter filter = rawfilters[i];
                if (filter != null && filter != CardSpace.Cards.NullFilter)
                    clip.Filters.Add(filter);
            }
            for (int i = 0; i < modifiers.Count; ++i)
            {
                Modifier modifier = modifiers[i];
                if (modifier != null && modifier != CardSpace.Cards.NullModifier)
                    clip.Modifiers.Add(modifier);
            }
            
            clip.MaterialType = MaterialType;
            clip.WaterType = WaterType;

            clip.sayString = SayString;
            clip.sayMode = SayMode;
            clip.saidString = SaidString;
            clip.saidMode = SaidMode;
            clip.Indentation = Indentation;

            clip.ParamInt = Data.ParamInt;
            clip.ParamFloat = Data.ParamFloat;

            clip.ReScaleEnabled = Data.ReScaleEnabled;
            clip.ReScale = Data.ReScale;
            clip.HoldDistance = Data.HoldDistance;
            clip.MaxHitpoints = Data.MaxHitpoints;
            clip.MoveSpeedTileModifier = Data.MoveSpeedTileModifier;
            clip.TurnSpeedTileModifier = Data.TurnSpeedTileModifier;
            clip.WorldLightChangeEnabled = Data.WorldLightChangeEnabled;
            clip.WorldLightChangeIndex = Data.WorldLightChangeIndex;
            clip.WorldSkyChangeEnabled = Data.WorldSkyChangeEnabled;
            clip.WorldSkyChangeIndex = Data.WorldSkyChangeIndex;
            clip.SetWaterTypeIndex = Data.SetWaterTypeIndex;

            if (Data.microbitPatterns == null)
            {
                clip.microbitPatterns = null;
            }
            else
            {
                if (clip.microbitPatterns == null)
                {
                    clip.microbitPatterns = new List<MicroBitPattern>();
                    foreach (MicroBitPattern pattern in Data.microbitPatterns)
                    {
                        clip.microbitPatterns.Add(pattern.Clone() as MicroBitPattern);
                    }
                }
            }

            return clip;
        }

        public void Paste(ReflexData clip)
        {
            if (clip.Sensor != null && clip.Sensor.ActorCompatible(task.Brain.GameActor))
            {
                data.Sensor = clip.Sensor.Clone() as Sensor;
            }
            else
            {
                data.Sensor = null;
            }

            if (clip.Actuator != null && clip.Actuator.ActorCompatible(task.Brain.GameActor))
            {
                // Need to clone theActuator, not copy it.  This is because some actuators contain
                // state which changes during reflex evaluation.  The particular bug in question was
                // 2 reflexes both setting score values with a once tile.
                //  WHEN DO SetScore Red Random 5Points Once
                //  WHEN DO SetScore Green Random 5Points Once
                // If the second line was created via copy/paste then they would share an actuator
                // which caused the second line to act like the Once tile isn't there.
                //
                // I'm not sure if it's needed but I'm going to update the other tiles to do the same.
                data.Actuator = clip.Actuator.Clone() as Actuator;
            }
            else
            {
                data.Actuator = null;
            }

            if (clip.Selector != null && clip.Selector.ActorCompatible(task.Brain.GameActor))
            {
                data.Selector = clip.Selector.Clone() as Selector;
            }
            else
            {
                data.Selector = null;
            }

            data.Filters.Clear();
            for (int i = 0; i < clip.Filters.Count; ++i)
            {
                //this doesn't fix it, but at least it keeps it from crashing
                //FIXME: figure out where this null reference is being set/added and fix it there
                //Steps to repro: Add a few filters and then hit delete when on the plus (not on a specific tile)
                if (clip.Filters[i] == null)
                {
                    //remove the bad entry and continue on...
                    clip.Filters.RemoveAt(i--);
                    continue;
                }

                if (clip.Filters[i].ActorCompatible(task.Brain.GameActor))
                {
                    data.Filters.Add(clip.Filters[i].Clone() as Filter);
                }
            }

            data.Modifiers.Clear();
            clip.CompactModifiers();
            for (int i = 0; i < clip.Modifiers.Count; ++i)
            {
                if (clip.Modifiers[i].ActorCompatible(task.Brain.GameActor))
                {
                    // If we're inlining, we need to verify that the page # we're pasting is valid.
                    // If it's not, just skip over it.
                    // TODO (****) With this here, is the test in ReflexCard redundant?
                    if (data.actuatorUpid == "actuator.inlinetask")
                    {
                        Brain brain = Task.Brain;
                        int curTask = brain.ActiveTaskId;
                        TaskModifier tm = clip.Modifiers[i] as TaskModifier;
                        if(tm != null)
                        {
                            int targetTask = (int)tm.taskid;

                            // Start with array of false for each task.
                            bool[] touched = new bool[Brain.kCountDefaultTasks];
                            for (int t = 0; t < Brain.kCountDefaultTasks; t++)
                            {
                                touched[t] = false;
                            }

                            if (!ReflexCard.IsValidInline(brain, curTask, targetTask, touched))
                            {
                                // Not valid so lets skip it.
                                continue;
                            }
                        }
                    }
                    data.Modifiers.Add(clip.Modifiers[i].Clone() as Modifier);
                }
            }

            data.ParamInt = clip.ParamInt;
            data.ParamFloat = clip.ParamFloat;

            data.MaterialType = clip.MaterialType;
            data.WaterType = clip.WaterType;

            // Copy info related to "say" verb.
            data.sayLine = clip.sayLine;
            data.sayMode = clip.sayMode;
            data.sayString = clip.sayString;
            data.saidMode = clip.saidMode;
            data.saidString = clip.saidString;
            data.Indentation = clip.Indentation;
            data.ReScaleEnabled = clip.ReScaleEnabled;
            data.ReScale = clip.ReScale;
            data.HoldDistance = clip.HoldDistance;
            data.MaxHitpoints = clip.MaxHitpoints;
            data.MoveSpeedTileModifier = clip.MoveSpeedTileModifier;
            data.TurnSpeedTileModifier = clip.TurnSpeedTileModifier;

            data.WorldLightChangeEnabled = clip.WorldLightChangeEnabled;
            data.WorldLightChangeIndex = clip.WorldLightChangeIndex;
            data.WorldSkyChangeEnabled = clip.WorldSkyChangeEnabled;
            data.WorldSkyChangeIndex = clip.WorldSkyChangeIndex;
            data.SetWaterTypeIndex = clip.SetWaterTypeIndex;
            
            // Deep copy needed since we may paste multiple times.
            // Could probably remove the deep copy for the Copy command.
            if (clip.microbitPatterns == null)
            {
                data.microbitPatterns = null;
            }
            else
            {
                data.microbitPatterns = new List<MicroBitPattern>();
                foreach (MicroBitPattern pattern in clip.microbitPatterns)
                {
                    data.microbitPatterns.Add(pattern.Clone() as MicroBitPattern);
                }
            }
            
            RebuildProcessFilters();
            TaskFixup();
        }

        /// <summary>
        /// Replace one tile with another.
        /// </summary>
        /// <param name="find">The exisitng tile.</param>
        /// <param name="replace">The replacement tile.</param>
        /// <param name="cardType"></param>
        /// <param name="cardIndex">Index of tile.  Needed since there may be mnore than one instance of some tiles.</param>
        public void Replace(ProgrammingElement find, ProgrammingElement replace, CardSpace.CardType cardType, int cardIndex)
        {
            ReflexData clip = this.Copy();

            if ((cardType & CardSpace.CardType.Sensor) == CardSpace.CardType.Sensor)
            {
                if (replace != null)
                {
                    clip.Sensor = replace as Sensor;
                }
                else
                {
                    clip.Sensor = null;
                }
            }

            if ((cardType & CardSpace.CardType.Selector) == CardSpace.CardType.Selector)
            {
                if (replace is Selector)
                {
                    clip.Selector = replace as Selector;
                    cardType = CardSpace.CardType.Selector;
                }
                else if (replace is Modifier)
                {
                    cardType = CardSpace.CardType.Modifier;
                }
                else
                {
                    clip.Selector = null;
                }
            }

            if ((cardType & CardSpace.CardType.Actuator) == CardSpace.CardType.Actuator)
            {
                if (replace is Actuator)
                {
                    clip.Actuator = replace as Actuator;
                }
                else
                {
                    clip.Actuator = null;
                }
            }

            if ((cardType & CardSpace.CardType.Filter) == CardSpace.CardType.Filter)
            {
                int indexFilter = cardIndex - 2;

                if (find == null || find is NullFilter)
                {
                    // Adding a new filter.
                    clip.Filters.Add(replace as Filter);
                }
                else if (replace == null)
                {
                    // Deleting a filter.
                    clip.Filters.RemoveAt(indexFilter);
                }
                else
                {
                    // Must be replacing
                    clip.Filters[indexFilter] = replace as Filter;
                }
            }

            if ((cardType & CardSpace.CardType.Modifier) == CardSpace.CardType.Modifier)
            {
                cardType = CardSpace.CardType.Modifier;

                int indexModifier = clip.Modifiers.IndexOf(find as Modifier);
                if (indexModifier >= 0)
                {
                    if (clip.Modifiers[indexModifier] != replace)
                    {
                        if (replace != null)
                        {
                            clip.Modifiers[indexModifier] = replace as Modifier;
                        }
                        else
                        {
                            clip.Modifiers.RemoveAt(indexModifier);
                        }
                    }
                }
                else
                {
                    // not found by exact match, check for upid match
                    bool found = false;
                    for (indexModifier = 0; indexModifier < this.modifiers.Count; indexModifier++)
                    {
                        Modifier modifier = clip.Modifiers[indexModifier] as Modifier;
                        if (modifier.upid == find.upid)
                        {
                            if (replace != null)
                            {
                                clip.Modifiers[indexModifier] = replace as Modifier;
                                found = true;
                            }
                            else
                            {
                                clip.Modifiers.RemoveAt(indexModifier);
                            }
                            break;
                        }
                    }
                    if (!found && replace != null)
                    {
                        clip.Modifiers.Add(replace as Modifier);
                    }
                }
            }

            this.Paste(clip);
        }

        /// <summary>
        /// This is called to have the reflex reset its specific componenents
        /// It should not reset the actuators as they are shared among all
        /// the reflexes of the owning task
        /// </summary>
        public void Reset()
        {
            targetSet.Clear();

            // Clear the mouse targets.
            MousePosition = null;
            MouseActor = null;

            // If this reflex has a mouseSensor tell MouseInput to 
            // ignore inputs until the buttons have been released
            // to prevent clicks from carrying over between tasks.
            if (Sensor is MouseSensor)
            {
                LowLevelMouseInput.Left.IgnoreUntilReleased = true;
                LowLevelMouseInput.Right.IgnoreUntilReleased = true;
            }

            // Check if we have a "me", "dead", "squashed", or "missile" filters.
            hasMeFilter = false;
            hasDeadFilter = false;
            hasMissileFilter = false;
            hasGUIButtonFilter = false;
            hasClassificationFitler = false;
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i] is MeFilter)
                {
                    hasMeFilter = true;
                    break;
                }
                else if (Filters[i] is DeadFilter)
                {
                    hasDeadFilter = true;
                    break;
                }
                else if (Filters[i] is SquashedFilter)
                {
                    hasSquashedFilter = true;
                    break;
                }
                else if (Filters[i] is GUIButtonFilter)
                {
                    hasGUIButtonFilter = true;
                    break;
                }
                else if (Filters[i] is ClassificationFilter)
                {
                    ClassificationFilter cf = Filters[i] as ClassificationFilter;
                    if (cf.classification.name == "missile")
                    {
                        hasMissileFilter = true;
                        break;
                    }
                    hasClassificationFitler = true;
                }
            }

            // This will throw out a lot of stuff and re-allocate it. At runtime, it
            // amounts to a lot of rebuilding the same thing. Would be nice if we
            // only did this during edit time, when there's a chance that the before
            // and after rebuild might be different.
            RebuildProcessFilters();

            if (sensor != null)
            {
                sensor.Reset(this);
            }

            if (actuator != null)
            {
                actuator.Reset(this);
            }

            for (int indexFilter = 0; indexFilter < this.rawfilters.Count; indexFilter++)
            {
                Filter filter = this.rawfilters[indexFilter] as Filter;
                filter.Reset(this);
            }
            for (int indexFilter = 0; indexFilter < this.processFilters.Count; indexFilter++)
            {
                Filter filter = this.processFilters[indexFilter] as Filter;
                filter.Reset(this);
            }

            if (this.selector != null)
            {
                this.selector.Reset(this);
            }
            else if (this.selectorActive != null)
            {
                this.selectorActive.Reset(this);
            }

            for (int indexModifier = 0; indexModifier < this.modifiers.Count; indexModifier++)
            {
                Modifier modifier = this.modifiers[indexModifier] as Modifier;
                modifier.Reset(this);
            }

            leftMouseButtonPresent = false;
            rightMouseButtonPresent = false;

            MouseFilter mouseButtonFilter = data.GetFilterByType(typeof(MouseFilter)) as MouseFilter;

            if (mouseButtonFilter != null)
            {
                switch (mouseButtonFilter.type)
                {
                    case MouseFilterType.LeftButton:
                        leftMouseButtonPresent = true;
                        break;

                    case MouseFilterType.RightButton:
                        rightMouseButtonPresent = true;
                        break;
                }
            }
        }

        public void Update(GameActor gameActor, int priority)
        {
            // movement reflexes are always considered acted on so that their movement
            // actuators will always update.
            actedOn = this.IsMovement;

            // Clear the mouse targets EXCEPT in the case of movement 
            // where we want the actor or position to persist.
            if (!IsMovement)
            {
                MouseActor = null;
                MousePosition = null;
            }

            targetSet.Clear();

            if (this.sensor != null)
            {
                this.sensor.ComposeSensorTargetSet(gameActor, this);
            }
            else
            {
                CardSpace.Cards.NullSensor.ComposeSensorTargetSet(gameActor, this);
            }

            CreateActionSet(gameActor, priority);
        }   // end of Update()

        public void CreateActionSet(GameActor gameActor, int priority)
        {
            ActionSet actionSet = null;
            if (targetSet.AnyAction && this.selectorActive != null && !(this.selectorActive is NullSelector))
            {
                actionSet = this.selectorActive.ComposeActionSet(this, gameActor);
                Debug.Assert(actionSet != null);
                actionSet.priority = priority;
            }
            if (actionSet != null && this.actuator != null)
            {
                this.actuator.AttachActionSet(actionSet);
            }
        }   // endof CreateActionSet()

        public void Used(bool newUse)
        {
            if (this.selectorActive != null)
            {
                this.selectorActive.Used(newUse);
            }
        }
        [XmlIgnore]
        public Sensor Sensor
        {
            get { return sensor; }
        }
        [XmlIgnore]
        public List<Filter> RawFilters
        {
            get { return rawfilters; }
        }
        [XmlIgnore]
        public List<Filter> Filters
        {
            get { return processFilters; }
        }
        [XmlIgnore]
        public Selector Selector
        {
            get { return selector; }
        }
        [XmlIgnore]
        public List<Modifier> Modifiers
        {
            get { return modifiers; }
        }
        [XmlIgnore]
        public Actuator Actuator
        {
            get { return actuator; }
        }

        [XmlIgnore]
        public ModifierParams ModifierParams
        {
            get { return data.modifierParams; }
        }
        
        protected void FillFilters(int FillFilterCount)
        {
            if (rawfilters.Count < FillFilterCount)
            {
                for (int iNullFilters = rawfilters.Count; iNullFilters < FillFilterCount; iNullFilters++)
                {
                    Filter filter = CardSpace.Cards.NullFilter;
                    rawfilters.Add(filter);
                }
            }
        }
        protected void FillModifiers(int FillModifierCount)
        {
            if (modifiers.Count < FillModifierCount)
            {
                for (int iNullModifiers = modifiers.Count; iNullModifiers < FillModifierCount; iNullModifiers++)
                {
                    Modifier modifier = CardSpace.Cards.NullModifier;
                    modifiers.Add(modifier);
                }
            }
        }
        protected void AlignFiltersLeft()
        {
            // remove nulls from filters first
            // they will get added back to the end
            for (int iFilter = 0; iFilter < rawfilters.Count; iFilter++)
            {
                Filter filter = rawfilters[iFilter] as Filter;
                if (filter is NullFilter)
                {
                    rawfilters.RemoveAt(iFilter);
                    iFilter--;
                }
            }
        }
        protected void AlignModifiersLeft()
        {
            // remove nulls from modifiers first
            // they will get added back to the end
            for (int iModifier = 0; iModifier < modifiers.Count; iModifier++)
            {
                Modifier modifier = modifiers[iModifier] as Modifier;
                if (modifier is NullModifier)
                {
                    modifiers.RemoveAt(iModifier);
                    iModifier--;
                }
            }
        }

        public void Fill(int FillFilterCount, int FillModifierCount)
        {
            /*
            // make sure spots are filled at least with nulls
            if (sensor == null)
            {
                sensor = new NullSensor();
            }
            FillFilters(FillFilterCount);
            if (selector == null)
            {
                selector = new NullSelector();
            }

            FillModifiers(FillModifierCount);

            if (actuator == null)
            {
                actuator = new NullActuator();
            }
             */
        }


        // Used as a local variable in several methods here.
        private static List<ProgrammingElement> _scratchSelectorPieces;
        private static List<ProgrammingElement> ScratchSelectorPieces
        {
            get { return _scratchSelectorPieces ?? (_scratchSelectorPieces = new List<ProgrammingElement>()); }
        }

        protected Selector FindCompatibleHiddenSelector()
        {
            if (Actuator == null)
                return null;

            CardSpace.Cards.Pieces(CardSpace.CardType.Selector, ScratchSelectorPieces);
            Selector compatibleSelector = null;

            if (Sensor != null && Sensor.IsUserControlled)
            {
                // User input provides a direction, so use a selector that will apply the supplied direction.
                compatibleSelector = CardSpace.Cards.GetSelector(Actuator.gamepadDefaultSelectorUpid);
            }
            else
            {
                data.modifierParams.Clear();

                for (int i = 0; i < Modifiers.Count; ++i)
                {
                    Modifiers[i].GatherParams(data.modifierParams);
                }

                if (data.modifierParams.HasDirection)
                {
                    // If a modifier provides a direction, so use a selector that will apply the supplied direction.
                    compatibleSelector = CardSpace.Cards.GetSelector(Actuator.gamepadDefaultSelectorUpid);
                }
                else
                {
                    // If no directional inputs are supplied, use a selector that will provide one.
                    compatibleSelector = CardSpace.Cards.GetSelector(Actuator.autoDefaultSelectorUpid);
                }
            }

            if (compatibleSelector != null)
            {
                if (compatibleSelector.ReflexCompatible(this, this.Selector, false))
                {
                    selectorHidden = compatibleSelector.Clone() as Selector;
                    return selectorHidden;
                }
                else
                {
                    compatibleSelector = null;
                }
            }

            // Search for a compatible selector for use as default.

            // Search the selectors and remember the first compatible selector but check if current hidden is in the set
            for (int indexSelector = 0; indexSelector < ScratchSelectorPieces.Count; indexSelector++)
            {
                Selector selector = ScratchSelectorPieces[indexSelector] as Selector;

                if (selector.ReflexCompatible(this, this.Selector, false))
                {
                    if (compatibleSelector == null)
                    {
                        compatibleSelector = selector;
                        if (this.selectorHidden == null)
                        {
                            break; // stop searching
                        }
                    }
                    if (this.selectorHidden != null)
                    {
                        if (this.selectorHidden.upid == selector.upid)
                        {
                            compatibleSelector = selector;
                            break; // stop searching
                        }
                    }
                }
            }

            if (compatibleSelector == null)
            {
                this.selectorHidden = null;
            }
            else if (this.selectorHidden != null && this.selectorHidden.upid == compatibleSelector.upid)
            {
                // no change, the selectorHidden is still valid
            }
            else
            {
                this.selectorHidden = CardSpace.Cards.GetSelector(compatibleSelector.upid) as Selector;
            }

            return this.selectorHidden;
        }

        /// <summary>
        /// WARNING This comment is a total guess but it's better than what the author provided.
        /// This function appears to clean up the reflex by replacing any elements which have a
        /// Null upid value with actual null values and then ensuring that the filter and 
        /// modifier lists are all nicely aligned.  It then returns a bool telling you if the 
        /// reflex is empty.  Why this is called "Chill" I have no clue.  Looking through SD logs
        /// it was added back when the row handles were first added.
        /// </summary>
        /// <returns></returns>
        public bool Chill()
        {
            if (sensor != null && sensor.upid == ProgrammingElement.upidNull)
            {
                sensor = null;
            }
            AlignFiltersLeft();
            if ((selector != null && selector is NullSelector) || selector == null)
            {
                this.selectorActive = FindCompatibleHiddenSelector();

            }
            else
            {
                this.selectorActive = selector;
            }

            AlignModifiersLeft();
            if (actuator != null && actuator.upid == ProgrammingElement.upidNull)
            {
                actuator = null;
            }
            return  (sensor == null &&
                    rawfilters.Count == 0 &&
                    selector == null && 
                    modifiers.Count == 0 &&
                    actuator == null);
        }

        /// <summary>
        /// Hack to transform an old-style color modifier into a newer one.
        /// </summary>
        /// <param name="m"></param>
        /// <returns>New modifier to replace existing one or existing one if no change needed.</returns>
        private Modifier ScorebucketHack(Modifier m)
        {
            ColorModifier cm = m as ColorModifier;

            if (cm != null)
            {
                int index = cm.upid.LastIndexOf('.');
                string color = cm.upid.Substring(index + 1);

                string upid = "modifier.scorebucket.color." + color;
                ScoreBucketModifier sbm = CardSpace.Cards.GetModifier(upid) as ScoreBucketModifier;

                Debug.Assert(sbm != null);

                return sbm;
            }

            return m;
        }   // end of ScorebucketHack()

        /// <summary>
        /// Hack to transform an old-style score filter into a newer one.
        /// </summary>
        /// <param name="m"></param>
        /// <returns>New filter to replace existing one or existing one if no change needed.</returns>
        private Filter ScorebucketHack(Filter f)
        {
            ClassificationFilter cf = f as ClassificationFilter;

            if (cf != null)
            {
                int index = cf.upid.LastIndexOf('.');
                string color = cf.upid.Substring(index + 1);

                // Filter names are always all lower case.
                color = color.ToLower();

                ScoreBucketFilter sbf = new ScoreBucketFilter();
                sbf.upid = "filter.scorebucket.color." + color;

                sbf.bucket = (ScoreBucket)cf.classification.color;

                return sbf;
            }

            return f;
        }   // end of ScorebucketHack()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="atLoadTime">If true this fixup is happening when the level is being loaded.  If false then it's happening during brain editing.</param>
        /// <param name="incomingLanguageVersion">If atLoadTime is true, this is the version of the language for the level being loaded.</param>
        public void Fixup(bool atLoadTime, int incomingLanguageVersion)
        {
            // Welcome to the land of the hacks...
            // Hack Hack Hack

            // HACK  At one point modifier.blue was able to be used as a modifier for score actions, ie resets.
            // Then it got changed to modifier.scorebucket.color.blue.  This has the advantage of having a 
            // more relevant tile icon.  The downside is that it totally breaks existing levels.  Since 
            // modifier.blue is still valid in other cases we can't just use the autoreplace functionality.
            // So, what's happening now is that in a reflex like Always Reset Score Blue the Blue modifier
            // is being deleted and the game is broken.  Time for a hack.  A vile hack.
            // Sample world that shows this is Kodu: Portal v10
            if (data.Actuator != null && data.Modifiers.Count >= 1)
            {
                // Check if this reflex is dealing with scores.  If so, it's 
                // a candidate to have its modifiers filtered.
                bool needToFilter = false;
                if(data.Actuator.upid == "actuator.score" || data.Actuator.upid == "actuator.unscore" ||
                   data.Actuator.upid == "actuator.scoreset" )
                {
                    needToFilter = true;
                }
                else if(data.Actuator.upid == "actuator.reset")
                {
                    for (int i = 0; i < data.Modifiers.Count; i++)
                    {
                        if(data.Modifiers[i].upid == "modifier.reset.score")
                        {
                            needToFilter = true;
                        }
                    }
                }

                if(needToFilter)
                {
                    for (int i = 0; i < data.Modifiers.Count; i++)
                    {
                        // Note we have to change things in both places since there's
                        // no auto sync between Reflex and ReflexData.
                        // TODO This should be cleaned up.
                        data.Modifiers[i] = ScorebucketHack(data.Modifiers[i]);
                        Modifiers[i] = data.Modifiers[i];
                    }
                }
            }

            // HACK  Same as above but on the WHEN side of the rule.  Previously we just
            // used the color filters rather than the scorebucket filters.
            if (data.sensorUpid == "sensor.scored")
            {
                // Convert any color filters to scorebucket filters.
                for (int i = 0; i < data.Filters.Count; i++)
                {
                    Filter filter = data.Filters[i];
                    if (filter is ClassificationFilter)
                    {
                        // Need to replace it with matching scorebucket filter.
                        data.Filters[i] = ScorebucketHack(filter);
                    }
                }
            }

            // HACK Previously we defaulted to the red scorebucket when no scorebucket was 
            // specified.  This is no longer deemed compatible so insert the red scorebucket
            // where needed.
            // We also allowed strange ordering of the tiles that conflicts with the new setup so rearrange tiles as needed.
            // Note this hack is only on the WHEN side.
            // Only do this at load time.  At run time it will interfere with editing.
            if (atLoadTime)
            {
                if (data.sensorUpid == "sensor.scored")
                {
                    // If no scorebucket, no points and no comparison then checking if red changed. Add red.
                    // If 1 scorebucket, no points and no comparison then checking if that bucket changed. Do nothing.
                    // If no scorebucket no comparison but does have points when comparing sum to red.  Add red and comp tiles.
                    // If 1 scorebucket and points then comparing that bucket to sum of points.  Add equals compare and put score on left hand side.
                    // If comparison and nothing else then comparing red to 0.  Add red and 0 tiles.
                    // If comparison and 1 scorebucket then we're comparing that bucket to the sum of the point tiles (which may be all missing so add 0).  Rearrange to bucket-comp-points.

                    //     Be careful when rearranging since we don't want to mess up ordering with Random.

                    // Find scorebucket filter, if any.  Note that SettingsFilter also can act as a ScoreBucket.
                    int scoreBucketIndex = -1;
                    for (int i = 0; i < data.Filters.Count; i++)
                    {
                        if (data.Filters[i] is ScoreBucketFilter || data.Filters[i] is SettingsFilter)
                        {
                            scoreBucketIndex = i;
                            break;
                        }
                    }

                    // Find comparison filter, if any.
                    int comparisonIndex = -1;
                    for (int i = 0; i < data.Filters.Count; i++)
                    {
                        if (data.Filters[i] is ScoreCompareFilter)
                        {
                            comparisonIndex = i;
                            break;
                        }
                    }

                    bool hasPointFilter = false;
                    for (int i = 0; i < data.Filters.Count; i++)
                    {
                        if (data.Filters[i] is ScoreFilter)
                        {
                            hasPointFilter = true;
                            break;
                        }
                    }

                    // Since we're triggering on 3 bools there are 8 (2^3) possible combinations.
                    // So create an index and use a switch rather than nested if's.
                    // scorebucket 4
                    // comparison 2
                    // points 1
                    int combo = 0;
                    combo += (scoreBucketIndex != -1) ? 4 : 0;
                    combo += (comparisonIndex != -1) ? 2 : 0;
                    combo += hasPointFilter ? 1 : 0;

                    switch(combo)
                    {
                        case 0:
                            {
                                // Did red change?  Add red tile.
                                ScoreBucketFilter sbf = CardSpace.Cards.GetFilter("filter.scorebucket.color.red") as ScoreBucketFilter;
                                data.Filters.Add(sbf);
                            }
                            break;
                        case 1:
                            {
                                // Compare existing points to red.  Add red and equals in front of points.
                                ScoreCompareFilter scf = CardSpace.Cards.GetFilter("filter.scoreequals") as ScoreCompareFilter;
                                data.Filters.Insert(0, scf);
                                ScoreBucketFilter sbf = CardSpace.Cards.GetFilter("filter.scorebucket.color.red") as ScoreBucketFilter;
                                data.Filters.Insert(0, sbf);
                            }
                            break;
                        case 2:
                            {
                                // Comparing red to 0.  Add red in front of comp and 0 after.
                                ScoreBucketFilter sbf = CardSpace.Cards.GetFilter("filter.scorebucket.color.red") as ScoreBucketFilter;
                                data.Filters.Insert(0, sbf);
                                ScoreFilter sf = CardSpace.Cards.GetFilter("filter.000point") as ScoreFilter;
                                data.Filters.Add(sf);
                            }
                            break;
                        case 3:
                            {
                                // Comparing red to points.  Move comp to beginning and then insert red in front of that.
                                if (comparisonIndex != 0)
                                {
                                    ScoreCompareFilter scf = data.Filters[comparisonIndex] as ScoreCompareFilter;
                                    data.Filters.RemoveAt(comparisonIndex);
                                    data.Filters.Insert(0, scf);
                                }
                                ScoreBucketFilter sbf = CardSpace.Cards.GetFilter("filter.scorebucket.color.red") as ScoreBucketFilter;
                                data.Filters.Insert(0, sbf);
                            }
                            break;
                        case 4:
                            {
                                // Did this score change.  Nothing to do here.  Move along.
                            }
                            break;
                        case 5:
                            {
                                // Comparison of score to points.  Need to move score to front and then insert equals after it.
                                if (scoreBucketIndex != 0)
                                {
                                    ScoreBucketFilter sbf = data.Filters[scoreBucketIndex] as ScoreBucketFilter;
                                    data.Filters.RemoveAt(scoreBucketIndex);
                                    data.Filters.Insert(0, sbf);
                                }
                                ScoreCompareFilter scf = CardSpace.Cards.GetFilter("filter.scoreequals") as ScoreCompareFilter;
                                data.Filters.Insert(1, scf);
                            }
                            break;
                        case 6:
                            {
                                if (incomingLanguageVersion < 3)
                                {
                                    // Comparing score to 0.  Need to move score to front and add 0.
                                    if (scoreBucketIndex != 0)
                                    {
                                        ScoreBucketFilter sbf = data.Filters[scoreBucketIndex] as ScoreBucketFilter;
                                        data.Filters.RemoveAt(scoreBucketIndex);
                                        data.Filters.Insert(0, sbf);
                                    }
                                    ScoreFilter sf = CardSpace.Cards.GetFilter("filter.000point") as ScoreFilter;
                                    data.Filters.Add(sf);
                                }
                            }
                            break;
                        case 7:
                            {
                                // Comparing score to points.  Just need to reorder but only do this for older language versions.
                                // With newer versions we are fine.
                                if (incomingLanguageVersion < 3)
                                {
                                    if (scoreBucketIndex != 0 || comparisonIndex != 1)
                                    {
                                        // Move comparison to first slot.
                                        if (comparisonIndex != 0)
                                        {
                                            // Since the comparison is currently after the score, moving 
                                            // it to the front will change the index of the score.
                                            if (comparisonIndex > scoreBucketIndex)
                                            {
                                                ++scoreBucketIndex;
                                            }
                                            ScoreCompareFilter scf = data.Filters[comparisonIndex] as ScoreCompareFilter;
                                            data.Filters.RemoveAt(comparisonIndex);
                                            data.Filters.Insert(0, scf);
                                        }
                                        // Move score to first slot, pushing comparison to second.
                                        if (scoreBucketIndex != 0)
                                        {
                                            ScoreBucketFilter sbf = data.Filters[scoreBucketIndex] as ScoreBucketFilter;
                                            data.Filters.RemoveAt(scoreBucketIndex);
                                            data.Filters.Insert(0, sbf);
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }

                // Do similar thing for DO side.  If default red score is being used, explicitly put it in.
                if (data.actuatorUpid == "actuator.score" || data.actuatorUpid == "actuator.unscore" || data.actuatorUpid == "actuator.scoreset")
                {
                    // Find scorebucket or settings modifier, if any.
                    int scoreBucketIndex = -1;
                    for (int i = 0; i < data.Modifiers.Count; i++)
                    {
                        if (data.Modifiers[i] is ScoreBucketModifier || data.Modifiers[i] is SettingsModifier)
                        {
                            scoreBucketIndex = i;
                            break;
                        }
                    }
    
                    // If not found, add the default red bucket. If it was found, move it to the front.
                    if (scoreBucketIndex == -1)
                    {
                        ScoreBucketModifier sbm = CardSpace.Cards.GetModifier("modifier.scorebucket.color.red") as ScoreBucketModifier;
                        data.Modifiers.Insert(0, sbm);
                    }
                    else
                    {
                        // Note that it's important that the scoreBucketIndex is the first one we find.
                        // In later language versions (3 and above) there may be multiple scorebuckets
                        // but the first one should always be in index 0 so this should be a noop.
                        if (scoreBucketIndex != 0)
                        {
                            ScoreBucketModifier sbm = data.Modifiers[scoreBucketIndex] as ScoreBucketModifier;
                            data.Modifiers.RemoveAt(scoreBucketIndex);
                            data.Modifiers.Insert(0, sbm);
                        }
                    }
                }
            }   // end if atLoadTime

            // HACK For testing health we previously allowed HealthSensor #s <optional NOT>
            // Change this to put in Filter.Equals or Filter.NotEquals (depending on whether a NOT exists)
            // If no #s are present then assume a comparison to 0.
            // Result should look more like score comparisons.
            //
            // Somehow I screwed up and we ended up with some code that looks like:
            // WHEN Health scoreEquals healthAbove points.
            // Need to detect this, remove the scoreEquals and replace the healthAbove with scoreAbove.
            if (atLoadTime)
            {
                if (data.sensorUpid == "sensor.health")
                {
                    // What currently exits?
                    bool scoreCompExists = false;
                    bool healthCompExists = false;
                    for (int i = 0; i < data.Filters.Count; i++)
                    {
                        if (data.Filters[i] is ScoreCompareFilter)
                        {
                            scoreCompExists = true;
                        }
                        if (data.Filters[i] is HealthCompareFilter)
                        {
                            healthCompExists = true;
                        }
                    }

                    // If we have both, remove the scoreComp and then 
                    // replace the healthComp with the proper scoreComp.
                    if (scoreCompExists && healthCompExists)
                    {
                        for (int i = 0; i < data.Filters.Count; i++)
                        {
                            if (data.Filters[i] is ScoreCompareFilter)
                            {
                                data.Filters.RemoveAt(i);
                                --i;
                            }
                        }
                    }

                    // Convert old healthabove and healthbelow filters to the matching score filters.
                    if (healthCompExists)
                    {
                        for (int i = 0; i < data.Filters.Count; i++)
                        {
                            if (data.Filters[i] is HealthCompareFilter)
                            {
                                switch (data.Filters[i].upid)
                                {
                                    case "filter.healthabove":
                                        {
                                            string upid = "filter.scoreabove";
                                            Filter cmp = CardSpace.Cards.GetFilter(upid);
                                            Debug.Assert(cmp != null);
                                            data.Filters.RemoveAt(i);
                                            data.Filters.Insert(i, cmp);
                                        }
                                        break;
                                    case "filter.healthbelow":
                                        {
                                            string upid = "filter.scorebelow";
                                            Filter cmp = CardSpace.Cards.GetFilter(upid);
                                            Debug.Assert(cmp != null);
                                            data.Filters.RemoveAt(i);
                                            data.Filters.Insert(i, cmp);
                                        }
                                        break;
                                }
                                break;
                            }
                        }
                    }   // end if healthCompExists

                    // Do we already have a comparison filter?
                    bool compFilterExists = false;
                    for (int i = 0; i < data.Filters.Count; i++)
                    {
                        if (data.Filters[i] is ScoreCompareFilter)
                        {
                            compFilterExists = true;
                            break;
                        }
                    }

                    // If we don't have a comparison then add one, either = or !=
                    // depending on whether filter.not is used.
                    if (!compFilterExists)
                    {
                        bool notExists = false;
                        // Find and remove not filter if it's there.
                        for (int i = 0; i < data.Filters.Count; i++)
                        {
                            if (data.Filters[i] is NotFilter)
                            {
                                notExists = true;
                                data.Filters.RemoveAt(i);
                                break;
                            }
                        }

                        // Insert = or != as needed.
                        string upid = notExists ? "filter.notequals" : "filter.scoreequals";
                        Filter op = CardSpace.Cards.GetFilter(upid);
                        Debug.Assert(op != null);
                        data.Filters.Insert(0, op);
                    }

                    // If there are no numbers after the comparison, add 0.
                    if (data.Filters.Count == 1)
                    {
                        string upid = "filter.000point";
                        Filter pnts = CardSpace.Cards.GetFilter(upid);
                        Debug.Assert(pnts != null);
                        data.Filters.Add(pnts);
                    }
                }
            }   // end if atLoadTime

            // HACK  this one is caused by an early version of the turning behavior.  At that time
            // Move(actuator) Turn(selector) Toward(modifier) was how you turned.  Now it's
            // Turn(actuator) Toward(modifier)
            // So we need to find this pattern, remove the selector and replace the actuator.
            // Sample world to show this is Kodu FC vs Kodu United
            if (data.Selector != null && data.Selector.upid == "selector.moveleftright")
            {
                if (data.Actuator != null && data.Actuator.upid == "actuator.movement")
                {
                    // Note we have to change things in both places since there's
                    // no auto sync between Reflex and ReflexData.
                    // TODO This should be cleaned up.
                    data.Selector = null;
                    data.selectorUpid = null;
                    selector = null;
                    selectorUpid = null;

                    // Note that setting the data.actuatorUpid automagically
                    // sets the actuator and the values on the reflex, too.
                    TurnActuator ta = new TurnActuator();
                    data.actuatorUpid = "actuator.turn";
                }
            }

            // HACK  Do Shoot Level Missile is breaking on Level.  This problem is that since
            // we added blips "Level" is no longer always valid.  It's only available after
            // "missile" is chosen.  So look for "level" or "cruise" before "missile" and 
            // if found, swap places.
            if (data.Actuator != null && data.Actuator.upid == "actuator.shoot2")
            {
                int missileIndex = data.GetModifierIndex("modifier.projectile.missile");
                int trackingIndex = data.GetModifierIndex("modifier.trackingnone");
                // If we didn't find it, try looking for cruise.
                if (trackingIndex == -1)
                {
                    trackingIndex = data.GetModifierIndex("modifier.trackinghoming");
                }

                // If we have both.
                if (missileIndex != -1 && trackingIndex != -1)
                {
                    // If we need to swap order.
                    if (missileIndex > trackingIndex)
                    {
                        // Swap in ReflexData.
                        // This automaticaly updates the list in the Reflex.
                        Modifier tmp = data.Modifiers[missileIndex];
                        data.Modifiers[missileIndex] = data.Modifiers[trackingIndex];
                        data.Modifiers[trackingIndex] = tmp;
                    }
                }

            }

            // HACK  Tracking used to be valid when we assumed missiles.  Since blips are the default
            // we don't need the tracking modifiers.  So, delete if no missile found.
            if (data.Actuator != null && data.actuatorUpid == "actuator.shoot2" || data.actuatorUpid == "actuator.shoot")
            {
                int missileIndex = data.GetModifierIndex("modifier.projectile.missile");

                // If we don't have a missile, remove any tracking modifier.
                if (missileIndex == -1)
                {
                    int trackingIndex = data.GetModifierIndex("modifier.trackingnone");
                    if (trackingIndex != -1)
                    {
                        data.Modifiers.RemoveAt(trackingIndex);
                    }
                    
                    trackingIndex = data.GetModifierIndex("modifier.trackinghoming");
                    if (trackingIndex != -1)
                    {
                        data.Modifiers.RemoveAt(trackingIndex);
                    }
                }
            }
            
            // HACK For some reason we used to allow Move Forward North.  So, now if
            // we see a selector (Forward, Wander, etc) combined with a direction (N, S, E, W)
            // we'll just ignore the selector.
            if (data.Actuator != null && data.Actuator.upid == "actuator.movement")
            {
                bool dirModifierExists = false;
                for (int i = 0; i<data.Modifiers.Count; i++)
                {
                    if (data.Modifiers[i].upid == "modifier.north"
                        || data.Modifiers[i].upid == "modifier.south"
                        || data.Modifiers[i].upid == "modifier.east"
                        || data.Modifiers[i].upid == "modifier.west")
                    {
                        dirModifierExists = true;
                    }
                }

                if (dirModifierExists)
                {
                    // Remove Selector if it exists.
                    if (data.Selector != null)
                    {
                        data.Selector = null;
                        data.selectorUpid = null;
                        selector = null;
                        selectorUpid = null;
                    }
                }
            }

            // HACK  Launch used to allow 'forward' as a modifier.  It no longer does since that's the default.
            // So, if we have a launch actuator, remove any forward modifier if it's there.
            if (data.Actuator != null && data.Actuator.upid == "actuator.launch")
            {
                for (int i = 0; i < data.Modifiers.Count; i++)
                {
                    if (data.Modifiers[i].upid == "modifier.forward")
                    {
                        data.Modifiers.RemoveAt(i);

                        break;
                    }
                }
            }

            // Bump and Once have never worked together so we no longer allow them together.
            if (data.Sensor != null && data.sensorUpid == "sensor.bumpers")
            {
                int onceIndex = data.GetModifierIndex("modifier.once");
                if (onceIndex != -1)
                {
                    data.Modifiers.RemoveAt(onceIndex);
                }
            }

            ProgrammingElementFixup();

            data.modifierParams.Clear();

            for (int i = 0; i < Modifiers.Count; ++i)
            {
                Modifiers[i].GatherParams(data.modifierParams);

                // TODO Microbit
                // Hack to get hit points for HealthModifier.
                if (Modifiers[i] is HealthModifier)
                {
                    data.modifierParams.Points = this.Task.GameActor.HitPoints;
                }
                if (Modifiers[i] is MaxHealthModifier)
                {
                    data.modifierParams.Points = this.Task.GameActor.MaxHitPoints;
                }
            }

            // Originally, the NextLevel tile uses the next level specified in 
            // XmlWorldData.  Now, we want to use the GUID string stored in 
            // ReflexData.ParamString.  So, for old levels, copy the level GUID
            // from XmlWorldData to the reflex.
            if (incomingLanguageVersion < 9 && InGame.XmlWorldData.LinkedToLevel != null)
            {
                Data.ParamString = InGame.XmlWorldData.LinkedToLevel.Value.ToString();
            }

        }   // end of Fixup()

        protected void TaskFixup()
        {
            // This will be null on load but after full load FixUp gets called anyway.
            if (task != null)
            {
                task.Fixup(atLoadTime: false, incomingLanguageVersion: int.Parse(Program2.CurrentKCodeVersion));
            }
        }

        protected void RebuildProcessFilters()
        {
            data.MakeValid();
            processFilters.Clear();
            processFilters.AddRange(rawfilters);
            AppendCompatibleHiddenFilters();
        }

        protected void ProgrammingElementFixup()
        {
            // due to XML loading not setting parent task until after 
            // the member ProgrammingElements reference upids we have to
            // reset them asking our brain for the real instances
            if (sensor != null && sensor.upid != ProgrammingElement.upidNull)
            {
                sensor = CardSpace.Cards.GetSensor(sensor.upid) as Sensor;
            }
            for (int iFilter = 0; iFilter < rawfilters.Count; iFilter++)
            {
                Filter filter = rawfilters[iFilter] as Filter;
                // clean up and XML oddities of stored NULLs
                if (filter == null)
                {
                    rawfilters.RemoveAt(iFilter);
                    iFilter--;
                }
                else if (!(filter is NullFilter))
                {
                    rawfilters[iFilter] = CardSpace.Cards.GetFilter(filter.upid) as Filter;
                }
            }

            RebuildProcessFilters();

            if (selector != null && !(selector is NullSelector))
            {
                selector = CardSpace.Cards.GetSelector(selector.upid) as Selector;
                this.selectorActive = selector;
            }
            else
            {
                this.selectorActive = FindCompatibleHiddenSelector();
            }
            for (int iModifier = 0; iModifier < modifiers.Count; iModifier++)
            {
                Modifier modifier = modifiers[iModifier] as Modifier;
                // clean up and XML oddities of stored NULLs
                if (modifier == null)
                {
                    modifiers.RemoveAt(iModifier);
                    iModifier--;
                }
                else if (!(modifier is NullModifier))
                {
                    Modifier newMod = CardSpace.Cards.GetModifier(modifier.upid);

                    if (newMod != null)
                    {
                        modifiers[iModifier] = CardSpace.Cards.GetModifier(modifier.upid) as Modifier;
                    }
                    else
                    {
                        modifiers.RemoveAt(iModifier);
                        iModifier--;
                    }
                }
            }
            if (selectorActive != null)
            {
                selectorActive.Fixup(this);
            }
        }

        // Used as a local variable in AppendCompatibleHiddenFilters.
        static List<Filter> _scratchHiddenFilters;
        List<Filter> scratchHiddenFilters
        {
            get { return _scratchHiddenFilters ?? (_scratchHiddenFilters = new List<Filter>()); }
        }

        protected void AppendCompatibleHiddenFilters()
        {
            scratchHiddenFilters.Clear();

            // search the filters and remember the compatible hidden filters
            List<Filter> hiddenDefaults = CardSpace.Cards.HiddenDefaults;
            for (int indexFilter = 0; indexFilter < hiddenDefaults.Count; indexFilter++)
            {
                Filter filter = hiddenDefaults[indexFilter];
                Debug.Assert((filter != null)
                    && filter.hiddenDefault
                    && !filter.archived);

                if (filter.ReflexCompatible(this, null, false))
                {
                    scratchHiddenFilters.Add(filter);
                }
            }

            // now walk our filters and add the hiddenDefault if there isn't the same type
            // already present
            for (int indexDefault = 0; indexDefault < scratchHiddenFilters.Count; indexDefault++)
            {
                Filter filterDefault = scratchHiddenFilters[indexDefault];

                bool present = false;
                for (int indexFilter = 0; indexFilter < this.processFilters.Count; indexFilter++)
                {
                    Filter filter = this.processFilters[indexFilter] as Filter;
                    if (filter.GetType() == filterDefault.GetType())
                    {
                        // one of this type present
                        present = true;
                        break;
                    }
                }
                if (!present)
                {
                    // add an instance
                    filterDefault = (Filter)filterDefault.Clone();
                    this.processFilters.Add(filterDefault);
                }
            }
        }

        internal void ApplyConstraints()
        {
            foreach (Modifier modifier in Modifiers)
            {
                if (modifier is ConstraintModifier)
                {
                    ConstraintModifier cm = modifier as ConstraintModifier;
                    cm.Constrain(this);
                }
            }
        }

        /// <summary>
        /// Remove all invalid elements
        /// </summary>
        internal void Validate()
        {
            // Remove invalid creatables
            for (int i = 0; i < modifiers.Count; ++i)
            {
                Modifier modifier = modifiers[i] as Modifier;
                if (modifier is CreatableModifier)
                {
                    CreatableModifier cm = modifier as CreatableModifier;
                    if (CardSpace.Cards.GetModifier(cm.upid) == null)
                    {
                        modifiers.RemoveAt(i);
                        CardSpace.Cards.UncacheCardFace(cm.upid);
                        --i;
                    }
                }
            }
        }

        public bool ModifyHeading(GameActor gameActor, ref Vector3 heading)
        {
            return ModifyHeading(gameActor, Modifier.ReferenceFrames.All, ref heading);
        }

        /// <summary>
        /// TODO (****) What I _think_ this is supposed to do is to take the exisiting heading
        /// and modify it based on any modifier tiles (Quckly, Slowly, etc.)
        /// Basically this will just scale the length of heading.
        /// This also appear to be where contraints to the motion should be applied but I
        /// suspect they are being done elsewhere.  So, the question because, is this serving 
        /// any useful purpose?  If so, what is it and please document it.
        /// </summary>
        /// <param name="gameActor"></param>
        /// <param name="frames"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        public bool ModifyHeading(GameActor gameActor, Modifier.ReferenceFrames frames, ref Vector3 heading)
        {
            bool apply = true;

            for (int i = 0; apply && i < Modifiers.Count; ++i)
            {
                Modifier modifier = modifiers[i] as Modifier;
                if (0 != (modifier.referenceFrame & frames))
                    apply = modifier.ModifyHeading(this, gameActor, ref heading);
            }

            return apply;
        }

        public bool HasModifier(string upid)
        {
            return data.HasModifier(upid);
        }

        public T GetModifier<T>(string upid) where T : Modifier
        {
            return data.GetModifier<T>(upid);
        }

        public Modifier GetModifierByType(Type modType)
        {
            return data.GetModifierByType(modType);
        }

        public T GetModifierByType<T>() where T : Modifier
        {
            return data.GetModifierByType(typeof(T)) as T;
        }

        public List<Modifier> GetModifiersByType(Type modType)
        {
            return data.GetModifiersByType(modType);
        }

        public int GetModifierCountByType(Type modType)
        {
            return data.GetModifierCountByType(modType);
        }

        public bool IsUserControlled
        {
            get
            {
                return
                    sensor != null &&
                    actuator != null &&
                    sensor.IsUserControlled &&
                    actuator.IsMovement;
            }
        }

        public bool IsMovement
        {
            get { return actuator != null && actuator.IsMovement; }
        }

        public bool IsTurning
        {
            get { return actuator != null && actuator.IsTurning; }
        }

        public bool RightStickControlled
        {
            get
            {
                if (sensor is GamePadSensor && Filters.Count > 0)
                {
                    Filter filter = data.GetFilterByType(typeof(GamePadStickFilter));

                    if (filter != null && (filter as GamePadStickFilter).stick == GamePadStickFilter.GamePadStick.Right)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Even if the sensor fires, the action may be suppressed by the game actor being acted on for reasons the
        /// brain cannot be aware of, therefore we keep a running count in the once modifier of the number of times
        /// an effector was acted on in this reflex.
        /// </summary>
        public void AdjustOnceModifiers()
        {
            foreach (Modifier modifier in Modifiers)
            {
                OnceModifier onceMod = modifier as OnceModifier;
                if (onceMod != null)
                {
                    if (onceMod.Fired && actedOn)
                    {
                        onceMod.applyCount += 1;
                    }
                    else if (!onceMod.Fired)
                    {
                        onceMod.applyCount = 0;
                    }
                    break;
                }
            }
        }

        public void ResetOnceModifiers()
        {
            foreach (Modifier modifier in Modifiers)
            {
                OnceModifier onceMod = modifier as OnceModifier;
                if (onceMod != null)
                {
                    onceMod.Reset(this);
                }
            }
        }

        public static Reflex DeepCopy(Reflex srcReflex)
        {
            Reflex dstReflex = new Reflex();

            dstReflex.data = ReflexData.DeepCopy(srcReflex.data);

            return dstReflex;
        }
    }

}
