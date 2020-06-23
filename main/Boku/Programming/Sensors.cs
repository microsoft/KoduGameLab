using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    public enum SensorOutputType
    {
        // Bits that indicate datatype of sensor output. For filtering elements to sensors with compatible output.
        Boolean,
        Integer,
        Real,
        Vector,
        ActorSet,
        Time,
        Score,
        Health,
        TerrainMaterial,
        WaterMaterial,
        PathColor,
        TouchButton,

        // Bits that indicate output from a specific sensor. For filtering elements to specific sensors.
        SoundSensor,
        ScoreSensor,

        SIZEOF
    }

    public class SensedActorSet
    {
        static SensedActorSet empty = new SensedActorSet();

        public static SensedActorSet Empty { get { return empty; } }
    }

    public class SensorOutputBlock
    {
        BitArray hasBits = new BitArray((int)SensorOutputType.SIZEOF);
        bool hasAnything;

        int integer;
        float real;
        bool boolean;
        Vector3 vector;
        SensedActorSet actorSet;
        float time;
        int score;
        int health;
        int terrainMat;
        int waterMat;

        public int Integer { get { return integer; } set { integer = value; SetBit(SensorOutputType.Integer); } }
        public float Real { get { return real; } set { real = value; SetBit(SensorOutputType.Real); } }
        public bool Boolean { get { return boolean; } set { boolean = value; SetBit(SensorOutputType.Boolean); } }
        public Vector3 Vector { get { return vector; } set { vector = value; SetBit(SensorOutputType.Vector); } }
        public SensedActorSet ActorSet { get { return actorSet; } set { actorSet = value; SetBit(SensorOutputType.ActorSet); } }
        public float Time { get { return time; } set { time = value; SetBit(SensorOutputType.Time); } }
        public int Score { get { return score; } set { score = value; SetBit(SensorOutputType.Score); } }
        public int Health { get { return health; } set { health = value; SetBit(SensorOutputType.Health); } }
        public int TerrainMaterial { get { return terrainMat; } set { terrainMat = value; SetBit(SensorOutputType.TerrainMaterial); } }
        public int WaterMaterial { get { return waterMat; } set { waterMat = value; SetBit(SensorOutputType.WaterMaterial); } }

        /*
        
        // WARNING these are commented out so that you don't try and use them.
        // None of the bit values are ever set so these will always return false.
          
        public bool HasInteger { get { return GetBit(SensorOutputType.Integer); } }
        public bool HasReal { get { return GetBit(SensorOutputType.Real); } }
        public bool HasBoolean { get { return GetBit(SensorOutputType.Boolean); } }
        public bool HasVector { get { return GetBit(SensorOutputType.Vector); } }
        public bool HasActorSet { get { return GetBit(SensorOutputType.ActorSet); } }
        public bool HasTime { get { return GetBit(SensorOutputType.Vector); } }
        public bool HasScore { get { return GetBit(SensorOutputType.Score); } }
        public bool HasHealth { get { return GetBit(SensorOutputType.Health); } }
        public bool HasTerrainMaterial { get { return GetBit(SensorOutputType.TerrainMaterial); } }
        public bool HasWaterMaterial { get { return GetBit(SensorOutputType.WaterMaterial); } }
        */
        public bool HasAnything { get { return hasAnything; } }
        

        public SensorOutputBlock()
        {
            Reset();
        }

        public void Reset()
        {
            integer = 0;
            real = 0f;
            boolean = false;
            vector = Vector3.Zero;
            actorSet = SensedActorSet.Empty;
            time = 0f;
            score = 0;
            health = 0;
            terrainMat = -1;
            waterMat = -1;

            hasBits.SetAll(false);
            hasAnything = false;
        }

        private void SetBit(SensorOutputType bit)
        {
            hasBits.Set((int)bit, true);
            hasAnything = true;
        }

        private bool GetBit(SensorOutputType bit)
        {
            return hasBits.Get((int)bit);
        }
    }

    /// <summary>
    /// Sensor represents the base class for all sensor types
    /// It is the input into the behavior based programming model
    /// </summary>
    public abstract class Sensor : ProgrammingElement
    {
        private BitArray outputs = new BitArray((int)SensorOutputType.SIZEOF);

        int outputCount;

        [XmlArray]
        [XmlArrayItem("Output")]
        public List<SensorOutputType> XmlOutputs = new List<SensorOutputType>();

        [XmlIgnore]
        public BitArray Outputs { get { return outputs; } }

        [XmlIgnore]
        public int OutputCount { get { return outputCount; } }

        SensorOutputBlock outputBlock = new SensorOutputBlock();

        public SensorOutputBlock OutputBlock { get { return outputBlock; } }

        public bool IsVisualDevice { get { return Categories.Get((int)BrainCategories.WhenSight); } }

        public bool WantThingUpdate
        {
            get
            {
                return
                    Categories.Get((int)BrainCategories.WhenSight) ||
                    Categories.Get((int)BrainCategories.WhenHearing) ||
                    Categories.Get((int)BrainCategories.WhenEndofPath); 
            }
        }

        public bool IsUserControlled
        {
            get
            {
                return
                    Categories.Get((int)BrainCategories.WhenUserInput) ||
                    Categories.Get((int)BrainCategories.WhenGamePad) ||
                    Categories.Get((int)BrainCategories.WhenMicrobit) ||
                    Categories.Get((int)BrainCategories.WhenKeyBoard) ||
                    Categories.Get((int)BrainCategories.WhenTouch) ||
                    Categories.Get((int)BrainCategories.WhenMouse);
            }
        }

        protected void CopyTo(Sensor clone)
        {
            base.CopyTo(clone);
            clone.XmlOutputs = this.XmlOutputs;
            clone.outputs = this.outputs;
            clone.outputCount = this.outputCount;
            // don't copy outputBlock
        }

        public override void OnLoad()
        {
            base.OnLoad();

            outputs.SetAll(false);

            outputCount = 0;

            foreach (SensorOutputType type in XmlOutputs)
            {
                outputs.Set((int)type, true);
                outputCount += 1;
            }
        }

        /// <summary>
        /// Start update is called for sensors that sensor GameThings to let the sensor know it 
        /// will called on ThingUpdate for every GameThing in the world and then called on FinishUpdate
        /// This will happen on the GameActors responce cycle and not every update frame
        /// </summary>
        public abstract void StartUpdate(GameActor gameActor);
        
        /// <summary>
        /// Called for every GameThing in the world.
        /// </summary>
        /// <param name="gameThing"></param>
        /// <param name="direction"></param>
        /// <param name="range"></param>
        public abstract void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range);
        
        /// <summary>
        /// Called after all ThingUpdate were called.
        /// </summary>
        public abstract void FinishUpdate(GameActor gameActor);

        /// <summary>
        /// Provide a SensorTargetSet from this Sensors output after applying the given set of filters
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public abstract void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex);

        /// <summary>
        /// Applies special rules, such as the "once" rules (In OnceModifier).
        /// 
        /// In fact this is only ever used for the "once" modifier.  Do we
        /// actually need all this or could we just check for "once" and 
        /// handle that as a special case rather than expecting every single
        /// filter and modifier to need a PostProcessAction?
        /// </summary>
        /// <param name="firing"></param>
        /// <param name="reflex"></param>
        /// <returns></returns>
        public virtual bool PostProcessAction(bool firing, Reflex reflex)
        {
            bool action = firing;

            for (int i = 0; i < reflex.Filters.Count; ++i)
            {
                Filter filter = reflex.Filters[i] as Filter;
                filter.PostProcessAction(firing, reflex, ref action);
            }

            for (int i = 0; i < reflex.Modifiers.Count; ++i)
            {
                Modifier modifier = reflex.Modifiers[i] as Modifier;
                modifier.PostProcessAction(firing, reflex, ref action);
            }

            return action;
        }

        public override bool ActorCompatible(GameActor gameActor)
        {
            if (base.ActorCompatible(gameActor))
            {
                // If this is a sight sensor...
                if (gameActor != null && this.IsVisualDevice)
                {
                    // make sure the actor has visual capability
                    if (gameActor.VisualDevices.Count > 0)
                    {
                        return true;
                    }
                }
                else
                {
                    // non-devices just need the base check
                    return true;
                }
            }
            return false;
        }

        // helper method 
        protected bool TestObjectSet(Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            bool match = true;
            for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
            {
                Filter filter = filters[indexFilter] as Filter;
                object param;
                if (!filter.MatchAction(reflex, out param))
                {
                    match = false;
                    break;
                }
                if (param != null)
                {
                    reflex.targetSet.Param = param;
                }
            }

            // After everything is done, apply the not filter,
            // if any, to negate the result.
            if (reflex.Data.FilterExists("filter.not"))
            {
                match = !match;
            }

            match = PostProcessAction(match, reflex);

            return match;

        }   // end TestObjectSet()
        
    }   // end class Sensor
    
    /// <summary>
    /// Represents a sensed object in the scene. Instances of this class may
    /// appear in more than one sensor target set.
    /// </summary>
    public class SensorTarget : IComparable
    {
        #region Members

        private GameThing gameThing;
        private GameThing shooter;      // Only valid for ShotHit
        private Movement movement;
        private Classification classification;
        private double sensedAt; // Time.GameTimeTotalSeconds
        private float range;
        private Vector3 position;
        private Vector3 direction;

        public int refs;

        #endregion

        #region Accessors

        public GameThing GameThing
        {
            get { return gameThing; }
            set { gameThing = value; }
        }
        public GameThing Shooter
        {
            get { return shooter; }
            set { shooter = value; }
        }
        public Movement Movement
        {
            get { return movement; }
            set { movement = value; }
        }
        public Classification Classification
        {
            get { return classification; }
            set { classification = value; }
        }
        public float Range
        {
            get { return range; }
            set { range = value; }
        }
        public double SensedAt
        {
            get { return sensedAt; }
            set { sensedAt = value; }
        }
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        public object Tag
        {
            get;
            set;
        }

        #endregion

        #region Public

        /// <summary>
        /// Called in the case where a single gameThing has multiple targets (walls)
        /// </summary>
        /// <param name="gameThing"></param>
        /// <param name="movement"></param>
        /// <param name="range"></param>
        public void Init(GameThing gameThing, Movement movement, Classification classification, float range, Vector3 direction)
        {
            this.gameThing = gameThing;
            this.movement = movement;
            this.classification = classification;
            this.range = range;
            this.position = Movement.Position;
            this.direction = direction;
            this.sensedAt = Time.GameTimeTotalSeconds;
            this.Tag = null;
        }

        public void Init(GameActor gameActor, GameThing gameThing)
        {
            Vector3 actorPos = gameActor.Movement.Position;
            Vector3 thingPos = gameThing.Movement.Position;

            this.direction = thingPos - actorPos;
            this.range = this.direction.Length();
            if (this.range > 0.0f)
            {
                this.direction *= 1.0f / this.range;
            }

            this.gameThing = gameThing;
            this.position = thingPos;
            this.classification = gameThing.Classification;
            this.movement = gameThing.Movement;
            this.sensedAt = Time.GameTimeTotalSeconds;
            this.Tag = null;
        }
        /// <summary>
        /// Called in the case where a gamething is a single Target
        /// </summary>
        /// <param name="gameThing"></param>
        /// <param name="range"></param>
        public void Init(GameThing gameThing, Vector3 direction, float range)
        {
            this.gameThing = gameThing;
            this.classification = gameThing.Classification;
            this.movement = gameThing.Movement;
            this.position = movement.Position;
            this.direction = direction;
            this.range = range;
            this.sensedAt = Time.GameTimeTotalSeconds;
            this.Tag = null;
        }

        public int CompareTo(Object right)
        {
            if (right is SensorTarget)
            {
                SensorTarget rightTarget = right as SensorTarget;
                int result = 0;
                if (range < rightTarget.range)
                    result = -1;
                else if (range > rightTarget.range)
                    result = 1;
                else
                    result = gameThing.CompareTo(rightTarget.gameThing);
                return result;
            }

            throw new ArgumentException("object is not a SensorTarget");
        }

        #endregion

        #region Internal

        internal void Clear()
        {
            Debug.Assert(refs == 0);
            gameThing = null;
            movement = null;
            classification = null;
            sensedAt = 0.0;
            range = 0f;
            position = Vector3.Zero;
            direction = Vector3.Zero;
            Tag = null;
        }

        internal SensorTarget Ref()
        {
            refs++;
            return this;
        }

        internal int UnRef()
        {
            // The last unref must come from SensorTargetSpares.Free()
            Debug.Assert(refs > 1);
            refs--;
            return refs;
        }

        #endregion

    }   // end of class SensorTarget


    public class SensorTargetSet : IEnumerable
    {
        private Dictionary<int, SensorTarget> sensorTargets = new Dictionary<int, SensorTarget>();
        private List<SensorTarget> nearestTargets = new List<SensorTarget>();   // List of targets.  This list should be sorted by distance.
        private bool action = false;
        private bool actionMouseTarget = false; // Action caused by persistent mouse target.
        private object param;

        private bool dirty = true;  // Set when the sensorTargets dictionary changes.
        
        /// <summary>
        /// Nearest target, if any, in the target set.
        /// </summary>
        public SensorTarget Nearest
        {
            get 
            {
                SortTargets(justGetFirst: true);
                return nearestTargets.Count > 0 ? nearestTargets[0] : null; 
            }
            set { }
        }

        public SensorTargetSet()
        {
        }
        public SensorTargetSet(bool action)
        {
            this.action = action;
        }

        /// <summary>
        /// Gets a list of the nearest objects
        /// </summary>
        public IList<SensorTarget> NearestTargets
        {
            get 
            {
                SortTargets(justGetFirst: false); 
                return nearestTargets; 
            }
        }

        public void Add(GameThing gameThing, Vector3 direction, float range)
        {
            if (sensorTargets.ContainsKey(gameThing.UniqueNum))
                return;

            SensorTarget target = SensorTargetSpares.Alloc();
            target.Init(gameThing, direction, range);
            sensorTargets.Add(gameThing.UniqueNum, target.Ref());
            dirty = true;
        }
        /// <summary>
        /// Try adding to the set, if unsuccessful, free the SensorTarget.
        /// This is a shortcut for when you've just Alloc'd a sensor target,
        /// and then:
        ///   if(!Add(targ))
        ///     SensorTargetSpares.Release(targ);
        /// This is only appropriate where the target has just been alloc'd, so
        /// not iterating through a set, and is a local that will just fall out of scope.
        /// </summary>
        /// <param name="sensorTarget"></param>
        /// <returns>false if not added but freed, true if added</returns>
        public bool AddOrFree(SensorTarget sensorTarget)
        {
            Debug.Assert(sensorTarget.refs == 1, "Improper usage, use Add()?");
            if (!Add(sensorTarget))
            {
                if (sensorTarget.refs == 1)
                {
                    SensorTargetSpares.Free(sensorTarget);
                }
                return false;
            }
            return true;
        }
        public bool Add(SensorTarget sensorTarget)
        {
            if (sensorTargets.ContainsKey(sensorTarget.GameThing.UniqueNum))
                return false;

            sensorTargets.Add(sensorTarget.GameThing.UniqueNum, sensorTarget.Ref());
            dirty = true;

            return true;
        }
        public void Add(SensorTargetSet otherSet)
        {
            if (otherSet.Count > 0)
            {
                SensorTargetSet.Enumerator setIter = (SensorTargetSet.Enumerator)otherSet.GetEnumerator();

                while (setIter.MoveNext())
                {
                    Add(setIter.Current as SensorTarget);
                }
            }
        }

        /// <summary>
        /// Remove all SensorTargets referencing 'thing'
        /// </summary>
        /// <param name="dropThing"></param>
        public void Remove(GameThing thing)
        {
            if (sensorTargets.ContainsKey(thing.UniqueNum))
            {
                SensorTarget target = sensorTargets[thing.UniqueNum];
                sensorTargets.Remove(thing.UniqueNum);
                target.UnRef();
                dirty = true;
            }
        }

        public delegate bool CheckTarget(GameActor actor, GameActor target);

        /// <summary>
        /// Either sorts the full target list or just grabs the nearest target.
        /// Dependson whether this is called by Nearest or NearestTargets.
        /// </summary>
        /// <returns></returns>
        private void SortTargets(bool justGetFirst)
        {
            if (dirty)
            {
                nearestTargets.Clear();

                // Copy all non-ignored targets to list.
                foreach (SensorTarget target in sensorTargets.Values)
                {
                    if (target.GameThing != null || !target.GameThing.Ignored)
                    {
                        nearestTargets.Add(target);
                    }
                }

                if (justGetFirst)
                {
                    // Find the nearest target and copy into position 0.
                    // This leaves the nearest target at position 0 but
                    // leaves the rest of the list in kind of a mess so
                    // don't use it for anything else.
                    for (int i = 1; i < nearestTargets.Count; i++)
                    {
                        if (nearestTargets[0].Range > nearestTargets[i].Range)
                        {
                            nearestTargets[0] = nearestTargets[i];
                        }
                    }
                }
                else
                {
                    // Sort full list.  Rarely used and the general case
                    // will be a very short list so just bubble sort.
                    for (int i = 0; i < nearestTargets.Count - 1; i++)
                    {
                        for (int j = i + 1; j < nearestTargets.Count; j++)
                        {
                            if (nearestTargets[i].Range > nearestTargets[j].Range)
                            {
                                // Swap.
                                SensorTarget tmp = nearestTargets[i];
                                nearestTargets[i] = nearestTargets[j];
                                nearestTargets[j] = tmp;
                            }
                        }
                    }
                }
                dirty = false;
            }   // end if dirty
        }   // end of SortTargets()

        private void ClearTargets()
        {
            foreach (SensorTarget target in sensorTargets.Values)
            {
                // The last unref must be done by the spare list.
                if (target.UnRef() == 1)
                    SensorTargetSpares.Free(target);
            }
            sensorTargets.Clear();
            dirty = true;
        }

        public void Clear()
        {
            ClearTargets();
            action = false;
            actionMouseTarget = false;
            dirty = true;
        }

        public bool Contains(SensorTarget target)
        {
            return sensorTargets.ContainsKey(target.GameThing.UniqueNum);
        }

        public bool Contains(GameThing thing)
        {
            return sensorTargets.ContainsKey(thing.UniqueNum);
        }

        /// <summary>
        /// Is any action happening on this reflex?
        /// </summary>
        public bool AnyAction
        {
            get { return action | actionMouseTarget; }
        }
        /// <summary>
        /// Is this sensor triggering an action?
        /// </summary>
        public bool Action
        {
            get { return this.action; }
            set { this.action = value; }
        }
        /// <summary>
        /// Is this sensor triggering an action caused
        /// by a previous mouse action.  For example if we
        /// have Mouse Left Move Toward, we will continue 
        /// to move toward the target until cleared.  So,
        /// this will be true.  
        /// </summary>
        public bool ActionMouseTarget
        {
            get { return actionMouseTarget; }
            set { actionMouseTarget = value; }
        }

        public object Param
        {
            get { return this.param; }
            set { this.param = value; }
        }
        public int Count
        {
            get { return sensorTargets.Count; }
        }

        #region ENUMERATOR
        public class Enumerator : IEnumerator
        {
            SensorTargetSet set;
            Dictionary<int, SensorTarget>.Enumerator enumerator;

            public Enumerator(SensorTargetSet set)
            {
                this.set = set;
                this.enumerator = (Dictionary<int, SensorTarget>.Enumerator)set.sensorTargets.GetEnumerator();
            }

            public object Current
            {
                get { return enumerator.Current.Value; }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator = (Dictionary<int, SensorTarget>.Enumerator)set.sensorTargets.GetEnumerator();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion
    }

    public static class SensorTargetSpares
    {
        private static List<SensorTarget> spareSensorTargets = new List<SensorTarget>();

        public static SensorTarget Alloc()
        {
            SensorTarget target;
            if (spareSensorTargets.Count > 0)
            {
                int index = spareSensorTargets.Count - 1;
                target = spareSensorTargets[index];
                spareSensorTargets.RemoveAt(index);
            }
            else
            {
                target = new SensorTarget();
            }

            target.refs++;
            return target;
        }

        public static void Free(SensorTarget target)
        {
            target.refs--;

            Debug.Assert(target.refs == 0);

            target.Clear();
            spareSensorTargets.Add(target);
        }

        public static void Clear()
        {
            spareSensorTargets.Clear();
        }
    }
}
