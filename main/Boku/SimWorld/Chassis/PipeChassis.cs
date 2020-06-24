
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Chassis
{
    //struct used to return the preferred connection to make when snapping a pipe
    struct PipeConnection
    {
        //source pos used to check snapping (will be source object's center position + an offset based on connection position)
        public Vector3 SourcePos { get; set; } 
        //up vector of the source pipe - used for determining if the pipes need to be pushed together
        public Vector3 SourceUpDir { get; set; }
        //target pos of the connection (which connecting position on another pipe did we snap to)
        public Vector3 TargetPos { get; set; }
    };

    //struct used to contain potential candidate snapping target info
    struct CandidateConnection
    {
        //which index in the source connection array does this candidate come from?
        public int SourceConnectionIndex;
        //where would we snap to for this candidate?
        public Vector3 SnapToPosition;
        //what is this candidate's up vector?
        public Vector3 UpDir;
        //what is this candidate's center position (combined with snap to position to generate a push together vector)
        public Vector3 CenterPosition;
        //how much weight should this candidate be given (based on distance with some modifiers to eliminate flickering)
        public float Weight;
        //is this the candidate we chose last time?
        public bool RetainSnap;
    }

    /// <summary>
    /// A chassis that keeps the object properly rotated.  Used for the building pipes.
    /// </summary>
    public class PipeChassis : BaseChassis
    {
        public enum PipeTypeEnum
        {
            PipeStraight = 1,
            PipeCorner,
            PipeCross
        };

        private const float kGridSize = 2.0f; //should align with actual content size
        private const float kPushTogetherFactor = 2.5f; //how much to push pipes together when their up vectors are not aligned
        private const float kMaxSnapOffset = 2.0f; //within what radius will we snap to a point
        
        #region Members
        //current pipe's rotation based on terrain and yaw-snapped angle
        private float m_terrainPitch;
        private float m_terrainRoll;
        private float m_snappedYaw;

        //current pipe's pipe tyoe
        private PipeTypeEnum m_pipeType;


        //was this pipe snapped last frame?
        private bool m_bIsSnapped;
        //which source index was used to snap?
        private int m_lastSnapSourceIndex;
        //what was the position of that source index?
        private Vector3 m_lastSourcePos;
        //what cursor position was used as a starting reference (used to reduce jitter when the cursor isn't moving)
        private Vector3 m_lastSnapFromPos;
        //where did was last snap to?
        private Vector3 m_lastSnapTarget;
        //how much did we last push the pipes together?
        private Vector3 m_lastPushTogetherAmount;

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        /// <summary>
        /// type of the pipe (used to determine connection points for terrain rotations
        /// </summary>
        public PipeTypeEnum PipeType
        {
            get { return m_pipeType; }
            set { m_pipeType = value; }
        }

        #endregion

        public PipeChassis()
        {
            //pipes shouldn't bounce around
            fixedPosition = true;
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            m_terrainPitch = 0.0f;
            m_terrainRoll = 0.0f;
            m_snappedYaw = 0.0f;

            m_bIsSnapped = false;
            m_lastSourcePos = Vector3.Zero;
            m_lastPushTogetherAmount = Vector3.Zero;
            m_lastSnapSourceIndex = -1;
            m_lastSnapFromPos = Vector3.Zero;
            m_lastSnapTarget = Vector3.Zero;
        }

        
        public override void PreCollisionTestUpdate(GameThing thing)
        {
            bool isSelected = IsSelectedActor(thing);

            GameActor actor = thing as GameActor;

            // Snap the yaw to the nearest 45 degree angle.  Pi/4 equals 45 degrees.
            // Adding 0.5 allows answer to round to nearest 45 rather than truncate.
            // Then scale to get back into radians.
            int num45s = (int)(thing.Movement.RotationZ / MathHelper.PiOver4 + 0.5f);
            m_snappedYaw = num45s * MathHelper.PiOver4;

            //calculate facing/right vectors based on the snapped yaw
            Matrix rotationZ = Matrix.CreateRotationZ(m_snappedYaw);
            Vector3 facingDir = Vector3.TransformNormal(new Vector3(1.0f, 0.0f, 0.0f), rotationZ);
            facingDir.Normalize();

            Vector3 rightDir = -1.0f*Vector3.Cross(new Vector3(0.0f, 0.0f, 1.0f), facingDir);

            //calculate a snapped position based on nearby pipe pieces
            Vector3 snapToPosition = thing.Movement.Position;

            //note: position only updated if the object is selected
            if (isSelected)
            {
                //this will hold the connection to snap to, if we find one
                PipeConnection snapToConnection = new PipeConnection();

                //always use the cursor position as a starting point instead of the pipe position (otherwise we get jitter)
                Vector3 snapFromPosition = FindCursorPosition();

                //when selected, use the cursor as a starting point for all math to avoid twitching
                if (FindSnapToConnection(thing, snapFromPosition, out snapToConnection))
                {                    
                    //we found a connection, apply the 2D offset to the snap position (i.e. difference in movement needed to make source line up with target)
                    //ignore z movement for this
                    Vector3 snapOffset = snapToConnection.TargetPos - snapToConnection.SourcePos;
                    snapOffset.Z = 0.0f;

                    snapToPosition = snapFromPosition + snapOffset;
                }
            }
           

            //difference between the object at it's current height and the object when it's on the ground
            float editHeightDiff = Parent.EditHeight - Parent.MinHeight;

            //determine terrain height for each of the possible connection points
            float heightCenter = Terrain.GetTerrainHeight(snapToPosition);
            float heightForward = Terrain.GetTerrainHeight(snapToPosition + facingDir * kGridSize * Parent.ReScale);
            float heightBackward = Terrain.GetTerrainHeight(snapToPosition - facingDir * kGridSize * Parent.ReScale);
            float heightRight = Terrain.GetTerrainHeight(snapToPosition + rightDir * kGridSize * Parent.ReScale);
            float heightLeft = Terrain.GetTerrainHeight(snapToPosition - rightDir * kGridSize * Parent.ReScale);

            float waterHeightCenter = Terrain.GetWaterHeight(snapToPosition);
            float waterHeightForward = Terrain.GetWaterHeight(snapToPosition + facingDir * kGridSize * Parent.ReScale);
            float waterHeightBackward = Terrain.GetWaterHeight(snapToPosition - facingDir * kGridSize * Parent.ReScale);
            float waterHeightRight = Terrain.GetWaterHeight(snapToPosition + rightDir * kGridSize * Parent.ReScale);
            float waterHeightLeft = Terrain.GetWaterHeight(snapToPosition - rightDir * kGridSize * Parent.ReScale);

            if (thing.StayAboveWater)
            {
                heightCenter = Math.Max(heightCenter, waterHeightCenter);
                heightForward = Math.Max(heightForward, waterHeightForward);
                heightBackward = Math.Max(heightBackward, waterHeightBackward);
                heightRight = Math.Max(heightRight, waterHeightRight);
                heightLeft = Math.Max(heightLeft, waterHeightLeft);
            }

            //depending on the mode, we calculate each of these differently
            float minHeight = heightCenter;
            float maxHeight = heightCenter;

            switch (m_pipeType)
            {
                case PipeTypeEnum.PipeStraight:
                    //straight pipes only pitch
                    m_terrainPitch = (float)Math.Atan2(heightBackward - heightForward, kGridSize * Parent.ReScale * 2.0f);
                    m_terrainRoll = 0.0f;

                    minHeight = Math.Min(heightForward, heightBackward);
                    maxHeight = Math.Max(heightForward, heightBackward);

                    //update snap position on Z axis to be average of all connection points plus edit height
                    snapToPosition.Z = (heightForward + heightCenter + heightBackward) / 3.0f + EditHeight; 

                    break;

                //cross and corner are treated the same...corner looks better and is positioned better if it takes
                //all five positions into account
                case PipeTypeEnum.PipeCross:
                case PipeTypeEnum.PipeCorner:
                    //pitch using one axis, roll using the other
                    m_terrainPitch = (float)Math.Atan2(heightBackward - heightForward, kGridSize * Parent.ReScale * 2.0f);
                    m_terrainRoll = (float)Math.Atan2(heightRight - heightLeft, kGridSize * Parent.ReScale * 2.0f);

                    minHeight = Math.Min(heightForward, Math.Min(heightBackward, Math.Min(heightLeft, heightRight)));
                    maxHeight = Math.Max(heightForward, Math.Max(heightBackward, Math.Max(heightLeft, heightRight)));

                    //update snap position on Z axis to be average of connection points and center (plus edit height)
                    snapToPosition.Z = (heightForward + heightBackward + heightLeft + heightRight + heightCenter) / 5.0f + EditHeight;

                    break;
                default:
                    //nothing to do - unknown pipe types won't rotate at all
                    break;
            }

            //calculate rotation weight (how much to apply rotation based on terrain)
            //   the higher an edit height, the less we apply the rotation
            float terrainHeightDiff = maxHeight - minHeight;
            float rotationWeight = (terrainHeightDiff - editHeightDiff) / (terrainHeightDiff);
            rotationWeight = MyMath.Clamp<float>(rotationWeight, 0.0f, 1.0f);

            //clamp and apply rotation weight (pipes high enough off the ground won't need rotation
            m_terrainPitch = MyMath.Clamp<float>(m_terrainPitch, -MathHelper.PiOver2, MathHelper.PiOver2) * rotationWeight;
            m_terrainRoll = MyMath.Clamp<float>(m_terrainRoll, -MathHelper.PiOver2, MathHelper.PiOver2) * rotationWeight;


            //update the snap position, then factor it into the matrix with rotations
            //this is necessary as the kodu framework assumes a single rotation (about z), but we're actually rotating on all three axis
            // in order to align the pipes to the terrain
            thing.Movement.Position = snapToPosition;

            //generate the rotation matrix based on movement facing direction, terrain pitch and terrain roll
            Matrix local = Matrix.CreateRotationY(m_terrainPitch) * //rotation due to pitch from terrain in X (rotated around Y)
                                Matrix.CreateRotationX(-m_terrainRoll) *//rotation due to roll from terrain in Y (rotated around X)
                                Matrix.CreateRotationZ(m_snappedYaw); //normal rotation due to direction facing            

            //set translation to the snapped to position
            local.Translation = thing.Movement.Position;

            //and update our object
            thing.Movement.LocalMatrix = local;

        }

        //helper function that, given an actor and a starting position, will generate valid outgoing pipe connections
        private List<PipeConnection> GenerateConnections(GameActor actor, Vector3 fromPosition)
        {
            List<PipeConnection> connections = new List<PipeConnection>();
            
            PipeChassis chassis = actor.Chassis as PipeChassis;
            if (chassis == null)
            {
                return connections;
            }

            //determine modified direction vectors based on rotation
            Matrix rotationMat = Matrix.CreateRotationY(chassis.m_terrainPitch) * //rotation due to pitch from terrain in X (rotated around Y)
                                Matrix.CreateRotationX(-chassis.m_terrainRoll) *//rotation due to roll from terrain in Y (rotated around X)
                                Matrix.CreateRotationZ(chassis.m_snappedYaw); //normal rotation due to direction facing     

            Vector3 facingDir = Vector3.TransformNormal(new Vector3(1.0f, 0.0f, 0.0f), rotationMat);
            facingDir.Normalize();
            Vector3 rightDir = Vector3.TransformNormal(new Vector3(0.0f, -1.0f, 0.0f), rotationMat);
            rightDir.Normalize();
            Vector3 upDir = Vector3.TransformNormal(new Vector3(0.0f, 0.0f, 1.0f), rotationMat);
            upDir.Normalize();

            //from those modified direction vectors, determine potential snap points
            Vector3 forwardPos = fromPosition + facingDir * kGridSize * actor.ReScale;
            Vector3 backwardPos = fromPosition - facingDir * kGridSize * actor.ReScale;
            Vector3 rightPos = fromPosition + rightDir * kGridSize * actor.ReScale;
            Vector3 leftPos = fromPosition - rightDir * kGridSize * actor.ReScale;

            //add connections based on pipe type
            PipeConnection nextConnection;

            switch (chassis.PipeType)
            {
                case PipeTypeEnum.PipeStraight:

                    //forward
                    nextConnection = new PipeConnection { SourcePos = forwardPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);

                    //backward
                    nextConnection = new PipeConnection { SourcePos = backwardPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);
                    break;
                case PipeTypeEnum.PipeCross:

                    //forward
                    nextConnection = new PipeConnection { SourcePos = forwardPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);

                    //backward
                    nextConnection = new PipeConnection { SourcePos = backwardPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);

                    //left
                    nextConnection = new PipeConnection { SourcePos = leftPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);

                    //right
                    nextConnection = new PipeConnection { SourcePos = rightPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);
                    break;

                case PipeTypeEnum.PipeCorner:

                    //backward
                    nextConnection = new PipeConnection { SourcePos = backwardPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);

                    //right
                    nextConnection = new PipeConnection { SourcePos = rightPos, SourceUpDir = upDir };
                    connections.Add(nextConnection);
                    break;
            }

            return connections;
        }


        //helper function that, based on a given actor and reference position, will find the best connection to snap to
        //returns false if no connection found (pipe is not close to other pipes)
        private bool FindSnapToConnection(GameThing thing, Vector3 snapFromPosition, out PipeConnection snapToConnection)
        {
            snapToConnection = new PipeConnection { SourcePos = snapFromPosition, TargetPos = snapFromPosition };

            GameActor actor = thing as GameActor;

            if (actor == null || actor.Movement==null) return false;

            //build a list of connections based on our pipe type
            List<PipeConnection> connections = GenerateConnections(actor, snapFromPosition);

            //generate a list of all candidate snap to targets
            List<CandidateConnection> candidates = new List<CandidateConnection>();

            for (int i=0; i<connections.Count; ++i) 
            {
                PipeConnection connection = connections[i];

                AddToCandidates(thing, connection.SourcePos, i, ref candidates);
            }

            //no snap to targets?  early out...
            if (candidates.Count <= 0)
            {
                m_bIsSnapped = false;
                return false;
            }

            //find the candidate with the highest weight
            CandidateConnection bestCandidate = new CandidateConnection();
            bestCandidate.Weight = float.MinValue;
            
            for (int j = 0; j < candidates.Count; ++j)
            {
                if (candidates[j].Weight > bestCandidate.Weight)
                {
                    bestCandidate = candidates[j];
                }
            }

            //update the snap connection we're building based on the best candidate's values
            snapToConnection = connections[bestCandidate.SourceConnectionIndex];

            //if we were already snapped and our candidate is the same, then don't recalculate push-together amount

            //based on up dir difference, bump the snap position closer to the center of the target object
            float cosAngle = Vector3.Dot(bestCandidate.UpDir, connections[bestCandidate.SourceConnectionIndex].SourceUpDir);

            Vector3 pushTogetherDirection = bestCandidate.CenterPosition - bestCandidate.SnapToPosition;
            pushTogetherDirection.Normalize();

            ///////////////////////////////////////////
            //JITTER REDUCTION + PUSH TOGETHER LOGIC
            // The below code pushes together pipe pieces whose up vectors are not aligned, attempting to cover gaps
            // It also looks for cases where the source conditions were identical to last frame, and uses cached values 
            //  instead if so, preventing jitter
            ///////////////////////////////////////////

            //if snapping to same place, only allow push together amount if it is larger
            Vector3 pushTogetherAmount = pushTogetherDirection * kPushTogetherFactor * (1.0f - cosAngle);
            if (m_bIsSnapped && bestCandidate.RetainSnap)
            {
                //if the cursor hasn't moved and we're still snapped, don't ever update the source pos
                float deltaSnapFrom = (m_lastSnapFromPos - snapFromPosition).Length();
                if (deltaSnapFrom < 0.01f)
                {
                    //same connection and cursor hasn't moved, override the source pos to match what we had last time
                    snapToConnection.SourcePos = m_lastSourcePos;
                }
                else
                {
                    //cursor was moved, update source info
                    m_lastSnapFromPos = snapFromPosition;
                    m_lastSourcePos = snapToConnection.SourcePos;
                }

                //check if push together amount has a better value than last time
                if (pushTogetherAmount.LengthSquared() > m_lastPushTogetherAmount.LengthSquared())
                {
                    //this push amount is better, let's use it
                    snapToConnection.TargetPos = bestCandidate.SnapToPosition + pushTogetherAmount;
                    m_lastPushTogetherAmount = pushTogetherAmount;
                }
                else
                {
                    //our previously value still the best, keep it
                    snapToConnection.TargetPos = bestCandidate.SnapToPosition + m_lastPushTogetherAmount;
                }
            }
            else
            {
                //we weren't snapped here before, update various state
                snapToConnection.TargetPos = bestCandidate.SnapToPosition + pushTogetherAmount;
                m_lastPushTogetherAmount = pushTogetherAmount;
                m_lastSnapFromPos = snapFromPosition;
                m_lastSourcePos = snapToConnection.SourcePos;
            }

            //if we get this far, we found a snap taget, update some final values and return true
            m_bIsSnapped = true;
            m_lastSnapSourceIndex = bestCandidate.SourceConnectionIndex;
            m_lastSnapTarget = bestCandidate.SnapToPosition; //where were we trying to snap to?

            return true;
        }

        //helper function to determine all viable snap candidates given an actor and a starting position
        // will automatically reject candidates outside the configured min snap offset
        // will also automatically weight the previous frame's results higher to reduce jitter between two close snap points
        private void AddToCandidates(GameThing thing, Vector3 snapFromPosition, int sourceIndex, ref List<CandidateConnection> snapCandidates)
        {
            //loop over all game things looking for pipes
            for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
            {
                GameActor actor = InGame.inGame.gameThingList[i] as GameActor;

                if (actor != null && actor.Movement != null && actor != (thing as GameActor))
                {
                    if (actor.Chassis != null && actor.Chassis is PipeChassis)
                    {
                        //found a pipe that isn't ourself, look for candidates
                        PipeChassis targetPipe = actor.Chassis as PipeChassis;

                        List<PipeConnection> potentialSnapPoints = GenerateConnections(actor, actor.Movement.Position);

                        //weight each candidate based on how far away it is - if too far, reject it completely
                        foreach (PipeConnection nextSnapPoint in potentialSnapPoints)
                        {
                            Vector3 snapVector = nextSnapPoint.SourcePos - snapFromPosition;
                            snapVector.Z = 0.0f;

                            float distance = snapVector.Length();
                            //if distance is in max offset or, if it's exactly where we snapped to last time, allow for double the radius to consider it valid
                            if (distance <= kMaxSnapOffset * Parent.ReScale || 
                                (m_bIsSnapped && m_lastSnapSourceIndex == sourceIndex && distance < kMaxSnapOffset * Parent.ReScale * 2.0f))
                            {
                                float weight = 1.0f - MathHelper.Clamp(distance/kMaxSnapOffset, 0.0f, 1.0f);
                                bool retainSnap = false;

                                //special case: were we previously snapped to this exact target, from the same source?
                                // if so, add some weight - we'd prefer to stay in place
                                if (m_bIsSnapped && m_lastSnapSourceIndex == sourceIndex)
                                {
                                    float distanceToLastSnapped = (m_lastSnapTarget - nextSnapPoint.SourcePos).Length();
                                    if (distanceToLastSnapped < 0.001f)
                                    {
                                        weight += 1.0f;
                                        retainSnap = true;
                                    }
                                }

                                //found a candidate, add it to the lsit
                                CandidateConnection newCandidate = new CandidateConnection { SourceConnectionIndex = sourceIndex, 
                                                                                                SnapToPosition = nextSnapPoint.SourcePos, 
                                                                                                UpDir = nextSnapPoint.SourceUpDir, 
                                                                                                CenterPosition = actor.Movement.Position, 
                                                                                                Weight = weight,
                                                                                                RetainSnap = retainSnap };
                                snapCandidates.Add(newCandidate);
                            }
                        }
                    }
                }
            }
        }

        //helper function to determine if a given actor is selected (only selected actors snap)
        private bool IsSelectedActor(GameThing thing)
        {
            GameActor actor = thing as GameActor;

            //no selected actors in sim mode
            if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
            {
                return false;
            }

            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse && InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.SelectedActor == actor)
            {
                return true;
            }

            if (KoiLibrary.LastTouchedDeviceIsTouch && InGame.inGame.touchEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor == actor)
            {
                return true;
            }

            if (KoiLibrary.LastTouchedDeviceIsGamepad && InGame.inGame.editObjectUpdateObj.selectedObject == actor)
            {
                return true;
            }

            return false;
        }

        //helper function to find the cursor position across various input modes (used as the starting point when snapping)
        private Vector3 FindCursorPosition()
        {
            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                return MouseEdit.MouseTouchHitInfo.TerrainPosition;
            }
            else if (KoiLibrary.LastTouchedDeviceIsGamepad)
            {
                return InGame.inGame.Cursor3D.Position;
            }
            else if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                return TouchEdit.MouseTouchHitInfo.LastTouchEditPos;
            }

            return Vector3.Zero;
        }

        //helper for displaying debug axis on connection points
        public void DebugDisplay(GameActor actor, Camera camera)
        {
            List<PipeConnection> connections = GenerateConnections(actor, actor.Movement.Position);

            foreach (PipeConnection connection in connections)
            {
                Utils.DrawAxis(camera, connection.SourcePos);
            }
        }
    }   
}   // end of namespace Boku.SimWorld.Chassis
