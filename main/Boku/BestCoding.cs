// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#region Defines
/// #defines in UPPER_CASE
#if DEBUG
#define REALLY_REALLY_DEBUG
#endif // DEBUG
#endregion DEFINES

/// Use using statements to avoid having to qualify names where possible.
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Common;


/// <summary>
/// Sample code to demonstrate "best practices" layout for Boku C# files. Start
/// with a highlevel summary. 
/// 
/// Formatting:
/// -----------
/// Based on the idea that any consistent formatting is more readable than a jumble of different
/// styles, formatting is generally to be left at the dev studio defaults.
/// 
/// Comments:
/// ---------
/// Broad comments that pertain to the entire class go here, in the class description.
/// A perceived charter of the class is helpful for someone else considering whether
/// to extend this class or build functionality into another.
/// Avoid getting into specifics of the implementation, or saying anything redundant
/// with the code within the class. Use this section for describing the intent of the
/// code.
/// This is also a good place to concisely describe how other classes should interface
/// with this class. 
/// Make all comments in as proper English as you can manage, including spelling and 
/// punctuation. This helps in reading as well as search.
/// Keep humor to a minimum in comments, you never know who will read them.
/// 
/// Naming:
/// ------
/// Keep one class per file, and create a subfolder for every namespace. The exceptions
/// are that child classes, where small and trivial may remain in the same file. If it gets
/// much beyond a struct, moving to another file using "partial".
/// 
/// Prefix interface names with "I", e.g. public interface IDislikeYou {.
/// 
/// See more naming notes in situ, like at a sample member variable name.
/// 
/// Spelling:
/// ---------
/// Also watch your spelling with variable names. If someone that searched for 20 minutes
/// on Finalize() comes to your door with a bat wondering why you spelled it Finialize(),
/// yell all you want but don't expect support.
/// 
/// </summary>
namespace Boku.SimWorld
{
    public class BestCoding
    {
        /// A typical class will have all the following regions in the same order.
        /// Exact names are flexible, but should include at least variants on the following
        /// four regions. Additional subregions within these may be added. 
        /// The overarching idea is to block off the public APIs and internals in separate
        /// sections, 
        #region Members
        /// <summary>
        /// All members should be private, and accessed through Properties. There are
        /// exceptions for trivial classes (really just reference structs).
        /// Member variables start with a lower case letter, then use Upper case initial
        /// for each following word.
        /// If a member variable is going to be accessed directly, rather than through
        /// accessors, then put it in the accessor section and name it like an accessor.
        /// It's more work for you, but less work for everyone around you.
        /// </summary>
        private int numTimes = 0;

        /// <summary>
        /// Might label this for clarity, but definitely label the property accessor below.
        /// </summary>
        private float lastTime = 0.0f;

        /// <summary>
        /// We (arbitrarily) use order [private,protected,public] [abstract,virtual,static,const]
        /// as shown here.
        /// </summary>
        private static float reallyLastTime = 0.0f;
        #endregion Members

        #region Accessors
        /// <summary>
        /// All public interfaces should be commented like this to pick up tooltips.
        /// Include the units of any returned value in this description.
        /// Trivial accessors may be collapsed to single line.
        /// Protected accessors should follow public, with private accessors last.
        /// Bracketing the protected and private accessors in an Internal subregion
        /// can be helpful to clients trying to get the public API at a glance.
        /// </summary>
        public int NumTimes
        {
            get { return numTimes; }
            private set { numTimes = value; }
        }

        /// <summary>
        /// Another property, this one is still trivial, but once it's past a single statement
        /// it should be expanded out like any other function. In seconds.
        /// </summary>
        public float LastTime
        {
            get { return lastTime; }
            set
            {
                lastTime = value;
                if (lastTime > reallyLastTime)
                    reallyLastTime = lastTime;
            }
        }

        /// <summary>
        /// Yet another. In seconds.
        /// </summary>
        public static float AnotherTime
        {
            get { return reallyLastTime; }
            set { reallyLastTime = value; }
        }

        /// <summary>
        /// Followed by less public accessors. Comments optional on internals, but appreciated.
        /// </summary>
        protected float ConstantTime
        {
            get { return 0.0f; }
        }
        #endregion Accessors

        #region Public Methods
        /// <summary>
        /// Constructor - Public members get a tooltip summary.
        /// </summary>
        public BestCoding(float lastTime)
        {
            /// Avoid using this.Member, just use Member instead. Exception is
            /// to avoid collision with input variables, especially in constructor.
            this.lastTime = lastTime;

            /// Constants are all prefixed with the letter k, as in:
            const int kNumTimes = 5;

            /// Short iterator names are fine, unless there's a special need to distinguish them.
            /// 
            /// On trivial single statement clauses, curly brace use should be
            /// based on whatever makes for most readable.
            for (int i = 0; i < kNumTimes; ++i)
            {
                KillTime(i);
            }
            /// Opening and closing curly braces are always appropriate. But where brevity will
            /// enhance clarity, they can be omitted for.
            if (NumTimes < kNumTimes)
                NumTimes = kNumTimes;
        }

        /// Any additional comments that wouldn't be appropriate for a tooltip (i.e. that
        /// the calling client doesn't care about, only the person trying to maintain/extend
        /// the code does) should go outside the summary like this.
        /// Make note of anything unusually tricky you are doing here. Don't repeat
        /// the code in English, specify intent, not implementation.
        /// Since it doesn't explain how the function blocks, someone could come in
        /// and replace the braindead code below without the comments getting out
        /// of sync.
        /// <summary>
        /// Block for howMuch seconds.
        /// Any input or output units should be included in the tooltip summary.
        /// </summary>
        /// <param name="howMuch"></param>
        public void KillTime(int howMuch)
        {
            for (int i = 0; i < howMuch; ++i)
            {
                Thread.Sleep(1000);
            }
        }

        /// Prefer List<> over ArrayList. If you use ArrayList, check for null on
        /// _every_element_ you pull out and cast to a type. If you already know what
        /// type it is, you should be using List<> instead.
        /// <summary>
        /// Do random things to lists of stuff.
        /// </summary>
        /// <param name="arrayList"></param>
        /// <param name="list"></param>
        public void SampleLists(ArrayList arrayList, List<BestCoding> list)
        {
            /// Include lots of asserts from System.Diagnostics.
            Debug.Assert(arrayList.Count > list.Count);

            /// This is bad. If you know every element in arrayList is a BestCoding,
            /// it should be a List<BestCoding>.
            foreach (object o in arrayList)
            {
                BestCoding b = o as BestCoding;
                Debug.Assert(b != null); // Assertion not good enough here.
                b.KillTime(1);
            }
            /// This is okay. 
            foreach (BestCoding b in arrayList)
            {
                if (b != null)
                {
                    b.KillTime(1);
                }
            }
            /// This is better
            foreach (BestCoding b in list)
            {
                /// Okay
                Debug.Assert(b != null); // Null element in list?
                /// Better, this way the user can report the comment.
                Debug.Assert(b != null, "Null element in list?"); 
                b.KillTime(1);
            }
        }
        #endregion Public Methods

        #region Internal Methods
        /// Prefer C# predefined types rather than their aliases in the System namepspace.
        /// 
        /// For very long function declarations or calls, break it into multiple lines as
        /// shown here. Very long is loosely defined as more than about 100 characters.
        /// 
        /// Similarly, break up very long functions into multiple subfunctions. Again, no
        /// hard limit, but when a function gets longer than 100 lines, start considering
        /// breaking it up.
        /// <summary>
        /// Not really sure what this function does, best not to call it.
        /// </summary>
        /// <param name="which"></param>
        /// <param name="howMuch"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        private string GetName(
            int which, // comments okay here, but keep in mind it won't show up in tooltip.
            double howMuch,
            object owner)
        {
            if (which > howMuch)
            {
                return GetName(
                    which,
                    which,
                    howMuch);
            }
            return owner.ToString();
        }
        #endregion Internal Methods

        #region Guidelines for GC-sensitive methods
        /// Methods called very frequently are considered GC-sensitive and therefore
        /// should not allocate any heap memory. Sometimes though it is unavoidable.
        /// In this case, the programmer should place these heap variables outside
        /// the function and reuse them each call. Although these variables become
        /// class members when promoted up, they should be considered local to the
        /// function. To help clarify this, their names should be prepended with the
        /// associated function name (or some such descriptive prefix), and the
        /// variables themselves should have intellisense comments calling out the
        /// association.

        /// <summary>
        /// Test for a hit at the specified location.
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public bool HitTest(Vector3 where)
        {
            // Clear the previous call's hit list.
            hitTestHitList.Clear();

            // Gather all the current hits.
            GatherHits(hitTestHitList);

            // Check whether we are on a hit.
            return CheckForHit(where, hitTestHitList);
        }

        /// <summary>
        /// A variable local to HitTest. Do not use.
        /// </summary>
        List<Hit> hitTestHitList = new List<Hit>();
        #endregion
    }
}
