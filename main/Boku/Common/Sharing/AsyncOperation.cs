//#define LOG
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace Boku.Common.Sharing
{
    public delegate void AsyncOpCallback(AsyncOperation op);

    public enum AsyncOperations
    {
        Null,                   // A no-op transaction. Useful for making a callback in the next frame.
        Timer,                  // A delayed no-op transaction. Useful for making a callback at some point in the future.
    }

    /// <summary>
    /// Used to be tied to Live.  Now this is just a way to delay an operation by a frame or two.
    /// </summary>
    public abstract class AsyncOperation : IDisposable
    {
        AsyncOperations op;
        AsyncOpCallback callback;
        object param0;
        object param1;
        object pwner;
        bool active;
        bool started;
        bool completed;
        bool succeeded;
        bool queued;

        /// <summary>
        /// The LIVE operation this structure manages.
        /// </summary>
        public AsyncOperations Op { get { return op; } }

        /// <summary>
        /// The callback specified in the constructor.
        /// </summary>
        public AsyncOpCallback Callback { get { return callback; } }

        /// <summary>
        /// The callback parameter specified in the constructor.
        /// </summary>
        public object Param0 { get { return param0; } }

        /// <summary>
        /// Another callback parameter specified in the constructor.
        /// </summary>
        public object Param1 { get { return param1; } }

        /// <summary>
        /// The object that started this operation.
        /// </summary>
        public object Pwner { get { return pwner; } }

        /// <summary>
        /// Whether this operation requires the user to first be signed in to LIVE. Default is true.
        /// </summary>
        public bool RequiresSignIn { get; set; }

        /// <summary>
        /// Whether this operation is in progress.
        /// </summary>
        public bool Active { get { return active; } }

        /// <summary>
        /// True if this op was ever started. Once true, never goes false.
        /// </summary>
        public bool Started { get { return started; } }

        /// <summary>
        /// Whether this operation has completed.
        /// </summary>
        public bool Completed { get { return completed; } }

        /// <summary>
        /// Whether this operation was successful.
        /// </summary>
        public bool Succeeded { get { return succeeded; } }

        /// <summary>
        /// State keeping variable used by LiveManager. No touchy.
        /// </summary>
        public bool Queued
        {
            get { return queued; }
            set { queued = value; }
        }

        /// <summary>
        /// A user-defined object.
        /// </summary>
        public object Tag { get; set; }


        public AsyncOperation(AsyncOperations op, AsyncOpCallback callback, object param0 = null, object param1 = null, object pwner = null)
        {
            this.op = op;
            this.callback = callback;
            this.param0 = param0;
            this.param1 = param1;
            this.pwner = pwner;

            RequiresSignIn = true;
        }

        /// <summary>
        /// Queue the operation for execution as soon as possible.
        /// </summary>
        public void Queue()
        {
            Queue(false);
        }

        /// <summary>
        /// Queue the operation and optionally start it immediately (in this frame).
        /// </summary>
        /// <param name="startImmediately"></param>
        public void Queue(bool startImmediately)
        {
            Log("Queueing " + GetType().Name);

#if NETFX_CORE
            Debug.Assert(false, "What are we doing with LiveManager?");
#else
            //LiveManager._QueueOperation(this, startImmediately);
#endif
        }

        /// <summary>
        /// Do not call this function. Consider it private to LiveManager. Call Queue() instead.
        /// </summary>
        internal void _Start()
        {
            Log("Starting " + GetType().Name);

            Debug.Assert(!queued);
            Debug.Assert(!active);
            Debug.Assert(!completed);

            started = true;
            active = true;

            try
            {
                IStart();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Complete(false);
            }
        }

        public void Complete(bool success)
        {
            Log("Completing(" + success + ") " + GetType().Name);

            // WARNING: This function is typically (but not always!) called from LIVE's asynchronous context,
            // and not from the main thread. Be careful what you do from here.

            Debug.Assert(!queued);
            Debug.Assert(!completed);

            active = false;

            completed = true;

            succeeded = success;

            // Queue ourselves for completion by the main thread.
#if NETFX_CORE
            Debug.Assert(false, "What are we doing with LiveManager?");
#else
            //LiveManager._CompleteOperation(this);
#endif
        }

        private void Log(string msg)
        {
#if LOG
            Debug.WriteLine(msg);
#endif
        }


        protected abstract void IStart();

        public abstract void Dispose();

        /// <summary>
        /// Do not call this function. Move along, move along...
        /// </summary>
        internal virtual void Update()
        {
        }
    }

    /// <summary>
    /// Static class for handling frame delayed callbacks.
    /// This would probably make more sense as an extention of the 
    /// Time class but that would require more rework.
    /// </summary>
    public static class AsyncOps
    {
        static List<FrameDelayedOperation> ops = new List<FrameDelayedOperation>();

        /// <summary>
        /// Needs to be called once per frame.
        /// Activates all the queued up ops.
        /// </summary>
        public static void Update()
        {
            for(int i=0; i<ops.Count; i++)
            {
                FrameDelayedOperation op = ops[i];
                op.Callback(op);
            }
            ops.Clear();
        }

        public static void Enqueue(FrameDelayedOperation op)
        {
            ops.Add(op);
        }
    }

}
