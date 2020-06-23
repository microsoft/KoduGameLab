using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.Input
{
    public class CommandMap
    {
        public static CommandMap Empty = new CommandMap( "empty" );

        [XmlAttribute]
        public string name;

        protected object eventTarget;

        [XmlArrayItem(typeof(VirtualButton)),
            XmlArrayItem(typeof(GamePadAButton)),
            XmlArrayItem(typeof(GamePadBButton)),
            XmlArrayItem(typeof(GamePadXButton)),
            XmlArrayItem(typeof(GamePadYButton)),
            XmlArrayItem(typeof(GamePadRightShoulderButton)),
            XmlArrayItem(typeof(GamePadLeftShoulderButton)),
            XmlArrayItem(typeof(GamePadRightStickButton)),
            XmlArrayItem(typeof(GamePadLeftStickButton)),
            XmlArrayItem(typeof(GamePadBackButton)),
            XmlArrayItem(typeof(GamePadStartButton)),
            XmlArrayItem(typeof(GamePadDownButton)),
            XmlArrayItem(typeof(GamePadLeftButton)),
            XmlArrayItem(typeof(GamePadRightButton)),
            XmlArrayItem(typeof(GamePadUpButton)),
            XmlArrayItem(typeof(GamePadDownExButton)),
            XmlArrayItem(typeof(GamePadDownLeftExButton)),
            XmlArrayItem(typeof(GamePadLeftExButton)),
            XmlArrayItem(typeof(GamePadDownRightExButton)),
            XmlArrayItem(typeof(GamePadRightExButton)),
            XmlArrayItem(typeof(GamePadUpRightExButton)),
            XmlArrayItem(typeof(GamePadUpExButton)),
            XmlArrayItem(typeof(GamePadUpLeftExButton)),
            XmlArrayItem(typeof(GamePadLeftTrigger)),
            XmlArrayItem(typeof(GamePadRightTrigger)),
            XmlArrayItem(typeof(GamePadLeftThumbStick)),
            XmlArrayItem(typeof(GamePadRightThumbStick)),
            XmlArrayItem(typeof(KeyboardCommand)),
            XmlArrayItem(typeof(KeyboardButton))]
//            XmlArrayItem(typeof(ChatPadCommand))]
        public List<InputCommand> commands = new List<InputCommand>();

        [XmlIgnore]
        public InputCommand this[string id]
        {
            get
            {
                for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
                {
                    InputCommand command = this.commands[indexCommand];
                    if (command.id == id)
                    {
                        return command;
                    }
                }
                return null;
            }
            set
            {
                value.id = id;
                for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
                {
                    InputCommand command = this.commands[indexCommand];
                    if (command.id == id)
                    {
                        this.commands[indexCommand] = value;
                        return;
                    }
                }
                this.commands.Add(value);
            }
        }

        public CommandMap()
        {
        }
        public CommandMap(string name)
        {
            this.name = name;
        }
        public void Add(string id, InputCommand command)
        {
            command.id = id;
            this.commands.Add(command);
        }
        public void Add(InputCommand command)
        {
            Debug.Assert(command.id.Length != 0);
            this.commands.Add(command);
        }
        public void Add(CommandMap map)
        {
            for (int indexCommand = 0; indexCommand < map.commands.Count; indexCommand++)
            {
                InputCommand command = map.commands[indexCommand];
                Debug.Assert(command != null);
                this.commands.Add(command);
            }
        }

        public void Remove(InputCommand command)
        {
            this.commands.Remove(command);
        }
        public void Remove(CommandMap map)
        {
            for (int indexCommand = 0; indexCommand < map.commands.Count; indexCommand++)
            {
                InputCommand command = map.commands[indexCommand];
                this.commands.Remove(command);
            }
        }

        public void Update()
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                if (CommandStack.Constraint == null || CommandStack.Constraint.UsableCommand(command.id))
                {
                    command.Update();
                }
            }
        }
        public void Sync( CommandMap prevMap )
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                bool clone = false;

                InputCommand command = this.commands[indexCommand];
                if (prevMap != null)
                {
                    // if we have a previous active map
                    // check if this inputCommand was in it already
                    clone = prevMap.commands.Contains(command);
                }
                // do not call sync on an inputCommand that was just active
                if (!clone && (command != null))
                {
                    command.Sync();
                }
            }
        }
        public void Reset()
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                command.Reset();
            }
        }
        
        /// It appears that on the 360, unknown types are added in as objects, whereas
        /// on the PC, they are filtered out. This function is to 
        protected void RemoveUnknowns()
        {
        }

        protected const string MapFolder = @"\Content\Xml\CommandMaps\";
        public void Serialize(string filepath)
        {
            string pathName = MapFolder + filepath;
            XmlSerializer serializer = new XmlSerializer(typeof(CommandMap));
            Stream stream = Storage4.OpenWrite(pathName);
            serializer.Serialize(stream, this);
            Storage4.Close(stream);
        }
/*
        /// <summary>
        /// Due to Compact Framework not supporting Delegate.CreateDelegate, these classes
        /// were created to work around that issue but still provide similiar functionality
        /// There are five versions for each of the type input events
        /// </summary>
        protected class DelegateBindShim
        {
            protected object eventTarget;
            protected MethodInfo method;

            private DelegateBindShim(MethodInfo method, object eventTarget)
            {
                this.eventTarget = eventTarget;
                this.method = method;
            }

            private void Invoke(Object sender, EventArgs args)
            {
                object[] invokeParams = new object[] { sender, args };
                this.method.Invoke(this.eventTarget, invokeParams);
            }

            public static Delegate CreateDelegate(MethodInfo method, object eventTarget)
            {
                DelegateBindShim del = new DelegateBindShim(method, eventTarget);
                InputCommandButtonDelegate result = new InputCommandButtonDelegate( del.Invoke );

                return result;
            }
        }

        /// <summary>
        /// Due to Compact Framework not supporting Delegate.CreateDelegate, these classes
        /// were created to work around that issue but still provide similiar functionality
        /// There are five versions for each of the type input events
        /// </summary>
        protected class DelegateBindShimFloat
        {
            protected object eventTarget;
            protected MethodInfo method;

            private DelegateBindShimFloat(MethodInfo method, object eventTarget)
            {
                this.eventTarget = eventTarget;
                this.method = method;
            }

            private void Invoke( float value)
            {
                object[] invokeParams = new object[] { value };
                this.method.Invoke(this.eventTarget, invokeParams);
            }

            public static InputCommandTriggerDelegate CreateDelegate(MethodInfo method, object eventTarget)
            {
                DelegateBindShimFloat del = new DelegateBindShimFloat(method, eventTarget);
                InputCommandTriggerDelegate result = new InputCommandTriggerDelegate(del.Invoke);

                return result;
            }
        }
        /// <summary>
        /// Due to Compact Framework not supporting Delegate.CreateDelegate, these classes
        /// were created to work around that issue but still provide similiar functionality
        /// There are five versions for each of the type input events
        /// </summary>
        protected class DelegateBindShimVector2
        {
            protected object eventTarget;
            protected MethodInfo method;

            private DelegateBindShimVector2(MethodInfo method, object eventTarget)
            {
                this.eventTarget = eventTarget;
                this.method = method;
            }

            private void Invoke( Vector2 value )
            {
                object[] invokeParams = new object[] { value};
                this.method.Invoke(this.eventTarget, invokeParams);
            }

            public static InputCommandStickDelegate CreateDelegate(MethodInfo method, object eventTarget)
            {
                DelegateBindShimVector2 del = new DelegateBindShimVector2(method, eventTarget);
                InputCommandStickDelegate result = new InputCommandStickDelegate(del.Invoke);

                return result;
            }
        }
        /// <summary>
        /// Due to Compact Framework not supporting Delegate.CreateDelegate, these classes
        /// were created to work around that issue but still provide similiar functionality
        /// There are five versions for each of the type input events
        /// </summary>
        protected class DelegateBindShimKey
        {
            protected object eventTarget;
            protected MethodInfo method;

            private DelegateBindShimKey(MethodInfo method, object eventTarget)
            {
                this.eventTarget = eventTarget;
                this.method = method;
            }

            private void Invoke(Keys value)
            {
                object[] invokeParams = new object[] { value };
                this.method.Invoke(this.eventTarget, invokeParams);
            }

            public static InputKeyCommandEvent CreateDelegate(MethodInfo method, object eventTarget)
            {
                DelegateBindShimKey del = new DelegateBindShimKey(method, eventTarget);
                InputKeyCommandEvent result = new InputKeyCommandEvent(del.Invoke);

                return result;
            }
        }
        /// <summary>
        /// Due to Compact Framework not supporting Delegate.CreateDelegate, these classes
        /// were created to work around that issue but still provide similiar functionality
        /// There are five versions for each of the type input events
        /// </summary>
        protected class DelegateBindShimChar
        {
            protected object eventTarget;
            protected MethodInfo method;

            private DelegateBindShimChar(MethodInfo method, object eventTarget)
            {
                this.eventTarget = eventTarget;
                this.method = method;
            }

            private void Invoke(Char value)
            {
                object[] invokeParams = new object[] { value };
                this.method.Invoke(this.eventTarget, invokeParams);
            }

            public static InputCharCommandEvent CreateDelegate(MethodInfo method, object eventTarget)
            {
                DelegateBindShimChar del = new DelegateBindShimChar(method, eventTarget);
                InputCharCommandEvent result = new InputCharCommandEvent(del.Invoke);

                return result;
            }
        }  
*/
        const string BindToken = "Bind";
        protected void LateBindEvents()
        {
#if NETFX_CORE
            Debug.Assert(false, "Is any of this actually used???");
#else
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                if (command != null)
                {

                    Type type = command.GetType();
                    Type matchType = Type.GetType("System.String");
                    FieldInfo[] fieldinfos = type.GetFields();

                    for (int indexFields = 0; indexFields < fieldinfos.Length; indexFields++)
                    {
                        FieldInfo field = fieldinfos[indexFields];
                        Type fieldType = field.FieldType;
                        if (fieldType == matchType && field.Name.StartsWith(BindToken))
                        {
                            string eventMethodName = (string)field.GetValue(command);
                            if (eventMethodName != null)
                            {
                                Type eventTargetType = this.eventTarget.GetType();
                                // helpful to see what the methods are
                                //                            MethodInfo[] methodTargetInfos = eventTargetType.GetMethods();
                                MethodInfo methodTarget = eventTargetType.GetMethod(eventMethodName);
                                if (methodTarget != null)
                                {
                                    string eventName = field.Name.Substring(BindToken.Length);
                                    EventInfo eventSource = type.GetEvent(eventName);
                                    if (eventSource != null)
                                    {
                                        Type eventSourceType = eventSource.EventHandlerType;
#if !false
                                        Delegate eventHandler = Delegate.CreateDelegate(eventSourceType, this.eventTarget, methodTarget);
#else
                                    // This seems to work with the latest release (GS2.0).
                                    // Due to Compact Framework not supporting Delegate.CreateDelegate
                                    // we have to instance a shim delegate class too provide the functionality
                                    // based upon the event type
                                    Delegate eventHandler;
                                    switch (eventSourceType.Name)
                                    {
                                        case "InputCommandChangeVector2Event":
                                            eventHandler = DelegateBindShimVector2.CreateDelegate(methodTarget, this.eventTarget);
                                            break;
                                        case "InputCommandChangeFloatEvent":
                                            eventHandler = DelegateBindShimFloat.CreateDelegate(methodTarget, this.eventTarget);
                                            break;
                                        case "InputCommandEvent":
                                            eventHandler = DelegateBindShim.CreateDelegate(methodTarget, this.eventTarget);
                                            break;
                                        case "InputKeyCommandEvent":
                                            eventHandler = DelegateBindShimKey.CreateDelegate(methodTarget, this.eventTarget);
                                            break;
                                        case "InputCharCommandEvent":
                                            eventHandler = DelegateBindShimChar.CreateDelegate(methodTarget, this.eventTarget);
                                            break;
                                        default:
                                            throw new System.Exception("event source type not supported");
                                    }
#endif
                                        MethodInfo addHandler = eventSource.GetAddMethod();
                                        Object[] invokeParams = { eventHandler };
                                        addHandler.Invoke(command, invokeParams);
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// This method is used to create and load a command map xml file and 
        /// map any events to the given eventTarget object
        /// </summary>
        /// <param name="eventTarget"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        static public CommandMap Deserialize( object eventTarget, string filepath )
        {
            CommandMap profile = Deserialize(filepath);
            if (profile != null)
            {
                profile.eventTarget = eventTarget;
                profile.LateBindEvents();
            }
            return profile;
        }

        static protected CommandMap Deserialize(string filepath)
        {
            string oldPathName = Storage4.TitleLocation + MapFolder + filepath;
            string pathName = MapFolder + filepath;
            CommandMap profile;
//            try
            {
                Stream stream = Storage4.OpenRead(pathName, StorageSource.TitleSpace);
                XmlSerializer serializer = new XmlSerializer(typeof(CommandMap));
                profile = (CommandMap)serializer.Deserialize(stream);
                Storage4.Close(stream);
            }
/*
            catch (FileNotFoundException ex)
            {
                profile = new CommandMap();
                profile.Serialize(pathName);
            }
*/
            return profile;
        }

    }
}
