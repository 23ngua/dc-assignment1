using ChatResult;
using Database;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ChatServerTier
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]

    internal class ChatServerImplementation : ChatServerInterface
    {
        private static Channels channels = Channels.Instance;
        private static HashSet<string> signedInUsers = new HashSet<string>();
        private static Dictionary<string, string> userChannels = new Dictionary<string, string>();
        private static SharedFiles sharedFiles = SharedFiles.Instance;
        private static PrivateConversations privateConversations = PrivateConversations.Instance;


        private static readonly object usersLock = new object();    // Protects signedInUsers
        private static readonly object membershipLock = new object(); // Protects user-to-channel membership state
        private static readonly object channelsLock = new object(); // Protect the shared channel list

        public SignInResult SignIn(string userID)
        {
            if(!string.IsNullOrWhiteSpace(userID))
            {
                string cleanUserID = userID.Trim();
                lock (usersLock)
                {
                    // Reject request if this ID is already signed in
                    if (signedInUsers.Contains(cleanUserID))
                    {
                        return new SignInResult { Success = false, 
                            Message = "That user ID is already signed in." };
                    }
                    // The ID is available, so reserve it for this user
                    signedInUsers.Add(cleanUserID);
                }
                return new SignInResult { Success = true, 
                    Message = "Sign-in successful." };
            } else {
                return new SignInResult { Success = false, 
                    Message = "Please enter a valid username" };
            }
        }

        public ChannelActionResult SignOut(string userID)
        {
            if (!string.IsNullOrWhiteSpace(userID))
            {
                string cleanUserID = userID.Trim();

                lock (usersLock)
                {
                    if (signedInUsers.Contains(cleanUserID))    // Checks if user is currently signed in
                    {
                        lock (membershipLock)   // Maybe move outside lock, but stops case where new user takes id before 
                        {
                            if (userChannels.ContainsKey(cleanUserID))
                            {
                                userChannels.Remove(cleanUserID);
                            }
                        }
                        signedInUsers.Remove(cleanUserID);
                        return new ChannelActionResult { Success = true, 
                            Message = "Sucsessfully signed Out User" };
                    } else {
                        return new ChannelActionResult { Success = false,
                            Message = "The user is not currently signed in." };
                    }
                }
            } else {
                return new ChannelActionResult { Success = false, 
                    Message = "Please enter a valid username" };
            }
        }

        /* -- Channel and Message Methods --- */
        public ChannelActionResult JoinChannel(string userID, string channelName)
        {
            // Reject invalid input
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName))
            {
                return new ChannelActionResult { Success = false, 
                    Message = "A valid user ID and channel must be provided." };
            }

            // Remove accidental spaces
            string cleanUserID = userID.Trim();
            string cleanChannelName = channelName.Trim();

            // Ensure user is actually signed in
            lock (usersLock)
            {
                if (!signedInUsers.Contains(cleanUserID))
                {
                    return new ChannelActionResult { Success = false, 
                        Message = "The user is not currently signed in." };
                }
            }

            // Ensure requested channel still exists
            lock (channelsLock)
            {
                if (!channels.ContainsChannel(cleanChannelName))
                {
                    return new ChannelActionResult { Success = false, 
                        Message = "That channel no longer exists." };
                }
            }

            // Protect membership state while checking and updating it
            lock (membershipLock)
            {
                // A user may belong to at most one channel
                if (userChannels.ContainsKey(cleanUserID))
                {
                    return new ChannelActionResult { Success = false,
                        Message = "You are already in a channel." };
                }
                // Record user's current channel
                userChannels.Add(cleanUserID, cleanChannelName);
            }

            return new ChannelActionResult { Success = true, 
                Message = "Joined channel successfully." };
        }


        public ChannelActionResult LeaveChannel(string userID)
        {
            if (!string.IsNullOrWhiteSpace(userID))
            {
                // Remove accidental spaces around the user ID
                string cleanUserID = userID.Trim();

                // Make sure user is currently signed in
                lock (usersLock)
                {
                    if (!signedInUsers.Contains(cleanUserID))
                    {
                        return new ChannelActionResult { Success = false,
                            Message = "The user is not currently signed in." };
                    }
                }

                // Protect shared membership state while changing it
                lock (membershipLock)
                {
                    // The user must currently belong to a channel
                    if (!userChannels.ContainsKey(cleanUserID))
                    {
                        return new ChannelActionResult { Success = false,
                            Message = "You are not currently in a channel." };
                    }

                    // Remove the user's channel membership
                    userChannels.Remove(cleanUserID);
                }

                // Tell the client that leaving succeeded
                return new ChannelActionResult { Success = true,
                    Message = "Left channel successfully." };
            }
            else
            {
                return new ChannelActionResult { Success = false,
                    Message = "Please enter a valid username" };
            }
        }

        public ChannelActionResult CreateChannel(string userID, string channelName)
        {
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName))
            {
                return new ChannelActionResult { Success = false,
                    Message = "A valid user ID and channel must be provided." };
            }
            // Clean up inputs
            string cleanUserID = userID.Trim();
            string cleanChannelName = channelName.Trim();

            lock (channelsLock)
            {
                if (channels.ContainsChannel(cleanChannelName))
                {
                    return new ChannelActionResult { Success = false,
                        Message = "A channel with that name already exists." };
                } else {
                    channels.SetNewChannel(cleanChannelName);
                }
            }
            return new ChannelActionResult { Success = true,
                Message = "Channel created successfully" };
        }

        public List<string> GetChannelList()
        {
            return channels.GetChannelList();
        }

        public ChannelActionResult SendMessage(string userID, string channelName, string message)
        {
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName) || string.IsNullOrWhiteSpace(message))
            {
                return new ChannelActionResult { Success = false, Message = "Invalid message" };
            }

            if (!IsMemberOfChannel(userID, channelName))
            {
                return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
            }

            channels.AddMessage(channelName, userID.Trim(), message); // ADD MESSAGE YIPPEE

            return new ChannelActionResult { Success = true, Message = "Message Sent Successfully" };
        }

        public int GetMessageCount(string channelName)
        {
            return channels.GetMessageCount(channelName);
        }

        public List<ChatMessage> GetMessagesSince(string channelName, int sinceIndex)
        {
            List<MessageStruct> sourceMessages = channels.GetMessagesSince(channelName, sinceIndex);
            return ConvertMessages(sourceMessages);
        }

        /* -- File Methods --- */
        public ChannelActionResult ShareFile(string userID, string channelName, string fileName, byte[] fileBytes)
        {
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName) || fileBytes == null)
            {
                return new ChannelActionResult { Success = false, Message = "Invalid file share request." };
            }

            string cleanUserID = userID.Trim();

            // CHannel Validation
            if (!IsMemberOfChannel(cleanUserID, channelName))
            {
                return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
            }

            // Extension type validation
            string ext = System.IO.Path.GetExtension(fileName);
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt" };
            if (!allowed.Contains(ext))
            {
                return new ChannelActionResult { Success = false, Message = "That file type is not allowed." };
            }

            // File size validation
            if (fileBytes.Length > 2 * 1024 * 1024)
            {
                return new ChannelActionResult { Success = false, Message = "File exceeds the 2 MB limit." };
            }

            sharedFiles.AddFile(new FileStruct(fileName, cleanUserID, channelName, fileBytes));

            return new ChannelActionResult { Success = true, Message = "File shared successfully." };
        }

        public List<SharedFileInformation> GetSharedFiles(string channelName)
        {
            List<SharedFileInformation> result = new List<SharedFileInformation>();

            var filesForChannel = sharedFiles.GetFilesForChannel(channelName);

            foreach (var file in filesForChannel)
            {
                SharedFileInformation info = new SharedFileInformation();
                info.FileID = file.GetFileID();
                info.FileName = file.GetFileName();
                info.SharedBy = file.GetSharedBy();

                result.Add(info);
            }

            return result;
        }

        public FileDownloadResult DownloadFile(string userID, Guid fileID)
        {
            string cleanUserID = (userID ?? "").Trim(); // Default to ""
            FileStruct file = sharedFiles.GetFile(fileID);

            if (file == null)
            {
                return new FileDownloadResult { Success = false, Message = "That file no longer exists." };
            }

            if (!IsMemberOfChannel(userID, file.GetChannelName()))
            {
                return new FileDownloadResult { Success = false, Message = "You are not a member of that channel." };
            }

            return new FileDownloadResult { Success = true, FileName = file.GetFileName(), FileBytes = file.GetFileBytes() };
        }

        /* -- Private Messaging Methods --- */
        public ChannelActionResult SendPrivateMessage(string senderID, string recieverID, string message)
        {
            if (string.IsNullOrWhiteSpace(senderID) || string.IsNullOrWhiteSpace(recieverID) || string.IsNullOrWhiteSpace(message))
            {
                return new ChannelActionResult { Success = false, Message = "Invalid private message request." };
            }

            string cleanSender = senderID.Trim();
            string cleanRecipient = recieverID.Trim();

            // check - both users must be signed in
            lock (usersLock) 
            {
                if (!signedInUsers.Contains(cleanSender) || !signedInUsers.Contains(cleanRecipient))
                {
                    return new ChannelActionResult { Success = false, Message = "Both users must be signed in." };
                }
            }

            // check - must be members of the same channel
            lock (membershipLock)
            {
                string senderChannel;
                string recipientChannel;

                bool senderFound = userChannels.TryGetValue(cleanSender, out senderChannel);
                bool recipientFound = userChannels.TryGetValue(cleanRecipient, out recipientChannel);

                if (!senderFound || !recipientFound || senderChannel != recipientChannel)
                {
                    return new ChannelActionResult { Success = false, Message = "You must be in the same channel as that user to message them." };
                }
            }

            // then add message
            privateConversations.AddMessage(cleanSender, cleanRecipient, message);

            return new ChannelActionResult { Success = true, Message = "Private message sent." };
        }

        public List<ChatMessage> GetPrivateMessages(string userOneID, string userTwoID)
        {
            // send the whole conversation
            List<MessageStruct> sourceMessages = privateConversations.GetMessagesSince(userOneID, userTwoID, 0);
            return ConvertMessages(sourceMessages);
        }

        public List<string> GetPrivateConversationPartners(string userID)
        {
            List<String> partners = new List<string>();

            if (!string.IsNullOrWhiteSpace(userID))
            {
                partners = privateConversations.GetPartners(userID.Trim());
            }

            return partners;
        }


        /* -- Helper Methods --- */
        private bool IsMemberOfChannel(string userID, string channelName)
        {
            string cleanUserID = (userID ?? "").Trim();

            lock (membershipLock)
            {
                string actualChannel;
                bool found = userChannels.TryGetValue(cleanUserID, out actualChannel);

                if (!found) {
                    return false;
                }

                if (actualChannel != channelName) {
                    return false;
                }

                return true;
            }
        }

        private static List<ChatMessage> ConvertMessages(List<MessageStruct> sourceMessages)
        {
            List<ChatMessage> result = new List<ChatMessage>();
            foreach (MessageStruct source in sourceMessages)
            {
                ChatMessage converted = new ChatMessage();
                converted.SenderId = source.SenderId;
                converted.Text = source.Text;
                converted.Timestamp = source.Timestamp;
                result.Add(converted);
            }
            return result;
        }

        public List<string> GetMemberList(string channelName)
        {
            List<string> memberList = new List<string>();

            lock (membershipLock)
            {
                foreach (KeyValuePair<string, string> result in userChannels)
                {
                    if (result.Value == channelName)
                    {
                        memberList.Add(result.Key);
                    }
                }
            }

            return memberList;
        }
    }
}
