// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using KoiX.Text;

using Boku.Common.Xml;

using BokuShared;
using BokuShared.Wire;

namespace Boku.Common
{
    /// <summary>
    /// A record containing the metadata for one level, as well as some status flags.
    /// </summary>
    public class LevelMetadata : IDisposable
    {
        private bool disposed;

        ~LevelMetadata()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Thumbnail.Dispose();
            }
        }

        public enum DownloadStates
        {
            None,
            Queued,
            InProgress,
            Complete,
            Failed
        }

        public DownloadStates DownloadState = DownloadStates.None;

        private ILevelBrowser browser;
        public ILevelBrowser Browser
        {
            get { return browser; }
            set { Debug.Assert(browser == null || value == null); browser = value; }
        }

        /// <summary>
        /// Level metadata fields
        /// </summary>
        public Guid WorldId;
        public DateTime LastWriteTime = DateTime.MinValue;
        public string Name;
        public string Description;
        public TextHelper.Justification DescJustification = TextHelper.Justification.Left;
        public string Creator;
        public Genres Genres;
        public int VotesUp;
        public int VotesDown;
        public int Downloads;

        public int NumLikes;
        public bool LikedByThisUser = false;    // Just here to let us fake preventing more than one like.
        public int NumComments;
        public string Permalink;

        public Guid? LinkedFromLevel;   // GUID of previous level in chain.  If null this this level is first.
        public Guid? LinkedToLevel;     // GUID of next level in chain.  If null then this level is last.

        public byte[] ThumbnailBytes;
        public AsyncThumbnail Thumbnail = new AsyncThumbnail();

        public SoclCommentPacket[] CommentDetails;
        public string RowKey;
        public string PartitionKey;

        public string Checksum;

        // Generated fields used within the UI
        public TextBlob UIDescBlob = null;
        public List<string> UIName = new List<string>();

        // ILevelBrowser's associated state
        public object BrowserState = null;

        /// <summary>
        /// The user's current vote on this level, if any.
        /// </summary>
        public Vote MyVote;

        /// <summary>
        /// True if user has flagged this level.
        /// </summary>
        public bool FlaggedByMe;

        /// <summary>
        /// Returns a computed value based on the number of up/down votes.
        /// </summary>
        public float Rating
        {
            get
            {
                float diff = VotesUp - VotesDown;
                float total = VotesUp + VotesDown;
                return diff * total;
            }
        }

        public DateTime LastSaveTime = DateTime.MinValue;//Used to determin if level is owned by user. 
                                                        //Note it should usually be the same as lastWriteTime but not the same as Modified.
        internal LevelMetadata Duplicate()
        {
            LevelMetadata level = new LevelMetadata();
            level.WorldId = WorldId;
            level.LastWriteTime = LastWriteTime;
            level.Name = Name;
            level.Description = Description;
            level.Creator = Creator;
            level.Genres = Genres;
            level.VotesUp = VotesUp;
            level.VotesDown = VotesDown;
            level.Downloads = Downloads;
            level.NumLikes = NumLikes;
            level.NumComments = NumComments;
            level.Permalink = Permalink;
            level.Thumbnail = Thumbnail;
            level.CommentDetails = CommentDetails;

            level.LinkedFromLevel = LinkedFromLevel;
            level.LinkedToLevel = LinkedToLevel;

            level.RowKey = RowKey;
            level.PartitionKey = PartitionKey;
            level.Checksum = Checksum;
            level.LastSaveTime = LastSaveTime;

            return level;
        }

        public void FromPacket(WorldInfoPacket packet)
        {
            WorldId = packet.WorldId;
            LastWriteTime = packet.Modified;
            Name = packet.Name;
            Description = packet.Description;
            Creator = packet.Creator;
            Genres = (Genres)packet.Genres;
            VotesUp = packet.VotesUp;
            VotesDown = packet.VotesDown;
            Downloads = packet.Downloads;
            NumLikes = packet.Likes;
            NumComments = packet.Comments;
            Permalink = packet.Permalink;
            MyVote = (Vote)packet.MyVote;
            FlaggedByMe = packet.FlaggedByMe;
            ThumbnailBytes = packet.ThumbnailBytes;
            CommentDetails = packet.CommentDetails;
            RowKey = packet.RowKey;
            PartitionKey = packet.PartitionKey;
            Checksum = packet.checksum;
            LastSaveTime = packet.LastSaveTime;
            
        }

        public void ToPacket(WorldInfoPacket packet)
        {
            packet.WorldId = WorldId;
            packet.Created = packet.Modified = LastWriteTime;
            packet.Name = Name;
            packet.Description = Description;
            packet.Creator = Creator;
            packet.Genres = (int)(Genres & ~Genres.Virtual);
            packet.VotesUp = VotesUp;
            packet.VotesDown = VotesDown;
            packet.Downloads = Downloads;
            packet.Likes = NumLikes;
            packet.Comments = NumComments;
            packet.Permalink = Permalink;
            packet.MyVote = (int)MyVote;
            packet.FlaggedByMe = FlaggedByMe;
            packet.ThumbnailBytes = ThumbnailBytes;
            packet.CommentDetails = CommentDetails;
            packet.RowKey = RowKey;
            packet.PartitionKey = PartitionKey;
            packet.LastSaveTime = LastSaveTime;
        }

        public void FromXml(XmlWorldData xml)
        {
            WorldId = xml.id;
            LastWriteTime = xml.lastWriteTime;
            Name = xml.name;
            Description = xml.description;
            DescJustification = xml.descJustification;
            Creator = xml.creator;
            Checksum = xml.checksum;

            Genres = (Genres)xml.genres;
            VotesUp = 0;
            VotesDown = 0;
            Downloads = 0;
            NumLikes = 0;
            NumComments = 0;
            Permalink = null;
            MyVote = Vote.None;
            FlaggedByMe = false;
            ThumbnailBytes = null;

            LinkedFromLevel = xml.LinkedFromLevel;
            LinkedToLevel = xml.LinkedToLevel;
            LastSaveTime = xml.lastWriteTime;

        }

        public void ToXml(XmlWorldData xml)
        {
            xml.id = WorldId;
            xml.lastWriteTime = LastWriteTime;
            xml.name = Name;
            xml.description = Description;
            xml.creator = Creator;
            xml.checksum = Checksum;
            xml.genres = (int)(Genres & ~(Genres.Virtual));
            xml.LinkedFromLevel = LinkedFromLevel;
            xml.LinkedToLevel = LinkedToLevel;
        }

        //will only follow consistent links
        public LevelMetadata NextLink()
        {
            if (LinkedToLevel != null && XmlDataHelper.CheckWorldExistsByGenre((Guid)LinkedToLevel, Genres))
            {
                LevelMetadata level = XmlDataHelper.LoadMetadataByGenre((Guid)LinkedToLevel, Genres);

                //make sure the link is consistent or don't return it
                if (level != null && level.LinkedFromLevel == this.WorldId)
                {
                    return level;
                }
            }

            return null;
        }

        //will only follow consistent links
        public LevelMetadata PreviousLink()
        {
            if (LinkedFromLevel != null && XmlDataHelper.CheckWorldExistsByGenre((Guid)LinkedFromLevel, Genres))
            {
                LevelMetadata level = XmlDataHelper.LoadMetadataByGenre((Guid)LinkedFromLevel, Genres);

                //make sure the link is consistent or don't return it
                if (level != null && level.LinkedToLevel == this.WorldId)
                {
                    return level;
                }
            }

            return null;
        }

        public LevelMetadata FindFirstLink()
        {
            LevelMetadata firstLink = this;
            LevelMetadata previousLink = this;

            //loop until the previous link is null
            while (previousLink != null)
            {
                firstLink = previousLink;
                previousLink = previousLink.PreviousLink();
            }

            return firstLink;
        }

        public LevelMetadata FindLastLink()
        {
            LevelMetadata lastLink = this;
            LevelMetadata nextLink = this;

            while (nextLink != null)
            {
                lastLink = nextLink;
                nextLink = nextLink.NextLink();
            }

            return lastLink;
        }

        //works on any link in the chain
        public int CalculateTotalLinkLength()
        {
            LevelMetadata firstLink = FindFirstLink();

            int linkLength = 1;
            while (null != firstLink && firstLink.LinkedToLevel != null)
            {
                firstLink = firstLink.NextLink();
                linkLength++;
            }

            return linkLength;
        }

        public bool FindBrokenLink(ref LevelMetadata brokenLevel, ref bool forwardLink)
        {
            //initialize the out params - assume no broken link
            brokenLevel = null;
            forwardLink = false;

            //First, walk backwards to the first link
            LevelMetadata currentLink = this;
            LevelMetadata previousLink = null;
            LevelMetadata nextLink = null;

            while (currentLink.LinkedFromLevel != null)
            {
                //check to make sure the xml exists
                previousLink = currentLink.PreviousLink();

                if (null == previousLink)
                {
                    brokenLevel = currentLink;
                    forwardLink = false; //broke walking backwards

                    return true;
                }
                currentLink = previousLink;
            }

            //first link now points to the beginning, walk forward to the end, ensuring we 
            while (currentLink.LinkedToLevel != null)
            {
                //check to make sure the xml exists
                nextLink = currentLink.NextLink();

                if (null == nextLink)
                {
                    brokenLevel = currentLink;
                    forwardLink = false; //broke walking backwards

                    return true;
                }
                currentLink = nextLink;

            }

            //if we made it this far, all of the links worked out
            return false;
        }

        public static LevelMetadata CreateFromXml(XmlWorldData xml)
        {
            LevelMetadata metadata = new LevelMetadata();
            metadata.FromXml(xml);
            return metadata;
        }
    }

    /// <summary>
    /// Contains one page of level metadata.
    /// </summary>
    public class LevelMetadataPage
    {
        public int First;
        public int Total;
        public List<LevelMetadata> Listing = new List<LevelMetadata>();
    }
}
