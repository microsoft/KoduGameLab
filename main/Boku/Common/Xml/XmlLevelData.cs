
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.Fx;


namespace Boku.Common.Xml
{
    //
    // The classes with the XmlData namespace exist only to be used for Xml IO.
    // They are wrapped in their own namespace to keep them from conflicting
    // with anything.  This way we can keep the class names simple so that the 
    // Xml files themselves have reasonable tag names.
    //
    namespace XmlData
    {
        public class Actor
        {
            public Vector3 position = new Vector3();
            public float orientation = 0.0f;
            public float heightOffset = 0.0f;
            public Classification.Colors color = Classification.Colors.White;
            public bool tintable = true;
            public Guid creatableId = Guid.Empty;
            public string creatableName;
            public Brain brain;

            // Tweakable params
            public GameActor.TweakableParameters parameters = null;

            public int version;

            public string typename;
            public Actor(string typename)
            {
                this.typename = typename;
            }
            public Actor()
            {
                parameters = new GameActor.TweakableParameters();
            }

            public void ToActor(GameActor actor)
            {
                actor.Movement.Position = position;
                actor.ResetAttachmentPositions();
                // For actors that "tumble" this also resets any XY rotations to 0.
                Matrix mat = Matrix.CreateRotationZ(orientation) * Matrix.CreateTranslation(position);
                actor.Movement.SetLocalMatrixAndRotation(mat, orientation);
                actor.HeightOffsetNoLimit = heightOffset;
                actor.ClassColor = color;
                actor.CreatableId = creatableId;
                actor.DisplayNameNumber = creatableName;
                actor.Version = version;
                
                // Clone the parameters instead of calling CopyTo so that the creatable flag
                // is preserved. CopyTo deliberately excludes this this field when copying.
                actor.LocalParameters = parameters.Clone() as GameActor.TweakableParameters;
                
                /// Backward compat - heightOffset used to be stored in the shared parameters,
                /// which only worked because of a bug in SyncParameters. If there is a valid
                /// heightOffset in parameters, put it in the right place on the actor and
                /// set it to the invalid value.
                if (actor.LocalParameters.heightOffset != GameActor.TweakableParameters.kInvalidHeightOffset)
                {
                    actor.HeightOffset = actor.LocalParameters.heightOffset;
                    actor.LocalParameters.heightOffset = GameActor.TweakableParameters.kInvalidHeightOffset;
                }

                // If we don't have a brain loaded, leave the actor with its default.
                if (brain != null)
                {
                    actor.Brain = Brain.DeepCopy(brain);
                }
            }
            public void FromActor(GameActor actor)
            {
                Debug.Assert(!actor.InRecycleBin);

                position = actor.Movement.Position;
                orientation = actor.Movement.RotationZ;
                heightOffset = actor.HeightOffset;
                color = actor.ClassColor;
                creatableId = actor.CreatableId;
                creatableName = actor.DisplayNameNumber;
                version = actor.Version;
                
                // Clone the parameters instead of calling CopyTo so that the creatable flag
                // is preserved. CopyTo deliberately excludes this this field when copying.
                parameters = actor.LocalParameters.Clone() as GameActor.TweakableParameters;

                tintable = true;
                brain = Brain.DeepCopy(actor.Brain);
            }
        }

        public class Node
        {
            public Vector2 position = new Vector2();
            public float height = 0.0f;
            public bool airborne = false;
        }

        public class Edge
        {
            public int node0 = 0;
            public int node1 = 0;
            public int direction = (int)WayPoint.Edge.Direction.Both;
        }

        public class Path
        {
            [XmlElement(Type = typeof(XmlData.Node))]
            public List<XmlData.Node> nodes = null;
            [XmlElement(Type = typeof(XmlData.Edge))]
            public List<XmlData.Edge> edges = null;

            public Classification.Colors color = Classification.Colors.White;
            public string roadName = null;

            public Path()
            {
                nodes = new List<Node>();
                edges = new List<Edge>();
            }
        }

        public class WayPoints
        {
            [XmlElement(Type = typeof(XmlData.Path))]
            public List<XmlData.Path> paths = null;

            public WayPoints()
            {
                paths = new List<Path>();
            }
        }

    }   // end of namespace XmlData

    /// <summary>
    /// Handles loading of level content, ie where does Boku start, where the fruit is located,
    /// where the "props" are, etc.
    /// </summary>
    public class XmlLevelData : BokuShared.XmlData<XmlLevelData>
    {
        public XmlData.WayPoints waypoints = new XmlData.WayPoints();

        [XmlElement(Type = typeof(XmlData.Actor))]
        public List<XmlData.Actor> actor = new List<XmlData.Actor>();

        // c'tor
        public XmlLevelData()
        {
        }

        /// <summary>
        /// For testing purposes...
        /// </summary>
        public void Init()
        {
            //XmlData.Fruit plumpkin = new XmlData.Fruit();
            //fruit.Add(plumpkin);
        }

        /// <summary>
        /// Copies the current state of the objects in
        /// the game to this XmlLevelData object.
        /// </summary>
        /// <param name="gameThingList"></param>
        public void FromGame(List<GameThing> gameThingList)
        {
            waypoints.paths.Clear();

            // Get the WayPoint data.
            for (int i = 0; i < WayPoint.Paths.Count; i++)
            {
                WayPoint.Path path = (WayPoint.Path)WayPoint.Paths[i];
                XmlData.Path p = new XmlData.Path();

                // Add path to list.
                waypoints.paths.Add(p);

                // Fill in path data.
                p.color = path.Color;
                p.roadName = path.RoadName;

                // Copy node info.
                for (int j = 0; j < path.Nodes.Count; j++)
                {
                    WayPoint.Node node = (WayPoint.Node)path.Nodes[j];
                    XmlData.Node n = new XmlData.Node();
                    n.position = new Vector2(node.Position.X, node.Position.Y);
                    n.height = node.Height;
                    p.nodes.Add(n);
                }

                // Copy edge info.
                for (int j = 0; j < path.Edges.Count; j++)
                {
                    WayPoint.Edge edge = (WayPoint.Edge)path.Edges[j];
                    XmlData.Edge e = new XmlData.Edge();
                    e.node0 = path.Nodes.IndexOf(edge.Node0);
                    e.node1 = path.Nodes.IndexOf(edge.Node1);
                    e.direction = (int)edge.Dir;
                    p.edges.Add(e);
                }
            }

            // Loop through the gameThingList putting things into the
            // right Xml array for serialization.
            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameActor srcActor = gameThingList[i] as GameActor;

                if (gameThingList[i].GetType() == typeof(Boku.SimWorld.CursorThing))
                {
                    // This space intentionally left blank.
                }
                else if (gameThingList[i].GetType() == Type.GetType("Boku.Fireball"))
                {
                    // This space intentionally left blank.
                }
                else if (gameThingList[i].GetType() == Type.GetType("Boku.CruiseMissile"))
                {
                    // This space intentionally left blank.
                }
                else if (actor != null)
                {
                    XmlData.Actor dstActor = new XmlData.Actor(srcActor.StaticActor.NonLocalizedName);
                    dstActor.FromActor(srcActor);
                    this.actor.Add(dstActor);
                }
                else
                {
                    Debug.Assert(false, @"Trying to serialize unrecognized type.");
                }

            }
        }   // end of FromGame()

        /// <summary>
        /// Hand off copies the objects in this XmlLevelData object
        /// into AddThing.
        /// </summary>
        /// <param name="gameThingList"></param>
        public void ToGame(InGame.AddThingDelegate AddThing)
        {
            WayPoint.ClearPaths();

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Copy local data to sim classes.

            // Copy waypoint data to sim classes.

            // In with the new.
            for (int i = 0; i < waypoints.paths.Count; i++)
            {
                XmlData.Path p = (XmlData.Path)waypoints.paths[i];
                WayPoint.Path path = new WayPoint.Path(p.color);
                path.RoadName = p.roadName;

                // Create nodes.
                for (int j = 0; j < p.nodes.Count; j++)
                {
                    XmlData.Node n = (XmlData.Node)p.nodes[j];
                    WayPoint.Node node = new WayPoint.Node(path, new Vector3(n.position, n.height));
                }

                // Create edges.
                for (int j = 0; j < p.edges.Count; j++)
                {
                    XmlData.Edge e = (XmlData.Edge)p.edges[j];
                    WayPoint.Node n0 = (WayPoint.Node)path.Nodes[e.node0];
                    WayPoint.Node n1 = (WayPoint.Node)path.Nodes[e.node1];
                    WayPoint.CreateNewEdge(n0, n1, (WayPoint.Edge.Direction)e.direction);
                }

                path.RecalcHeights(onLoad: true);
                if (path.Road != null)
                {
                    path.Road.Build();
                }
            }

            for (int i = 0; i < actor.Count; ++i)
            {
                XmlData.Actor srcActor = (XmlData.Actor)actor[i];
                GameActor dstActor = ActorFromString(srcActor.typename);
                if (dstActor != null)
                {
                    srcActor.ToActor(dstActor);

                    dstActor = (GameActor)AddThing(dstActor);
                    if (dstActor != null)
                    {
                        // Init InsideGlassWalls.
                        // TODO (****) Right now we're doing this by checking the height of the terrain.
                        // We should also be able to do this by checking the material index BUT it appears
                        // that when we erase terrain we only set the height to 0 without resetting the material.
                        // I think...
                        dstActor.Chassis.InsideGlassWalls = Terrain.GetTerrainAndPathHeight(dstActor.Movement.Position) > 0;
                    }
                }
            }

        }   // end of ToGame()

        /// <summary>
        /// Init the internal structures from existing data and save to file.
        /// </summary>
        /// <param name="bokuBot"></param>
        /// <param name="fruitList"></param>
        public void WriteToXml(string filename, List<GameThing> gameThingList)
        {
            FromGame(gameThingList);
            Save(filename, XnaStorageHelper.Instance);
        }   // end of XmlLevelData WriteToXml()

        protected GameActor ActorFromString(string actorName)
        {
            GameActor ret = null;

            var staticActor = ActorManager.GetActor(actorName);

            if (staticActor == null)
            {
                var splitName = actorName.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var nsQualName = splitName[0];
                splitName = nsQualName.Split('.');
                actorName = splitName[splitName.Length - 1];
                staticActor = ActorManager.GetActor(actorName);
            }

            if (staticActor != null)
            {
                ret = ActorFactory.Create(staticActor);
            }
            else
            {
                Debug.Assert(false, "Why can't find the actor for: '" + actorName + "'!!");
            }

            return ret;
        }

        /// <summary>
        /// Reads the "stuff" file from the given XmlWorldData.
        /// </summary>
        /// <param name="xmlWorldData"></param>
        /// <param name="gameThingList"></param>
        /// <returns>true on success, false on failure</returns>
        public bool ReadFromXml(XmlWorldData xmlWorldData, InGame.AddThingDelegate AddThing)
        {
            bool success = true;

            // Clear any existing paths.
            WayPoint.ClearPaths();

            // Read the Xml file into local data.
            XmlLevelData data;
            try
            {
                data = Load(BokuGame.Settings.MediaPath + xmlWorldData.stuffFilename, XnaStorageHelper.Instance);
            }
            catch
            {
#if !NETFX_CORE
                Debug.Print("Fail to read file " + BokuGame.Settings.MediaPath + xmlWorldData.stuffFilename);
#endif
                data = null;
                success = false;
            }

            if (data != null)
            {
                this.waypoints = data.waypoints;
                this.actor = data.actor;

                data = null;
                
                ToGame(AddThing);
            }

            return success;
        }   // end of XmlLevelData ReadFromXml()

    }   // end of class XmlLevelData

}   // end of namespace Boku.SimWorld
