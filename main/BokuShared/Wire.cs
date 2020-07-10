// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define LOG
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Diagnostics;

// NOTE:
// This library may not reference any XNA assemblies because it is
// shared by server-side modules (which cannot load XNA).

namespace BokuShared.Wire
{
    public enum ResultCode
    {
        Success,
        Fail
    }

    #region Messages

    // Hello
    public class Message_HelloRequest : XmlData<Message_HelloRequest>
    {
        public byte[] Modulus;
        public byte[] Exponent;
    }
    public class Message_HelloReply : XmlData<Message_HelloReply>
    {
        public int Index;
        public byte[] Key;
        public byte[] IV;
    }

    public class Message_Hello2Reply : XmlData<Message_Hello2Reply>
    {
        public int Index;
    }

    // UserLogin
    public class Message_UserLoginRequest : XmlData<Message_UserLoginRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Version Version;
    }
    public class Message_UserLoginReply : XmlData<Message_UserLoginReply>
    {
        public ResultCode ResultCode;
        public UserLevel UserLevel;
        public Version Version;
    }

    // UserLogin
    public class Message_VerifyUserRequest : XmlData<Message_VerifyUserRequest>
    {
        public string UserName;
        public string Identity;
    }
    public class Message_VerifyUserReply : XmlData<Message_VerifyUserReply>
    {
        public ResultCode ResultCode;
    }

    // GetWorldPage
    public class Message_GetWorldPageRequest : XmlData<Message_GetWorldPageRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public int First;
        public int Count;
        public string SortBy;
        public string SortDir;
        public int GenreFilter;
    }
    // GetSearchWorldPage
    public class Message_GetSearchWorldPageRequest : XmlData<Message_GetSearchWorldPageRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public int First;
        public int Count;
        public string SortBy;
        public string SortDir;
        public int GenreFilter;
        public string SearchString;
    }
    public class Message_GetWorldPageReply : XmlData<Message_GetWorldPageReply>
    {
        public ResultCode ResultCode;
        public WorldPagePacket Page;
    }
    public class Message_GetSearchWorldPageReply : XmlData<Message_GetSearchWorldPageReply>
    {
        public ResultCode ResultCode;
        public WorldPagePacket Page;
    }
    public class Message_GetThumbnailReply : XmlData<Message_GetThumbnailReply>
    {
        public ResultCode ResultCode;
        public byte[] thumbnailBytes;
    }
    // GetWorldData
    public class Message_GetWorldDataRequest : XmlData<Message_GetWorldDataRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Guid WorldId;
    }
    public class Message_GetWorldDataReply : XmlData<Message_GetWorldDataReply>
    {
        public ResultCode ResultCode;
        public WorldPacket World;
    }

    // PutWorldData
    public class Message_PutWorldDataRequest : XmlData<Message_PutWorldDataRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public WorldPacket World;
        public long UserId = 0;
    }
    // PutWorldData
    public class Message_PutWorldDataRequest2: XmlData<Message_PutWorldDataRequest2>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public WorldPacket World;
        public long UserId = 0;
        public string idHash;
    }
    public class Message_PutWorldDataReply : XmlData<Message_PutWorldDataReply>
    {
        public ResultCode ResultCode;
        public Guid WorldId;
    }

    // DelWorldData
    public class Message_DelWorldDataRequest : XmlData<Message_DelWorldDataRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Guid WorldId;
    }
    // DelWorldData
    public class Message_DelWorldData2Request : XmlData<Message_DelWorldData2Request>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Guid WorldId;
        public string pin;
    }
    public class Message_DelWorldDataReply : XmlData<Message_DelWorldDataReply>
    {
        public ResultCode ResultCode;
    }
    public class Message_DelWorldData2Reply : XmlData<Message_DelWorldData2Reply>
    {
        public ResultCode ResultCode;
    }
    // FlagLevel
    public class Message_FlagLevelRequest : XmlData<Message_FlagLevelRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Guid WorldId;
        public bool Flag;
    }
    public class Message_FlagLevelReply : XmlData<Message_FlagLevelReply>
    {
        public ResultCode ResultCode;
    }

    // VoteOnLevel
    public class Message_VoteOnLevelRequest : XmlData<Message_VoteOnLevelRequest>
    {
        public string SiteId;
        public string UserName;
        public string Community;
        public Guid WorldId;
        public int Vote;
    }
    public class Message_VoteOnLevelReply : XmlData<Message_VoteOnLevelReply>
    {
        public ResultCode ResultCode;
    }

    // Instrumentation
    public class Message_Instrumentation : XmlData<Message_Instrumentation>
    {
        public string SiteId;
        public string UserName;
        public string Community;

        public InstrumentationPacket Instruments = new InstrumentationPacket();
    }

    // ReportError
    public class Message_ReportError : XmlData<Message_ReportError>
    {
        public string ErrorMessage;
        public string StackTrace;
        public string AddInfo;
    }

    // GetCurrentVersion
    public class Message_GetCurrentVersion : XmlData<Message_GetCurrentVersion>
    {
        public string Token;
    }
    public class Message_Version : XmlData<Message_Version>
    {
        public string Token;
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;
        public string ReleaseNotesUrl;
        public string UpdateUrl;

        public Version Version
        {
            get { return new Version(Major, Minor, Build, Revision); }
        }

        public string ToJSON()
        {
            return String.Format(@"{6} {0}Token{0}:{0}{1}{0}, {0}Major{0}:{2}, {0}Minor{0}:{3}, {0}Build{0}:{4}, {0}Revision{0}:{5} {7}",
                "\"", Token, Major, Minor, Build, Revision, "{", "}");
        }
    }

    public class KoduWebUser
    {
        public long UserId;
        public string UserName;
        public string UserSecret;
        public long FbUserId;
        public string FbAuthToken;
    }

    public class Message_GetWebUserRequest : XmlData<Message_GetWebUserRequest>
    {
        public string UserSecret;
    }

    public class Message_GetWebUserReply : XmlData<Message_GetWebUserReply>
    {
        public ResultCode ResultCode;
        public KoduWebUser KoduUser;
    }

    #endregion

    #region Packets

    public class WorldInfoPacket : XmlData<WorldInfoPacket>
    {
        public Guid WorldId;
        public DateTime Created;
        public DateTime Modified;
        public string Name;
        public string Description;
        public string Creator;
        public string IdHash;   // Deprecated, but left in the class for server side compat.
        public int Genres;
        public byte[] ThumbnailBytes;
        public byte[] ScreenshotBytes;

        public int VotesUp;
        public int VotesDown;
        public int Downloads;
        public int MyVote;
        public bool FlaggedByMe;

        // So.cl data
        public int Likes;
        public int Comments;
        public string Permalink;
        public SoclCommentPacket[] CommentDetails;
        public string RowKey;
        public string PartitionKey;
        public string checksum;
        public DateTime LastSaveTime;//this value comes from the XML data.
    }

    public class WorldCommentPacket : XmlData<WorldCommentPacket>
    {
        public long CommentId;
        public long UserId;
        public string UserName;
        public DateTime PostDateTime;
        public string CommentBody;
    }

    public class SoclCommentPacket : XmlData<SoclCommentPacket>
    {
        public string CommentText;
        public string UserScreenName;
        public string DisplayName;
        public int UserId;
        public int Likes;
    }

    public class PostCommentPacket
    {
        public long UserId;
        public string CommentText;
        public string PartitionKey;
        public string RowKey;
    }

    public class Message_PostCommentRequest : XmlData<Message_PostCommentRequest>
    {
        public PostCommentPacket packet;
    }

    public class PostLikePacket
    {
        public long UserId;
        public bool Liked;
        public string PartitionKey;
        public string RowKey;
    }
    public class PostLikeByWorldIdPacket : PostLikePacket
    {
        public long UserId;
        public bool Liked;
        public Guid WorldId;
    }

    public class Message_PostLikeRequest : XmlData<Message_PostLikeRequest>
    {
        public PostLikePacket packet;
    }
    public class Message_PostLikeByWorldIdRequest : XmlData<Message_PostLikeByWorldIdRequest>
    {
        public PostLikeByWorldIdPacket packet;
    }
    public class Message_PostCommentReply : XmlData<Message_PostCommentReply>
    {
        public ResultCode ResultCode;
    }

    public class Message_PostLikeReply : XmlData<Message_PostLikeReply>
    {
        public ResultCode ResultCode;
    }

    public class WorldDataPacket : XmlData<WorldDataPacket>
    {
        public Guid WorldId;
        public byte[] WorldXmlBytes;
        public byte[] StuffXmlBytes;
        public byte[] VirtualMapBytes;
    }

    public class WorldPacket : XmlData<WorldPacket>
    {
        public WorldInfoPacket Info = new WorldInfoPacket();
        public WorldDataPacket Data = new WorldDataPacket();
    }

    public class WorldPagePacket : XmlData<WorldPagePacket>
    {
        public int First;
        public int Total;
        public WorldInfoPacket[] Descriptors;
    }

    public class UnreviewedWorldsPacket : XmlData<UnreviewedWorldsPacket>
    {
        public WorldInfoPacket[] Descriptors;
        public WorldFlagsPacket[] Flags;
    }

    public class WorldFlagsPacket : XmlData<WorldFlagsPacket>
    {
        public string[] Flaggers;
        public DateTime[] FlagDates;
    }

    public class WorldIdList : XmlData<WorldIdList>
    {
        public Guid[] WorldIds;
    }

    public class InstrumentationPacket : XmlData<InstrumentationPacket>
    {
        public class Event
        {
            public string Name;
            public string Comment;
        }

        public class Timer
        {
            public string Name;
            public double TotalTime;
            public int Count;
        }

        public class Counter
        {
            public string Name;
            public int Count;
        }

        public class DataItem
        {
            public string Name;
            public string Value;
        }

        public List<Event> Events = new List<Event>();
        public List<Timer> Timers = new List<Timer>();
        public List<Counter> Counters = new List<Counter>();
        public List<DataItem> DataItems = new List<DataItem>();
    }

    #endregion
    #region WireHelper

    public static class WireHelper
    {
        private static void Log(string msg)
        {
#if LOG
            Debug.WriteLine(msg);
#endif
        }

        public static byte[] ReadBuffer(BinaryReader reader)
        {
            byte[] buffer = null;
            int count = reader.ReadInt32();

            Log(String.Format("WireHelper.ReadBuffer: reading {0} bytes", count));

            if (count != -1)
            {
                // count of -1 means buffer written was a null reference.
                buffer = reader.ReadBytes(count);
            }
            return buffer;
        }

        public static void WriteBuffer(BinaryWriter writer, byte[] buffer)
        {
            if (buffer != null)
            {
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
            else
            {
                // count of -1 means buffer was a null reference.
                writer.Write(-1);
            }
        }

        public static Guid ReadGuid(BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(16);
            Guid guid = new Guid(buffer);
            return guid;
        }

        public static void WriteGuid(BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        public static DateTime ReadDateTime(BinaryReader reader)
        {
            long val = reader.ReadInt64();
            // Serialized time is UTC. Convert to local timezone.
            DateTime dt = new DateTime(val).ToLocalTime();
            return dt;
        }

        public static void WriteDateTime(BinaryWriter writer, DateTime dt)
        {
            // Convert to UTC before serialization so that we're correct across timezones.
            DateTime utc = dt.ToUniversalTime();
            long val = utc.Ticks;
            writer.Write(val);
        }
    }

    #endregion
}

