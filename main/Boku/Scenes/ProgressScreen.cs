using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Text;

using Boku.Base;
using Boku.Input;
using Boku.Common;

namespace Boku
{
    public class ProgressScreen : INeedsDeviceReset
    {
        // Amount of time before we consider the operation to be long-running.
        public static double LongRunningThresholdSecs = 1.0f;

        static ProgressScreen instance;

        CommandMap commandMap;
        List<ProgressOperation> operations;


        public static ProgressScreen Instance
        {
            get { return instance; }
        }

        static List<ProgressOperation> Operations
        {
            get { return instance.operations; }
        }

        public ProgressScreen()
        {
            instance = this;

            operations = new List<ProgressOperation>();
            commandMap = new CommandMap(@"App.ProgressScreen");
        }

        public void Render()
        {
            int opCount = ProgressScreen.Operations.Count;

            // It may be valid for this list to briefly be empty before the ProgressScreen deactivates.
            if (opCount == 0)
                return;

            ProgressOperation op = ProgressScreen.Operations[opCount - 1];

            if (op.LongRunning || op.AlwaysShow)
            {
                string message = op.Message;
                if (message != null)
                {
                    GetFont Font = SharedX.GetGameFont24;
                    SpriteBatch batch = KoiLibrary.SpriteBatch;

                    int textWidth = (int)Font().MeasureString(message).X;
                    int screenWidth = KoiLibrary.GraphicsDevice.Viewport.Width;
                    int textX = (screenWidth - textWidth) / 2;
                    int textY = KoiLibrary.GraphicsDevice.Viewport.TitleSafeArea.Top;
                    batch.Begin();
                    TextHelper.DrawString(Font, message, new Vector2(textX, textY), Color.White);
                    batch.End();
                }
            }
        }

        public void Update()
        {
        }

        public static ProgressOperation RegisterOperation()
        {
            ProgressOperation op = new ProgressOperation();

            if (instance != null)
            {
                if (instance.operations.Count == 0)
                    CommandStack.Push(instance.commandMap);

                instance.operations.Add(op);
            }

            return op;
        }

        public static void UnregisterOperation(ProgressOperation op)
        {
            if (instance != null)
            {
                if (instance.operations.Contains(op))
                    instance.operations.Remove(op);

                if (instance.operations.Count == 0)
                    CommandStack.Pop(instance.commandMap);
            }
        }

        public void LoadContent(bool immediate)
        {
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }

    public class ProgressOperation
    {
        string message;
        object userState;
        bool alwaysShow;
        double startTime;


        public bool LongRunning
        {
            get { return Time.WallClockTotalSeconds - startTime > ProgressScreen.LongRunningThresholdSecs; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public object UserState
        {
            get { return userState; }
            set { userState = value; }
        }

        public bool AlwaysShow
        {
            get { return alwaysShow; }
            set { alwaysShow = value; }
        }

        public ProgressOperation()
        {
            startTime = Time.WallClockTotalSeconds;
        }

        public void Complete()
        {
            ProgressScreen.UnregisterOperation(this);
        }
    }
}
