using System;
using System.Collections.Generic;

using KoiX;

using Boku.Base;
using Boku.Common;

using BokuShared;
using BokuShared.Wire;


namespace Boku.Web
{
    /// <summary>
    /// The result class returned to the UserLogin caller.
    /// </summary>
    public class AsyncResult_UserLogin : AsyncResult
    {
        public UserLevel UserLevel;
        public Version LatestVersion;
    }

    public class AsyncResult_GetWorldData : AsyncResult
    {
        public WorldPacket World = new WorldPacket();
    }

    /// <summary>
    /// Contains the result of a GetPageOfLevels query. Passed to the
    /// callback supplied to the function call that began the query.
    /// </summary>
    public class AsyncResult_GetPageOfLevels : AsyncResult
    {
        public LevelMetadataPage Page = new LevelMetadataPage();
    }

    public class AsyncResult_GetSearchPageOfLevels : AsyncResult
    {
        public LevelMetadataPage Page = new LevelMetadataPage();
    }

    public class AsyncResult_Thumbnail : AsyncResult
    {
        public byte[] ThumbnailBytes;
    }

    public class AsyncResult_GetKoduWebUser : AsyncResult
    {
        public KoduWebUser KoduWebUser;
    }

    /// <summary>
    /// Community server API.
    /// </summary>
    public static partial class Community
    {
        #region Public
        /// <summary>
        /// Caches the user priveledge level returned from the most recent UserLogin transaction.
        /// </summary>
        public static UserLevel UserLevel = UserLevel.User;

        public static void Async_AbortAll()
        {
            lock (active)
            {
                active.Clear();
            }
        }

        public static void Async_Cancel(int transId)
        {
            IUnregister(transId);
        }

        public static bool Async_Ping(
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.Ping trans = new Trans.Ping(Callback_Ping, state);
                IRegister(state.transId);
                return trans.Send();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Queries for the user's admin level.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_UserLogin(
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.UserLogin trans = new Trans.UserLogin(Callback_UserLogin, state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Query for a page of level metadata. This function is used by
        /// CommunityLevelBrowser class, but you may call it directly if
        /// you prefer.
        /// </summary>
        /// <param name="genreFilter"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortDir"></param>
        /// <param name="first"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_GetPageOfLevels(
            Genres genreFilter,
            SortBy sortBy,
            SortDirection sortDir,
            int first, 
            int count,
            BokuAsyncCallback callback, 
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.GetWorldPage trans = new Trans.GetWorldPage(
                    genreFilter,
                    sortBy,
                    sortDir,
                    first,
                    count,
                    Callback_GetPageOfLevels,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// Query for a page of level metadata. This function is used by
        /// CommunityLevelBrowser class, but you may call it directly if
        /// you prefer.
        /// </summary>
        /// <param name="genreFilter"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortDir"></param>
        /// <param name="first"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_GetSearchPageOfLevels(
            Genres genreFilter,
            string searchString,
            SortBy sortBy,
            SortDirection sortDir,
            int first,
            int count,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.GetSearchWorldPage trans = new Trans.GetSearchWorldPage(
                    genreFilter,
                    searchString,
                    sortBy,
                    sortDir,
                    first,
                    count,
                    Callback_GetSearchPageOfLevels,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Register the user's vote for a level. Multiple votes from the
        /// same user will not accumulate on a level. A user may change
        /// their vote at any time and it will be reflected.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="vote"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_VoteOnLevel(
            Guid id, 
            Vote vote,
            BokuAsyncCallback callback, 
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.VoteOnLevel trans = new Trans.VoteOnLevel(
                    id,
                    vote,
                    Callback_VoteOnLevel,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Flag a level as offensive. Behavior differs based on the flagger's admin level:
        ///   User  : A notification is sent to the domain admin DL about the flag.
        ///   Admin : Level is banned, its creator gets a mark, and a notification sent to domain admin DL.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_FlagLevel(
            Guid worldId,
            bool flag,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.FlagLevel trans = new Trans.FlagLevel(
                    worldId,
                    flag,
                    Callback_FlagLevel,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Download a level from the community server.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_GetWorldData(
            Guid worldId,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.GetWorldData trans = new Trans.GetWorldData(
                    worldId,
                    Callback_GetWorldData,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Delete a world from the server.  Server checks permissions.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_DelWorldData(
            Guid worldId,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.DelWorldData trans = new Trans.DelWorldData(
                    worldId,
                    Callback_DelWorldData,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }
        public static int Async_DelWorldData2(
            Guid worldId,
            string pin,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.DelWorldData2 trans = new Trans.DelWorldData2(
                    worldId,
                    pin,
                    Callback_DelWorldData2,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        public static int Async_PostComment(
            PostCommentPacket packet,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.PostComment trans = new Trans.PostComment(
                    Callback_PostComment,
                    state,
                    packet);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        public static int Async_PostLike(
        PostLikePacket packet,
        BokuAsyncCallback callback,
        object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.PostLike trans = new Trans.PostLike(
                    Callback_PostLike,
                    state,
                    packet);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }
        public static int Async_PostLikeByWorldId(
        PostLikeByWorldIdPacket packet,
        BokuAsyncCallback callback,
        object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.PostLikeByWorldId trans = new Trans.PostLikeByWorldId(
                    Callback_PostLikeByWorldId,
                    state,
                    packet);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Upload a level to the community server.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int Async_PutWorldData(
            WorldPacket packet,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;
                Trans.PutWorldData trans = new Trans.PutWorldData(
                    packet,
                    Callback_PutWorldData,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }

        public static int Async_GetThumbnail(
            Guid worldId,
            AsyncThumbnail asyncTexture,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;

                Trans.GetThumbnail trans = new Boku.Web.Trans.GetThumbnail(
                    worldId,
                    Callback_GetThumbnail,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }


        public static int Async_GetKoduWebUser(
            string userSecret,
            BokuAsyncCallback callback,
            object param)
        {
            try
            {
                AsyncState state = new AsyncState();
                state.callback = callback;
                state.param = param;

                Trans.GetKoduWebUser trans = new Boku.Web.Trans.GetKoduWebUser(
                    userSecret,
                    Callback_GetKoduWebUser,
                    state);
                IRegister(state.transId);
                if (trans.Send())
                    return state.transId;
                else
                    return 0;

            }
            catch
            {
                return 0;
            }
        }
        #endregion
    }





    /// <summary>
    /// The internal machinations of the Community class.
    /// </summary>
    public static partial class Community
    {
        #region Internal

        public static int? SymmetricIndex = null;

        private static int transIdSeq;
        private static Dictionary<int, int> active = new Dictionary<int, int>();

        private static int INextTransId()
        {
            ++transIdSeq;
            if (transIdSeq == 0)
                ++transIdSeq;
            return transIdSeq;
        }

        private static void IRegister(int transId)
        {
            lock (active)
            {
                active.Add(transId, transId);
            }
        }

        private static bool IUnregister(int transId)
        {
            lock (active)
            {
                bool registered = active.ContainsKey(transId);
                if (registered)
                    active.Remove(transId);
                return registered;
            }
        }


        private class AsyncState
        {
            public BokuAsyncCallback callback;
            public object param;
            public double sendTime = Time.WallClockTotalSeconds;
            public int transId = INextTransId();
        };

        private static void Callback_Ping(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.Ping.Result reply = (Trans.Ping.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_UserLogin(object replyObj)
        {
            AsyncResult_UserLogin result = new AsyncResult_UserLogin();
            Trans.UserLogin.Result reply = (Trans.UserLogin.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.UserLevel = reply.userLevel;
                result.LatestVersion = reply.latestVersion;

                // Remember the user's permission level.
                UserLevel = reply.userLevel;
            }

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }
        private static void Callback_GetPageOfLevels(object replyObj)
        {
            AsyncResult_GetPageOfLevels result = new AsyncResult_GetPageOfLevels();
            Trans.GetWorldPage.Result reply = (Trans.GetWorldPage.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.Page.First = reply.page.First;
                result.Page.Total = reply.page.Total;

                for (int i = 0; i < reply.page.Descriptors.Length; ++i)
                {
                    LevelMetadata datum = new LevelMetadata();
                    datum.FromPacket(reply.page.Descriptors[i]);
                    result.Page.Listing.Add(datum);
                }
            }

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }
        private static void Callback_GetSearchPageOfLevels(object replyObj)
        {
            //Note: Result is the same as GetPageOfLevels
            AsyncResult_GetPageOfLevels result = new AsyncResult_GetPageOfLevels();
            Trans.GetSearchWorldPage.Result reply = (Trans.GetSearchWorldPage.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.Page.First = reply.page.First;
                result.Page.Total = reply.page.Total;

                for (int i = 0; i < reply.page.Descriptors.Length; ++i)
                {
                    LevelMetadata datum = new LevelMetadata();
                    datum.FromPacket(reply.page.Descriptors[i]);
                    result.Page.Listing.Add(datum);
                }
            }

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_VoteOnLevel(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.VoteOnLevel.Result reply = (Trans.VoteOnLevel.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_FlagLevel(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.FlagLevel.Result reply = (Trans.FlagLevel.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_GetWorldData(object replyObj)
        {
            AsyncResult_GetWorldData result = new AsyncResult_GetWorldData();
            Trans.GetWorldData.Result reply = (Trans.GetWorldData.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.World = reply.world;
            }

            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_DelWorldData(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.DelWorldData.Result reply = (Trans.DelWorldData.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }
        private static void Callback_DelWorldData2(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.DelWorldData2.Result reply = (Trans.DelWorldData2.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_PostLike(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.PostLike.Result reply = (Trans.PostLike.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_PostLikeByWorldId(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.PostLikeByWorldId.Result reply = (Trans.PostLikeByWorldId.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }
        private static void Callback_PostComment(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.PostComment.Result reply = (Trans.PostComment.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_PutWorldData(object replyObj)
        {
            AsyncResult result = new AsyncResult();
            Trans.PutWorldData.Result reply = (Trans.PutWorldData.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_GetThumbnail(object replyObj)
        {
            AsyncResult_Thumbnail result = new AsyncResult_Thumbnail();
            Trans.GetThumbnail.Result reply = (Trans.GetThumbnail.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.ThumbnailBytes = reply.thumbnailBytes;
            }
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }

        private static void Callback_GetKoduWebUser(object replyObj)
        {
            AsyncResult_GetKoduWebUser result = new AsyncResult_GetKoduWebUser();
            Trans.GetKoduWebUser.Result reply = (Trans.GetKoduWebUser.Result)replyObj;
            AsyncState state = (AsyncState)reply.userState;
            result.Success = reply.success;

            if (result.Success)
            {
                result.KoduWebUser = reply.koduWebUser;
            }
            result.Param = state.param;
            result.Seconds = Time.WallClockTotalSeconds - state.sendTime;

            if (IUnregister(state.transId) && state.callback != null)
                state.callback(result);
        }
        #endregion
    }
}
