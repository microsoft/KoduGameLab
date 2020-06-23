using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boku.Common.Xml;
using Boku.SimWorld;
using Boku.Base;
using Boku.SimWorld.Chassis;
using System.Diagnostics;

namespace Boku.Common
{
    /// <summary>
    /// Keeps all the immutable data that is shared amongst
    /// its GameActors. Use this to create GameActor instances.
    /// </summary>
    public sealed class StaticActor : ArbitraryComparable
    {
        public readonly string NonLocalizedName;
        public readonly string LocalizedName;
        public readonly string ClassificationName;
        public readonly string ClassificationRevealedName;
        public readonly string SpecialType;
        public readonly string MenuTextureFile;
        public readonly string Group;

        private string xmlFileName;
        private string xmlFileNameRevealed;
        private XmlGameActor xmlGameActor;
        private XmlGameActor xmlGameActorRevealed;
        private string modelFileName;
        private string modelRevealedFileName;
        private FBXModel model;
        private FBXModel modelRevealed;

        /// <summary>
        /// The XmlGameActor that contains most the actor-specific information
        /// </summary>
        public XmlGameActor XmlGameActor
        {
            get
            {
                // xmlGameActor must be initialized lazily because XmlGameActor
                // requires the game surfaces be loaded before it is deserialized
                // (at which point it will bind its surfaces).
                if (xmlGameActor == null)
                {
                    xmlGameActor = XmlGameActor.Deserialize(xmlFileName);
                }
                return xmlGameActor;
            }
        }

        /// <summary>
        /// The XmlGameActor that contains most the actor-specific information
        /// </summary>
        public XmlGameActor XmlGameActorRevealed
        {
            get
            {
                // xmlGameActor must be initialized lazily because XmlGameActor
                // requires the game surfaces be loaded before it is deserialized
                // (at which point it will bind its surfaces).
                if (xmlGameActorRevealed == null)
                {
                    xmlGameActorRevealed = XmlGameActor.Deserialize(xmlFileNameRevealed);
                }
                return xmlGameActorRevealed;
            }
        }

        /// <summary>
        /// The FBXModel shared by all GameActor instances
        /// </summary>
        public FBXModel Model
        {
            get
            {
                if (model == null)
                {
                    model = InitializeModelFromXml(XmlGameActor, modelFileName);
                }
                return model;
            }
        }

        /// <summary>
        /// The FBXModel of the revealed version of the actor, shared by all GameActor instances
        /// </summary>
        public FBXModel ModelRevealed
        {
            get
            {
                if (modelRevealed == null)
                {
                    modelRevealed = InitializeModelFromXml(XmlGameActorRevealed, modelRevealedFileName);
                }
                return modelRevealed;
            }
        }


        public StaticActor(XmlStaticActor xml)
        {
            NonLocalizedName = xml.NonLocalizedName;
            LocalizedName = Strings.GetActorName(NonLocalizedName);
            ClassificationName = xml.Classification;
            ClassificationRevealedName = xml.ClassificationRevealed;

            if (ClassificationRevealedName == null || ClassificationRevealedName == "")
            {
                ClassificationRevealedName = ClassificationName;
            }

            SpecialType = xml.SpecialType;
            MenuTextureFile = xml.MenuTextureFile;
            Group = xml.Group;

            modelFileName = xml.ModelFile;
            modelRevealedFileName = xml.ModelRevealedFile;
            xmlFileName = xml.XmlFile;
            xmlFileNameRevealed = xml.XmlFileRevealed;
        }

        /// <summary>
        /// Creates a new GameActor instance.
        /// </summary>
        /// <remarks>
        /// This should ONLY be called by the ActorFactory.
        /// The list of special GameActor derivatives is kept here.
        /// </remarks>
        public GameActor CreateNewInstance()
        {
            var chassis = CreateNewChassis(XmlGameActor.ChassisData.Type);

            if (SpecialType != null)
            {
                switch (SpecialType.ToLower())
                {
                    case "wisp": return new Wisp(ClassificationName, chassis, () => Model, this);
                    case "windblimp": return new WindBlimp(ClassificationName, chassis, () => Model, this);
                    case "terracannon": return new TerraCannon(ClassificationName, chassis, () => Model, this);
                    case "swimfish": return new SwimFish(ClassificationName, chassis, () => Model, this);
                    case "light": return new Light(ClassificationName, chassis, () => Model, this);
                    case "floatbot": return new FloatBot(ClassificationName, chassis, () => Model, this);
                    case "drum": return new Drum(ClassificationName, chassis, () => Model, this);
                    case "cruisemissile": return new CruiseMissile(ClassificationName, chassis, () => Model, this);
                    case "bokugreeter": return new BokuGreeter(ClassificationName, chassis, () => Model, this);
                    case "rovergreeter": return new RoverGreeter(ClassificationName, chassis, () => Model, this);
                    case "sharedidle": return new SharedIdle(ClassificationName, chassis, () => Model, this);
                    case "fan": return new Fan(ClassificationName, chassis, () => Model, this);
                    case "inkjet": return new InkJet(ClassificationName, chassis, () => Model, this);
                }
            }

            if (ModelRevealed != null)
            {
                return new GameActor(ClassificationName, ClassificationRevealedName, chassis, () => Model, () => ModelRevealed, this);
            }
            else
            {
                return new GameActor(ClassificationName, ClassificationRevealedName, chassis, () => Model, () => Model, this);
            }
        }

        private static BaseChassis CreateNewChassis(ChassisType type)
        {
            switch (type)
            {
                case ChassisType.Boat: return new BoatChassis();
                case ChassisType.Cursor: return new CursorChassis();
                case ChassisType.Cycle: return new CycleChassis();
                case ChassisType.Rover: return new RoverChassis();
                case ChassisType.DynamicProp: return new DynamicPropChassis();
                case ChassisType.FloatInAir: return new FloatInAirChassis();
                case ChassisType.Hover: return new HoverChassis();
                case ChassisType.HoverSwim: return new HoverSwimChassis();
                case ChassisType.Missile: return new MissileChassis();
                case ChassisType.Puck: return new PuckChassis();
                case ChassisType.Saucer: return new SaucerChassis();
                case ChassisType.SitAndSpin: return new SitAndSpinChassis();
                case ChassisType.StaticProp: return new StaticPropChassis();
                case ChassisType.Swim: return new SwimChassis();
                case ChassisType.Pipe: return new PipeChassis();
            }

            Debug.Assert(false, "No chassis provided!");
            return new StaticPropChassis();
        }

        /// <summary>
        /// Returns a new FBXModel.
        /// </summary>
        /// <remarks>
        /// The list of special FBXModel derivatives is kept here.
        /// </remarks>
        private static FBXModel InitializeModelFromXml(XmlGameActor xmlGameActor, string modelFileName)
        {
            FBXModel result = null;

            if (xmlGameActor != null && xmlGameActor.ModelData != null)
            {
                if (xmlGameActor.ModelData.SpecialType != null)
                {
                    switch (xmlGameActor.ModelData.SpecialType.ToLower())
                    {
                        case "bokugreetermodel":
                            //if(BokuGame.bMarsMode)
                            //    modelFileName = "Models\\Rover";
                            result = new BokuGreeterModel(modelFileName);
                            break;
                    }
                }
            }

            if (result == null)
                result = new FBXModel(modelFileName);

            result.XmlActor = xmlGameActor;

            if (xmlGameActor != null && xmlGameActor.ModelData != null)
            {
                if (xmlGameActor.ModelData.TechniqueExt != null)
                    result.TechniqueExt = xmlGameActor.ModelData.TechniqueExt;

                if (xmlGameActor.ModelData.Shininess != null)
                    result.Shininess = xmlGameActor.ModelData.Shininess.Value;
            }

            return result;
        }
    }
}
