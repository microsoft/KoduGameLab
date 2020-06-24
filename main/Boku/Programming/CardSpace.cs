
//#define PROGRAMMING_TILE_HACK
//#define GENERATE_TILE_MAP

using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Fx;
using Boku.Common.Localization;

namespace Boku.Programming
{
    /// <summary>
    /// This class is a management/serializing class for the Programming Tiles
    /// 
    /// All Programming Elements exposed in the game as Programming Tiles are 
    /// defined in the CardSpace.Xml file and read into this class.  Many of the 
    /// exposed Tiles use the same Programming Element class, just with different 
    /// properties.  
    /// This CardSpace.Xml file allows for easy property management like changing 
    /// the Tiles text or icon without changing code.  Further, it includes 
    /// properties that allow a tile to be archived (hidden) and properties to 
    /// hint at how the tiles should be grouped.  
    /// Lastly the file includes a section to define replacements for “upgraded” 
    /// tiles and a section to define the UI groups Icon and labels.
    /// </summary>
    public class CardSpace
    {
        /// <summary>
        /// Used as our dictionary entry.  These get created up front
        /// without the texture.  The textures are created and added
        /// on the fly.
        /// </summary>
        public class CardFace
        {
            public RenderTarget2D Texture;
            public string Id;
            public string Icon;
            public string Label;
            public bool NoTexture = false;  // Set to true if we try and create a texture and fail.
                                            // This way we don't keep trying to do it again and again.

            public CardFace(string id, string icon, string label)
            {
                Id = id;
                Icon = icon;
                Label = label;
            }
        }

        #region Constants
        private const string xmlFileName = @"Content\Xml\CardSpace.xml";
        #endregion

        private static Dictionary<string, List<string>> localizationDictionary;

        protected NullSensor nullSensor = new NullSensor();
        protected NullFilter nullFilter = new NullFilter();
        protected NullSelector nullSelector = new NullSelector();
        protected NullModifier nullModifier = new NullModifier();
        protected NullActuator nullActuator = new NullActuator();
        private List<Filter> hiddenDefaults = new List<Filter>();
        private Dictionary<string, CardFace> cardFaces;

        public class Replacement
        {
            [XmlAttribute]
            public string upidOld;
            [XmlAttribute]
            public string upidNew;
        }

        public class Group : Atom
        {
            public const string RootGroup = "root";

            /// <summary>
            /// The minimum number of elements in the group before expansion becomes an option.
            /// </summary>
            [XmlAttribute]
            public int minExpandElements = int.MaxValue;

            /// <summary>
            /// Use the alternative carousel.
            /// </summary>
            [XmlAttribute]
            public bool notPieSelector = false;

            /// <summary>
            /// The name of this group's parent group.
            /// </summary>
            [XmlAttribute]
            public string group = RootGroup;

            public Group Clone()
            {
                Group clone = new Group();
                CopyTo(clone);
                return clone;
            }

            protected void CopyTo(Group clone)
            {
                base.CopyTo(clone);
                clone.minExpandElements = this.minExpandElements;
                clone.notPieSelector = this.notPieSelector;
                clone.group = this.group;
            }

            public override void OnLoad()
            {
            }
        }

        public const string IconPrefix = "icon";

        protected GetFont FontLabel;

        [Flags]
        public enum CardType
        {
            None = 0 << 0,
            Sensor = 1 << 0,
            Filter = 1 << 1,
            Selector = 1 << 2,
            Modifier = 1 << 3,
            Actuator = 1 << 4,
            All = Sensor | Filter | Selector | Modifier | Actuator
        }

        [
            XmlArrayItem(typeof(GotObjectSensor)),
            XmlArrayItem(typeof(BumpSensor)),
            XmlArrayItem(typeof(NullSensor)),
            XmlArrayItem(typeof(SightSensor)),
            XmlArrayItem(typeof(SoundSensor)),
            XmlArrayItem(typeof(GamePadSensor)),
            XmlArrayItem(typeof(MicrobitSensor)),
            XmlArrayItem(typeof(KeyBoardSensor)),
            XmlArrayItem(typeof(MouseSensor)),
            XmlArrayItem(typeof(TouchSensor)),
            XmlArrayItem(typeof(TerrainSensor)),
            XmlArrayItem(typeof(WaterSensor)),
            XmlArrayItem(typeof(PathSensor)),
            XmlArrayItem(typeof(TimerSensor)),
            XmlArrayItem(typeof(GameScoredSensor)),
            XmlArrayItem(typeof(HealthSensor)),
            XmlArrayItem(typeof(GivenObjectSensor)),
            XmlArrayItem(typeof(HoldingObjectSensor)),
            XmlArrayItem(typeof(MissileHitSensor)),
            XmlArrayItem(typeof(HeldSensor)),
            XmlArrayItem(typeof(BeamedSensor)),
            XmlArrayItem(typeof(InspectedSensor)),
            XmlArrayItem(typeof(ScannedSensor)),
            XmlArrayItem(typeof(EndofPathSensor)),
        ]
        public List<Sensor> SensorPieces = new List<Sensor>();
        [XmlIgnore]
        public Dictionary<string, Sensor> SensorDict = new Dictionary<string, Sensor>();
        public NullSensor NullSensor
        {
            get
            {
                return this.nullSensor;
            }
        }

        [
            XmlArrayItem(typeof(AnythingFilter)),
            XmlArrayItem(typeof(ClassificationFilter)),
            XmlArrayItem(typeof(DistanceFilter)),
            XmlArrayItem(typeof(RelativeFilter)),
            XmlArrayItem(typeof(CountFilter)),
            XmlArrayItem(typeof(ClusterFilter)),
            XmlArrayItem(typeof(SoundFilter)),
            XmlArrayItem(typeof(GamePadButtonFilter)),
            XmlArrayItem(typeof(GamePadStickFilter)),
            XmlArrayItem(typeof(GamePadTriggerFilter)),
            XmlArrayItem(typeof(KeyBoardKeyFilter)),
            XmlArrayItem(typeof(MouseFilter)),
            XmlArrayItem(typeof(TouchGestureFilter)),
            XmlArrayItem(typeof(TouchButtonFilter)),
            XmlArrayItem(typeof(MicrobitTiltFilter)),
            XmlArrayItem(typeof(MicrobitButtonFilter)),
            XmlArrayItem(typeof(MicrobitPinFilter)),
            XmlArrayItem(typeof(MicrobitShakeFilter)),
            XmlArrayItem(typeof(GUIButtonFilter)),
            XmlArrayItem(typeof(RotationFilter)),
            XmlArrayItem(typeof(TerrainFilter)),
            XmlArrayItem(typeof(TimerFilter)),
            XmlArrayItem(typeof(WaterFilter)),
            XmlArrayItem(typeof(TimerTriggerFilter)),
            XmlArrayItem(typeof(SaidFilter)),
            XmlArrayItem(typeof(ScoreFilter)),
            XmlArrayItem(typeof(ScoreTriggerFilter)),
            XmlArrayItem(typeof(RandomFilter)),
            XmlArrayItem(typeof(PercentFilter)),
            XmlArrayItem(typeof(TeamFilter)),
            XmlArrayItem(typeof(PlayerFilter)),
            XmlArrayItem(typeof(NothingFilter)),
            XmlArrayItem(typeof(ScoreCompareFilter)),
            XmlArrayItem(typeof(ScoreBucketFilter)),
            XmlArrayItem(typeof(HealthCompareFilter)),
            XmlArrayItem(typeof(HealthFilter)),
            XmlArrayItem(typeof(MaxHealthFilter)),
            XmlArrayItem(typeof(DirectionFilter)),
            XmlArrayItem(typeof(NotFilter)),
            XmlArrayItem(typeof(MeFilter)),
            XmlArrayItem(typeof(DeadFilter)),
            XmlArrayItem(typeof(SquashedFilter)),
            XmlArrayItem(typeof(NumericFilter)),
            XmlArrayItem(typeof(SettingsFilter)),
        ]
        public List<Filter> FilterPieces = new List<Filter>();
        [XmlIgnore]
        public Dictionary<string, Filter> FilterDict = new Dictionary<string, Filter>();
        public NullFilter NullFilter
        {
            get
            {
                return this.nullFilter;
            }
        }

        [
            XmlArrayItem(typeof(ClosestSelector)),
            XmlArrayItem(typeof(ObjectRelativeSelector)),
            XmlArrayItem(typeof(FollowWaypointsSelector)),
            XmlArrayItem(typeof(TowardClosestSelector)),
            XmlArrayItem(typeof(AwayFromAllSelector)),
            XmlArrayItem(typeof(WanderSelector)),
            XmlArrayItem(typeof(CircleSelector)),
            XmlArrayItem(typeof(SpinSelector)),
            XmlArrayItem(typeof(MoveUpDownSelector)),
            XmlArrayItem(typeof(MoveUpSelector)),
            XmlArrayItem(typeof(MoveDownSelector)),
            XmlArrayItem(typeof(MoveLeftRightSelector)),
            XmlArrayItem(typeof(TurnSelector)),
        ]
        public List<Selector> SelectorPieces = new List<Selector>();
        [XmlIgnore]
        public Dictionary<string, Selector> SelectorDict = new Dictionary<string, Selector>();
        public NullSelector NullSelector
        {
            get
            {
                return this.nullSelector;
            }
        }

        [
            XmlArrayItem(typeof(SpeedModifier)),
            XmlArrayItem(typeof(ResetModifier)),
            XmlArrayItem(typeof(ProjectileModifier)),
            XmlArrayItem(typeof(PitchModifier)),
            XmlArrayItem(typeof(TurnModifier)),
            XmlArrayItem(typeof(DirectionModifier)),
            XmlArrayItem(typeof(CircleDistanceModifier)),
            XmlArrayItem(typeof(GlowColorModifier)),
            XmlArrayItem(typeof(WaypointColorModifier)),
            XmlArrayItem(typeof(MakeObjectModifier)),
            XmlArrayItem(typeof(MakeColorModifier)),
            XmlArrayItem(typeof(RocketColorModifier)),
            XmlArrayItem(typeof(ObjectModifier)),
            XmlArrayItem(typeof(ColorModifier)),
            XmlArrayItem(typeof(PathColorModifier)),
            XmlArrayItem(typeof(TaskModifier)),
            XmlArrayItem(typeof(ScoreModifier)),
            XmlArrayItem(typeof(RandomModifier)),
            XmlArrayItem(typeof(PercentModifier)),
            XmlArrayItem(typeof(HealthModifier)),
            XmlArrayItem(typeof(MaxHealthModifier)),
            XmlArrayItem(typeof(TeamModifier)),
            XmlArrayItem(typeof(TrackingModifier)),
            XmlArrayItem(typeof(PayloadVerbModifier)),
            XmlArrayItem(typeof(ExpressModifier)),
            XmlArrayItem(typeof(SoundModifier)),
            XmlArrayItem(typeof(ConstraintModifier)),
            XmlArrayItem(typeof(PronounModifier)),
            XmlArrayItem(typeof(PlayerModifier)),
            XmlArrayItem(typeof(ScoreBucketModifier)),
            XmlArrayItem(typeof(OnceModifier)),
            XmlArrayItem(typeof(WaterModifier)),
            XmlArrayItem(typeof(MicrobitPatternModifier)),
            XmlArrayItem(typeof(OnOffModifier)),
            XmlArrayItem(typeof(NumericModifier)),
            XmlArrayItem(typeof(SettingsModifier)),
        ]
        public List<Modifier> ModifierPieces = new List<Modifier>();
        [XmlIgnore]
        public Dictionary<string, Modifier> ModifierDict = new Dictionary<string, Modifier>();
        public NullModifier NullModifier
        {
            get
            {
                return this.nullModifier;
            }
        }


        [
            XmlArrayItem(typeof(VerbActuator)),
            XmlArrayItem(typeof(NullActuator)),
            XmlArrayItem(typeof(MovementActuator)),
            XmlArrayItem(typeof(TurnActuator)),
        ]
        public List<Actuator> ActuatorPieces = new List<Actuator>();
        [XmlIgnore]
        public Dictionary<string, Actuator> ActuatorDict = new Dictionary<string, Actuator>();
        public NullActuator NullActuator
        {
            get
            {
                return this.nullActuator;
            }
        }

        [XmlArrayItem(typeof(Replacement))]
        public List<Replacement> UpidMap = new List<Replacement>();
        [XmlIgnore]
        public Dictionary<string, Replacement> UpidDict = new Dictionary<string, Replacement>();

        [XmlArrayItem(typeof(Group))]
        public List<Group> GroupPieces = new List<Group>();
        [XmlIgnore]
        public Dictionary<string, Group> GroupDict = new Dictionary<string, Group>();

        [XmlIgnore]
        public static CardSpace Cards;


        public void Pieces(CardType cardType, List<ProgrammingElement> results)
        {
            results.Clear();
            if ((cardType & CardType.Sensor) != 0)
                results.AddRange(SensorDict.Values);
            if ((cardType & CardType.Filter) != 0)
                results.AddRange(FilterDict.Values);
            if ((cardType & CardType.Actuator) != 0)
                results.AddRange(ActuatorDict.Values);
            if ((cardType & CardType.Selector) != 0)
                results.AddRange(SelectorDict.Values);
            if ((cardType & CardType.Modifier) != 0)
                results.AddRange(ModifierDict.Values);
        }

        /// <summary>
        /// Find sensor by string and return a clone of it.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Sensor GetSensor(string idObj)
        {
            if (idObj == null) { return null; }
            Sensor sensor;
            idObj = MapUpid(idObj);

            SensorDict.TryGetValue(idObj, out sensor);
            if (sensor != null)
            {
                return sensor.Clone() as Sensor;
            }
            return null;
        }

        /// <summary>
        /// Find filter by string and return a clone of it.
        /// 
        /// TODO (****) Why is this implemented as a list instead of 
        /// a dictionary?  Given that there are over 500 filters in
        /// the system, the constant, serial searching seems, um, less
        /// than optimal...
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Filter GetFilter(string idObj)
        {
            if (idObj == null || idObj == "null") 
            { 
                return null; 
            }

            Filter filter;
            idObj = MapUpid(idObj);
            FilterDict.TryGetValue(idObj, out filter);
            if (filter != null)
            {
                return filter.Clone() as Filter;
            }

            // At load time, we don't have the NamedFilters in CardSpace so
            // we'll end up here.  Recreate the filter from it's upid.
            if (idObj.StartsWith("filter.named."))
            {
                string names = idObj.Substring(13);
                int index = names.IndexOf(".");

                Debug.Assert(index > 0, "Must have a badly formed name.  Why?");

                if (index > 0)
                {
                    string name = names.Substring(0, index);
                    string actorName = names.Substring(index + 1);
                    filter = NamedFilter.RegisterInCardSpace(name, actorName);

                    return filter;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Find selector by string and return a clone of it.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Selector GetSelector(string idObj)
        {
            if (idObj == null) { return null; }
            Selector selector;
            idObj = MapUpid(idObj);

            SelectorDict.TryGetValue(idObj, out selector);
            if(selector != null)
            {
                return selector.Clone() as Selector;
            }
            return null;
        }

        /// <summary>
        /// Find modifier by string and return a clone of it.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Modifier GetModifier(string idObj)
        {
            if (idObj == null) { return null; }
            Modifier modifier;
            idObj = MapUpid(idObj);

            ModifierDict.TryGetValue(idObj, out modifier);
            if(modifier != null)
            {
                return modifier.Clone() as Modifier;
            }
            return null;
        }

        /// <summary>
        /// Find a modifier by id and remove it.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns>the modifier if found, else null</returns>
        public Modifier RemoveModifier(string idObj)
        {
            if (idObj == null) { return null; }
            Modifier modifier = null;
            idObj = MapUpid(idObj);

            ModifierDict.TryGetValue(idObj, out modifier);
            if(modifier != null)
            {
                ModifierDict.Remove(idObj);
                return modifier;
            }
            return null;
        }

        /// <summary>
        /// Find actuator by string and return a clone of it.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Actuator GetActuator(string idObj)
        {
            if (idObj == null) { return null; }
            Actuator actuator;
            idObj = MapUpid(idObj);

            ActuatorDict.TryGetValue(idObj, out actuator);
            if(actuator != null)
            {
                return actuator.Clone() as Actuator;
            }
            return null;
        }

        /// <summary>
        /// Lookup a group by string, and return that instance.
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Group GetGroup(string idObj)
        {
            if (idObj == null) { return null; }
            Group group;
            idObj = MapUpid(idObj);

            GroupDict.TryGetValue(idObj, out group);

            return group;
        }

        /// <summary>
        /// Returns the Atom associated with the id from any group.
        /// 
        /// TODO Deep down inside this is cloning the tile.  This happens
        /// for the tiles EVERY SINGLE FRAME!!!!
        /// </summary>
        /// <param name="idObj"></param>
        /// <returns></returns>
        public Atom GetAtom(string idObj)
        {
            Atom atom = null;

            atom = GetSensor(idObj);
            if (atom != null)
                return atom;

            atom = GetFilter(idObj);
            if (atom != null)
                return atom;

            atom = GetSelector(idObj);
            if (atom != null)
                return atom;

            atom = GetModifier(idObj);
            if (atom != null)
                return atom;

            atom = GetActuator(idObj);
            if (atom != null)
                return atom;

            atom = GetGroup(idObj);
            if (atom != null)
                return atom;

            return atom;
        }   // end of GetAtom()

        public bool IsWhenCard(string id)
        {
            ProgrammingElement element = GetAtom(id) as ProgrammingElement;
            if (element != null)
            {
                if ((element as Sensor != null) || (element as Filter != null))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDoCard(string id)
        {
            ProgrammingElement element = GetAtom(id) as ProgrammingElement;
            if (element != null)
            {
                if ((element as Actuator != null) || (element as Modifier != null) ||
                    (element as Selector != null))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return the cached list of hidden defaults. Don't mess with this,
        /// just look at it and let it go.
        /// </summary>
        public List<Filter> HiddenDefaults
        {
            get { return hiddenDefaults; }
        }

        /// <summary>
        /// Returns the texture associated with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Texture2D CardFaceTexture(string id)
        {
            CardFace cardFace = null;
            // Do we already have the card?
            if (cardFaces.ContainsKey(id))
            {
                cardFace = cardFaces[id];
            }
            else
            {
                // Create a new entry.  Looks like this happens for add item groups.
                cardFace = new CardFace(id, null, null);
                cardFaces.Add(id, cardFace);
            }

            // Do we need to create the texture?
            if (cardFace.Texture == null || cardFace.Texture.IsContentLost /* || cardFace.Texture.GraphicsDevice.IsDisposed */)
            {
                if (!cardFace.NoTexture)
                {
                    CreateCardFaceTexture(cardFace);
                    if (cardFace.Texture == null)
                    {
                        // Ok, we failed to create the texture.  Mark this so we don't keep trying.
                        cardFace.NoTexture = true;
                    }
                }
            }
            
            return cardFace.Texture;
        }

        /// <summary>
        /// Returns the help description associated with the given id.
        /// </summary>
        /// <param name="id"></param>
        public string GetHelpDescription(string id)
        {
            Atom atom = GetAtom(id);
            string desc = Strings.Localize("helpCard.noDescription");
            if (atom != null && atom.description != null)
            {
                desc = atom.description;
            }

            return desc;
        }   // end of GetHelpDescription()

        /// <summary>
        /// Returns the label associated with the given id.
        /// </summary>
        /// <param name="id"></param>
        public string GetLabel(string id)
        {
            Atom atom = GetAtom(id);
            string label = Strings.Localize("helpCard.noLabel");
            if (atom != null && atom.label != null)
            {
                label = atom.label;
            }

            return label;
        }   // end of GetLabel()

        public void UncacheCardFace(string id)
        {
            CardFace cardFace;
            if (cardFaces.TryGetValue(id, out cardFace))
            {
                DeviceResetX.Release(ref cardFace.Texture);
                cardFaces.Remove(id);
            }
        }

        public void CacheCardFace(string id, string icon, string label, bool noLabelIcon)
        {
            // If we already have this one.
            if (cardFaces.ContainsKey(id))
            {
                return;
            }

            CardFace cardFace = new CardFace(id, icon, label);
            cardFaces.Add(id, cardFace);

            if (noLabelIcon)
            {
                string iconId = IconPrefix + id.Remove(0, id.IndexOf("."));
                cardFace = new CardFace(iconId, icon, null);
                cardFaces.Add(iconId, cardFace);
            }
        }
        public void CacheCardFace(string id, string icon, string label)
        {
            CardFace cardFace = new CardFace(id, icon, label);
            cardFaces.Add(id, cardFace);
        }

        public void CreateButtonTexture(CardFace cardFace)
        {
            // Get big version of texture.
            string id = cardFace.Id;
            id = id.Remove(id.Length - 7);
            Texture2D bigTexture = CardFaceTexture(id);

            if (bigTexture == null)
            {
                return;
            }

            //
            // Create a version of this texture for use in text.
            //
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            // If content was lost we need to recreate the texture
            // but we don't want to reallocate it.
            if (cardFace.Texture == null)
            {
                cardFace.Texture = new RenderTarget2D(device, 64, 64);
            }

            SpriteBatch batch = KoiLibrary.SpriteBatch;
            
            InGame.SetRenderTarget(cardFace.Texture);
            device.Clear(Color.Transparent);

            batch.Begin();
            batch.Draw(bigTexture, new Rectangle(0, 0, 64, 64), Color.White);
            batch.End();

            InGame.RestoreRenderTarget();
        }  // end of CreateButtonTexture()

        public void CreateCardFaceTexture(CardFace cardFace)
        {
            if (cardFace.Id.EndsWith("_button"))
            {
                CreateButtonTexture(cardFace);
                return;
            }

            Texture2D texture = null;
            Texture2D overlay = null;
            string imagefile = cardFace.Icon;

            // if an icon is defined use it, otherwise revert to the id
            if (imagefile == null)
            {
                imagefile = cardFace.Id;
            }
            if (IsPlayerFilterCard(cardFace.Id))
            {
                int player = int.Parse(cardFace.Id.Substring("filter.player".Length));

                cardFace.Label = GamePadInput.GetGamerTag((PlayerIndex)player - 1);
                overlay = GamePadInput.GetControllerIcon((PlayerIndex)player - 1);
            }
            else if (IsPlayerModifierCard(cardFace.Id))
            {
                int player = int.Parse(cardFace.Id.Substring("modifier.player.".Length));

                cardFace.Label = GamePadInput.GetGamerTag((PlayerIndex)player - 1);
                overlay = GamePadInput.GetControllerIcon((PlayerIndex)player - 1);
            }

            if (texture == null)
            {
                if (imagefile.StartsWith(".."))
                {
                    texture = KoiLibrary.LoadTexture2D(@"Textures\" + imagefile.Substring(3));
                }
                else
                {
                    try
                    {
                        texture = KoiLibrary.LoadTexture2D(@"Textures\Tiles\" + imagefile);
                    }
                    catch
                    {
                        // Nothing to see here, move along.
                    }
                }

                if (texture != null)
                {
                    Debug.Assert(texture.Width == 128 && texture.Height == 128);
                }
            }

            // If there's nothing to work with, leave it null.
            if (texture == null)
            {
                return;
            }

            SpriteBatch batch = KoiLibrary.SpriteBatch;

            // Now render the card face.
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // If we've already got a rt it must be because the content was
            // lost and we need to recreate it so don't allocate a new one.
            if (cardFace.Texture == null)
            {
                cardFace.Texture = new RenderTarget2D(device, 128, 128);
            }

            Rectangle destRect = new Rectangle(0, 0, cardFace.Texture.Width, cardFace.Texture.Height);
            InGame.SetRenderTarget(cardFace.Texture);

#if PROGRAMMING_TILE_HACK
            // Set background color based on tile type.
            InGame.Clear(Color.HotPink);
            if (imagefile != null)
            {
                Color green = new Color(203, 223, 166);
                Color blue = new Color(155, 187, 225);
                if (imagefile.StartsWith("sensor") || imagefile.StartsWith("filter") || imagefile.StartsWith("scored") || imagefile.StartsWith("always"))
                {
                    InGame.Clear(green);
                }
                else if (imagefile.StartsWith("actuator")
                    || imagefile.StartsWith("modifier")
                    || imagefile.StartsWith("sound")
                    || imagefile.StartsWith("group")
                    || imagefile.StartsWith("selector")
                    || imagefile.StartsWith("zap")
                    || imagefile.StartsWith("pop")
                    || imagefile.StartsWith("boom")
                    || imagefile.StartsWith("vanish")
                    || imagefile.StartsWith("once")
                    )
                {
                    InGame.Clear(blue);
                }
                else
                {
                    InGame.Clear(green);
                }
            }

#else
            InGame.Clear(Color.Transparent);
#endif

#if GENERATE_TILE_MAP
            if (label != null)
            {
                // Set background color based on tile type.
                Color green = new Color(203, 223, 166);
                Color blue = new Color(155, 187, 225);
                if (id.StartsWith("sensor") || id.StartsWith("filter"))
                {
                    InGame.Clear(green);
                }
                else if (id.StartsWith("actuator") || id.StartsWith("modifier") || id.StartsWith("selector"))
                {
                    InGame.Clear(blue);
                }
                else
                {
                    InGame.Clear(Color.HotPink);
                }
            }
#endif

            // Draw the card face.
            batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            {
                batch.Draw(texture, destRect, Color.White);
            }
            batch.End();

            // Draw card label.
            if (!string.IsNullOrEmpty(cardFace.Label))
            {
                string label = cardFace.Label;
                label = TextHelper.FilterInvalidCharacters(label);
                Vector2 labelSize = FontLabel().MeasureString(label) + new Vector2(3, 2);

                Vector2 position = new Vector2(0, (int)(cardFace.Texture.Height - (labelSize.Y * 0.9f)));

                RectangleF rect = new RectangleF(position.X, position.Y, cardFace.Texture.Width, labelSize.Y);
                Vector2 scale = Vector2.One;
                if (labelSize.X > cardFace.Texture.Width)
                {
                    // Label is too wide, figure out scaling to shrink to fit.
                    scale.X = cardFace.Texture.Width / labelSize.X;
                }
                else
                {
                    // Label fits, figure out offset to center.
                    position.X = (int)((cardFace.Texture.Width - labelSize.X)/2.0f);
                }

#if NETFX_CORE
                if (scale.X == 1.0f)
                {
                    TextHelper.DrawStringNoBatch(FontLabel, label, position, Color.Black);
                }
                else
                {
                    // The label is too wide so we want to render it to another 
                    // texture and then shrink that texture onto our tile.
                    RenderTarget2D tmpRT = Shared.RenderTarget256_256;
                    InGame.SetRenderTarget(tmpRT);
                    InGame.Clear(Color.Transparent);

                    TextHelper.DrawStringNoBatch(FontLabel, label, Vector2.Zero, Color.Black);

                    // Restore tile texture.  Beause we're swapping rendertargets we effectively
                    // have to start over with rendering the tile.  So clear and redraw tthe icon.
                    InGame.SetRenderTarget(cardFace.Texture);
                    InGame.Clear(Color.Transparent);

                    batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                    {
                        // Draw the icon texture.
                        batch.Draw(texture, destRect, Color.White);

                        // Apply the label shrunk to fit the dstRect.
                        Rectangle dstRect = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                        Rectangle srcRect = new Rectangle(0, 0, (int)labelSize.X, (int)labelSize.Y);
                        batch.Draw(tmpRT, dstRect, srcRect, Color.White);
                    }
                    batch.End();
                }
#else
                SysFont.StartBatch(null);
                SysFont.DrawString(label, position, rect, FontLabel().systemFont, Color.Black, scale, outlineColor: new Color(248, 248, 248), outlineWidth: 1.5f);
                SysFont.EndBatch();
#endif
            }

            // Draw overlay if needed.
            if (overlay != null)
            {
                batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                {
                    batch.Draw(overlay, Vector2.Zero, new Color(1.0f, 1.0f, 1.0f, 0.75f));
                }
                batch.End();
            }

            // Restore backbuffer.
            InGame.RestoreRenderTarget();

#if PROGRAMMING_TILE_HACK
            string foo = id;
            foo += ".png";
            foo = Path.GetFileName(foo);
            foo = Path.Combine("Tiles_" + Localizer.LocalLanguage + @"\", foo);
            Storage4.TextureSaveAsPng(cardFace, foo);
#endif
        }   // end of CreateCardFaceTexture(

        private void Serialize()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CardSpace));
            //TextWriter writer = new StreamWriter(filename + @"UserProfile");
            Stream stream = Storage4.OpenWrite(xmlFileName);
            serializer.Serialize(stream, this);
            //writer.Close();
            Storage4.Close(stream);
        }

        static private void Deserialize()
        {
            Stream stream = Storage4.OpenRead(xmlFileName, StorageSource.TitleSpace);
            XmlSerializer serializer = new XmlSerializer(typeof(CardSpace));
            Cards = (CardSpace)serializer.Deserialize(stream);
            Storage4.Close(stream);

            Cards.OnLoad();
        }

        private static bool audioLoaded = false;

        private void OnLoad()
        {
            // Copy all cards from lists to dictionaries.  After this
            // point we shouldn't use the lists any more.
            // TODO (****) Clear them just to be sure?
            foreach (Sensor sensor in SensorPieces)
            {
                SensorDict.Add(sensor.upid, sensor);
            }
            foreach (Filter filter in FilterPieces)
            {
                FilterDict.Add(filter.upid, filter);
            }
            foreach (Actuator actuator in ActuatorPieces)
            {
                ActuatorDict.Add(actuator.upid, actuator);
            }
            foreach (Selector selector in SelectorPieces)
            {
                SelectorDict.Add(selector.upid, selector);
            }
            foreach (Modifier modifier in ModifierPieces)
            {
                ModifierDict.Add(modifier.upid, modifier);
            }
            foreach (Group group in GroupPieces)
            {
                GroupDict.Add(group.upid, group);
            }
            foreach (Replacement replacement in UpidMap)
            {
                UpidDict.Add(replacement.upidOld, replacement);
            }

            LoadLocalizedInfo();

            // Localize before we modify them
            foreach (Sensor sensor in SensorDict.Values)
                Localize(sensor);
            foreach (Filter filter in FilterDict.Values)
                Localize(filter);
            foreach (Actuator actuator in ActuatorDict.Values)
                Localize(actuator);
            foreach (Selector selector in SelectorDict.Values)
                Localize(selector);
            foreach (Modifier modifier in ModifierDict.Values)
                Localize(modifier);
            foreach (Group group in GroupDict.Values)
                Localize(group);

            FixupGroups();
            BuildSoundFilters();
            CacheHiddenDefaults();

            // Only ever create audio taxonomy once.
            if (!audioLoaded)
            {
                BokuGame.Audio.Build(Cards.GroupDict, Cards.ModifierDict);
                audioLoaded = true;
            }

            // Post-load fixup
            foreach (Sensor sensor in SensorDict.Values)
                sensor.OnLoad();
            foreach (Filter filter in FilterDict.Values)
                filter.OnLoad();
            foreach (Actuator actuator in ActuatorDict.Values)
                actuator.OnLoad();
            foreach (Selector selector in SelectorDict.Values)
                selector.OnLoad();
            foreach (Modifier modifier in ModifierDict.Values)
                modifier.OnLoad();
            foreach (Group group in GroupDict.Values)
                group.OnLoad();
        
            // Fix up names for settings tiles.
            foreach (Filter filter in FilterDict.Values)
            {
                SettingsFilter f = filter as SettingsFilter;
                if (f != null)
                {
                    int index = f.upid.LastIndexOf('.');
                    f.name = f.upid.Substring(index + 1);
                }
            }
            foreach (Modifier modifier in ModifierDict.Values)
            {
                SettingsModifier m = modifier as SettingsModifier;
                if (m != null)
                {
                    int index = m.upid.LastIndexOf('.');
                    m.name = m.upid.Substring(index + 1);
                }
            }
        
        }   // end of OnLoad()

        /// <summary>
        /// Initializes the "localizationDictionary" with localized information if available and default
        /// information otherwise.
        /// </summary>
        private static void LoadLocalizedInfo()
        {
            // Grab the default localization information
            localizationDictionary = Localizer.ReadToDictionary(Path.Combine(Localizer.DefaultLanguageDir, LocalizationResourceManager.CardsResource.Name), "sensor");

            // Is our run-time local language different from the default?
            if (!Localizer.IsLocalDefault)
            {
                var localPath = Localizer.LocalLanguageDir;

                // Do we have a directory for the local language?
                if (localPath != null)
                {
                    var localFile = Path.Combine(localPath, LocalizationResourceManager.CardsResource.Name);

                    if (Storage4.FileExists(localFile, StorageSource.All))
                    {
                        var localDict = Localizer.ReadToDictionary(localFile, "sensor");

                        var keys = localizationDictionary.Keys.ToArray();
                        foreach (var k in keys)
                            if (localDict.ContainsKey(k))
                            {
                                if (Localizer.ShouldReportMissing && !localDict[k].Any((s) => !localizationDictionary[k].Contains(s)))
                                {
                                    Localizer.ReportIdentical(LocalizationResourceManager.CardsResource.Name, k);
                                }

                                localizationDictionary[k] = localDict[k];
                            }
                            else
                                Localizer.ReportMissing(LocalizationResourceManager.CardsResource.Name, k);
                    }
                    else
                        Localizer.ReportMissing(LocalizationResourceManager.CardsResource.Name, "CAN'T FIND FILE!");
                }
                else
                    Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
            }
        }
        /// <summary>
        /// Attempts to fill the card's description and label with localized information.
        /// </summary>
        private static void Localize(Atom atom)
        {
            var upid = atom.upid;

            //HACK: Sound filters borrow their localization info from modifiers. To do this
            // properly, we'll have to generalize info that is common to filters & modifiers.
            if (atom is SoundFilter)
            {
                Debug.Assert(upid.StartsWith("filter"), "Umm... Why does a 'SoundFilter' upid not start with 'filter'?");

                const int filterLength = 6; //"filter".Length == 6
                upid = upid.Remove(0, filterLength);
                upid = upid.Insert(0, "modifier");
            }

            List<string> labels;
            if (localizationDictionary.TryGetValue(upid + ".label", out labels))
            {
                Debug.Assert(labels.Count == 1, "This card has multiple labels!: " + upid);

                atom.label = labels[0];
            }

            List<string> descriptions;
            if (localizationDictionary.TryGetValue(upid, out descriptions))
            {
                Debug.Assert(descriptions.Count == 1, "This card has multiple descriptions!: " + upid);

                atom.description = descriptions[0];
            }
        }

        public static string Localize(string upid)
        {
            string result = "";

            List<string> descriptions;
            if (localizationDictionary.TryGetValue(upid, out descriptions))
            {
                Debug.Assert(descriptions.Count == 1, "This card has multiple descriptions!: " + upid);

                result = descriptions[0];
            }

            return result;
        }

        private void FixupGroups()
        {
            List<ProgrammingElement> pieces = new List<ProgrammingElement>();
            Cards.Pieces(CardType.All, pieces);

            for (int i = 0; i < pieces.Count; ++i)
            {
                ProgrammingElement progElement = pieces[i] as ProgrammingElement;
                progElement.groupObj = Cards.GetGroup(progElement.group);
                Debug.Assert(progElement.groupObj != null);
            }
        }

        public static void LoadContent(bool immediate)
        {
            if (Cards != null)
                return;

            Deserialize();
        }

        private static RenderTarget2D blurRT = null;
        private static GaussianFilter filter = null;

        public static void InitDeviceResources(GraphicsDevice device)
        {
            if (Cards.cardFaces != null && Cards.cardFaces.Count > 0)
                return;

            Cards.FontLabel = SharedX.GetCardLabel;

            Cards.cardFaces = new Dictionary<string, CardFace>();

            Cards.cardFaces.Add(ProgrammingElement.upidNull, null); // add the empty one

            // Create a temp rendertarget and filter for lores blur pass.
            blurRT = new RenderTarget2D(KoiLibrary.GraphicsDevice,
                                        128,
                                        128,
                                        false,
                                        SurfaceFormat.Color,
                                        DepthFormat.None);
            SharedX.GetRT("CardSpace:blurRT", blurRT);
            filter = new GaussianFilter();
            filter.LoadContent(true);
            filter.InitDeviceResources(KoiLibrary.GraphicsDevice);

            foreach(ProgrammingElement item in Cards.SensorDict.Values)
            {
                Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
            }
            foreach (ProgrammingElement item in Cards.FilterDict.Values)
            {
                Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
            }
            foreach (ProgrammingElement item in Cards.SelectorDict.Values)
            {
                Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
            }
            foreach (ProgrammingElement item in Cards.ModifierDict.Values)
            {
                Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
            }
            foreach (ProgrammingElement item in Cards.ActuatorDict.Values)
            {
                Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
            }
            foreach (Group group in Cards.GroupDict.Values)
            {
                Cards.CacheCardFace(group.upid, group.icon, group.label, group.noLabelIcon);
            }


#if GENERATE_TILE_MAP
            List<ProgrammingElement> pieces = new List<ProgrammingElement>();
            Cards.Pieces(CardType.All, pieces);

            // AllTiles HTML
            StreamWriter writer = Storage4.OpenStreamWriter(@"tilemap\alltiles.html", Encoding.UTF8);
            writer.WriteLine(
@"<html>
<head>
<title>All Tiles</title>
<style>
img { margin-left: 20px; margin-bottom: 20px; }
</style>
</head>
<body>
");

            int cols = 0;
            int rows = 0;
            foreach (var piece in pieces)
            {
                string entry = MakeTileEntry(piece, false);
                if (entry.Length > 0)
                {
                    writer.WriteLine("{0}", entry);
                }
            }


            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Close();



            // TileGroups HTML
            writer = Storage4.OpenStreamWriter(@"tilemap\tilegroups.html", Encoding.UTF8);
            writer.WriteLine(
@"<html>
<head>
<title>Tile Groups</title>
<style>
img { margin-left: 20px; margin-bottom: 20px; }
p { page-break-before: always; }
</style>
</head>
<body>
");

            Dictionary<string, List<ProgrammingElement>> tilesByGroup = new Dictionary<string, List<ProgrammingElement>>();
            foreach (var g in Cards.GroupPieces)
            {
                tilesByGroup[GetGroupPath(g)] = new List<ProgrammingElement>();
            }

            foreach (var p in pieces)
            {
                tilesByGroup[GetGroupPath(p.groupObj)].Add(p);
            }

            int sections = 0;
            foreach (var path in tilesByGroup.Keys)
            {
                if (tilesByGroup[path].Count == 0)
                {
                    continue;
                }

                if (sections > 0)
                {
                    writer.WriteLine("<p/>");
                }

                writer.WriteLine("<hr/>");
                writer.WriteLine("<h1>Group: {0}</h1>", path);

                cols = 0;
                rows = 0;
                foreach (var piece in tilesByGroup[path])
                {
                    string entry = MakeTileEntry(piece, false);
                    if (entry.Length > 0)
                    {
                        writer.WriteLine("{0}", entry);
                        if (cols % 4 == 3)
                        {
                            writer.WriteLine("<br/>");
                            rows++;
                            if (rows == 6) {
                                writer.WriteLine("<p/>");
                                rows = 0;
                            }
                        }
                        cols++;
                    }
                }
                sections++;
            }

            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Close();
#endif



#if PROGRAMMING_TILE_HACK2
            // Hack to create dictionary of all tiles.  Images are written out to cur directory.
            int page = 0;

            PageDump("sensor", ref page);
            PageDump("filter", ref page);
            PageDump("actuator", ref page);
            PageDump("selector", ref page);
            PageDump("modifier", ref page);
            PageDump("group", ref page);

#endif

        }

#if GENERATE_TILE_MAP
        private static string MakeTileEntry(ProgrammingElement item, bool includePath = true)
        {
            StringBuilder sb = new StringBuilder();
            if (item.label != null && !item.archived && !item.hiddenDefault && Cards.textures.ContainsKey(item.upid))
            {
                string url = GetImageDataUrl(item);
                sb.AppendFormat("<img src='{0}'/>", url);
            }
            return sb.ToString();
        }

        private static string MakeVerticalTable(params string[] p)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<table>\n");

            foreach (var v in p)
            {
                sb.Append(String.Format("<tr><td>{0}</td></tr>\n", v));
            }

            sb.Append("</table>\n");

            return sb.ToString();
        }

        private static string MakeHorizontalTable(params string[] p)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<table><tr>\n");

            foreach (var v in p)
            {
                sb.Append(String.Format("<td>{0}</td>\n", v));
            }

            sb.Append("</tr></table>\n");

            return sb.ToString();
        }


        private static string GetGroupPath(Group group)
        {
            string path = "";

            do
            {
                path = group.label + "/" + path;
                group = Cards.GetGroup(group.group);
            }
            while (group.group != Group.RootGroup);

            return path;
        }

        private static string GetImageDataUrl(ProgrammingElement item)
        {
#if false
            Texture2D image = textures[item.upid];
            string url = "data:image/png;base64,";
            MemoryStream stream = new MemoryStream();
            image.SaveAsPng(stream, image.Width, image.Height);
            url += Convert.ToBase64String(stream.ToArray(), Base64FormattingOptions.None);
            stream.Close();
#else
            WriteTilePng(@"tilemap\images", item);
            string url = "images/" + item.upid + ".png";
            return url;
#endif
        }

        private static void WriteTilePng(string path, ProgrammingElement item)
        {
            string foo = item.upid;
            foo += ".png";
            foo = Path.GetFileName(foo);
            foo = Path.Combine(path, foo);
            Storage4.TextureSaveAsPng(Cards.textures[item.upid], foo);
        }
#endif

#if PROGRAMMING_TILE_HACK2
        static void PageDump(string filter, ref int page)
        {
            int width = 512;
            int height = 1024;
            RenderTarget2D rt = new RenderTarget2D(
                KoiLibrary.GraphicsDevice,
                width, height,
                false,  // Mipmaps
                SurfaceFormat.Color,
                DepthFormat.None,
                1, // Samples
                RenderTargetUsage.PlatformContents);

            TextBlob blob = new TextBlob(Shared.GetGameFont24Bold, "", 512);
            blob.LineSpacingAdjustment = 6;

            InGame.SetRenderTarget(rt);
            InGame.Clear(Color.White);

            int margin = 20;
            Vector2 pos = new Vector2(margin, margin);

            foreach (ProgrammingElement element in list)
            {
                // Render next button/string combo.
                string foo = key.Substring(0, key.Length - 6);
                blob.RawText = "<" + foo + "> < " + foo + " >";
                blob.RenderWithButtons(pos, Color.Black, false);

                pos.Y += blob.TotalSpacing;

                // At end of current page?
                if (pos.Y + margin + blob.TotalSpacing > 1024)
                {
                    InGame.SetRenderTarget(null);
                    Storage4.TextureSaveAsPng(rt, "Tiles" + page.ToString("00") + ".png");
                    ++page;
                    Debug.Print("page " + page.ToString());
                    InGame.SetRenderTarget(rt);
                    InGame.Clear(Color.White);
                    pos.Y = margin;
                }
            }
            // Output final page.
            InGame.SetRenderTarget(null);
            Storage4.TextureSaveAsPng(rt, "Tiles" + page.ToString("00") + ".png");
        }   // end of PageDump

        void ButtonDump(ref int page)
        {
            int width = 512;
            int height = 1024;
            RenderTarget2D rt = new RenderTarget2D(
                KoiLibrary.GraphicsDevice,
                width, height,
                false,  // Mipmaps
                SurfaceFormat.Color,
                DepthFormat.None,
                1, // Samples
                RenderTargetUsage.PlatformContents);

            TextBlob blob = new TextBlob(Shared.GetGameFont24Bold, "", 512);
            blob.LineSpacingAdjustment = 6;
            Dictionary<string, Texture2D>.KeyCollection keys = Cards.textures.Keys;

            InGame.SetRenderTarget(rt);
            InGame.Clear(Color.White);

            int margin = 20;
            Vector2 pos = new Vector2(margin, margin);

            foreach (string key in keys)
            {
                if (key.EndsWith("_button"))
                {
                    // Render next button/string combo.
                    string foo = key.Substring(0, key.Length - 6);
                    blob.RawText = "<" + foo + "> < " + foo + " >";
                    blob.RenderWithButtons(pos, Color.Black, false);

                    pos.Y += blob.TotalSpacing;

                    // At end of current page?
                    if (pos.Y + margin + blob.TotalSpacing > 1024)
                    {
                        InGame.SetRenderTarget(null);
                        Storage4.TextureSaveAsPng(rt, "Tiles" + page.ToString("00") + ".png");
                        ++page;
                        Debug.Print("page " + page.ToString());
                        InGame.SetRenderTarget(rt);
                        InGame.Clear(Color.White);
                        pos.Y = margin;
                    }
                }
            }
            // Output final page.
            InGame.SetRenderTarget(null);
            Storage4.TextureSaveAsPng(rt, "Tiles" + page.ToString("00") + ".png");
        }   // end of PageDump
#endif


        /// <summary>
        /// Called to rebuild player cards once the gamer profiles are available.
        /// Profiles are _not_ available during CardSpace's InitDevResources, when the
        /// rest of the cards are built, so they are made with the default player icons.
        /// </summary>
        public static void ReCachePlayerCards()
        {
            // This seems to work just fine as is.  This has to be causing all kinds of 
            // extra processing that just seems like a huge waste of time.
            return;
            /*
            for (int i = 0; i < Cards.FilterPieces.Count; ++i)
            {
                ProgrammingElement item = Cards.FilterPieces[i] as ProgrammingElement;
                if (IsPlayerFilterCard(item.upid))
                {
                    Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
                }
            }
            for (int i = 0; i < Cards.ModifierPieces.Count; ++i)
            {
                ProgrammingElement item = Cards.ModifierPieces[i] as ProgrammingElement;
                if (IsPlayerModifierCard(item.upid))
                {
                    Cards.CacheCardFace(item.upid, item.icon, item.label, item.noLabelIcon);
                }
            }
            */
        }

        private static bool IsPlayerFilterCard(string id)
        {
            return id.StartsWith("filter.player");
        }

        private static bool IsPlayerModifierCard(string id)
        {
            return id.StartsWith("modifier.player.");
        }

        public static void UnloadContent()
        {
            if (Cards == null)
                return;

            if (Cards.cardFaces != null)
            {
                foreach(KeyValuePair<string, CardFace> kvp in CardSpace.Cards.cardFaces)
                {
                    DeviceResetX.Release(ref kvp.Value.Texture);
                }

                Cards.cardFaces.Clear();
            }

            Cards = null;
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Maps old upids to new values for back compat.
        /// </summary>
        /// <param name="upidOld"></param>
        /// <returns></returns>
        protected string MapUpid(string upidOld)
        {
            string upidNew = upidOld;
            Replacement replacement;

            UpidDict.TryGetValue(upidOld, out replacement);
            if (replacement != null)
            {
                upidNew = replacement.upidNew;
            }

            return upidNew;
        }

        private void CacheHiddenDefaults()
        {
            hiddenDefaults.Clear();
            foreach (Filter filter in FilterDict.Values)
            {
                if ((filter != null)
                    && filter.hiddenDefault
                    && !filter.archived)
                {
                    hiddenDefaults.Add(filter);
                }
            }
        }

        /// <summary>
        /// We build the SoundFilters out of the SoundModifiers, because
        /// we always want a SoundFilter for each SoundModifier, and the SoundModifiers
        /// have all the info a SoundFilter needs (and more).
        /// </summary>
        private void BuildSoundFilters()
        {
            foreach (ProgrammingElement progElem in ModifierDict.Values)
            {
                SoundModifier soundMod = progElem as SoundModifier;
                if (soundMod != null)
                {
                    SoundFilter soundFilter = new SoundFilter(soundMod);

                    // Build side-by-side groups matching the modifier audio taxonomy, but enforce rigid grouping on the filter groups.
                    Group soundGroup = null;
                    if (soundMod.group != Group.RootGroup)
                    {
                        string soundGroupName = soundMod.group + ".filter";
                        soundGroup = Cards.GetGroup(soundGroupName);
                        if (soundGroup == null)
                        {
                            soundGroup = soundMod.groupObj.Clone();
                            soundGroup.upid += ".filter";
                            if (soundGroup.group != Group.RootGroup)
                                soundGroup.group += ".filter";
                            soundGroup.minExpandElements = 0;
                            GroupDict.Add(soundGroup.upid, soundGroup);
                        }
                    }

                    soundFilter.group = soundGroup.upid;
                    soundFilter.groupObj = soundGroup;

                    FilterDict.Add(soundFilter.upid, soundFilter);
                }
            }
        }
    }
}
