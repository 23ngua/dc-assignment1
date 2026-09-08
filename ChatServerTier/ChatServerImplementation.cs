using ChatResult;
using Database;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;


namespace ChatServerTier
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]

    internal class ChatServerImplementation : ChatServerInterface
    {
        private static Channels channels = Channels.Instance;
        private static HashSet<string> signedInUsers = new HashSet<string>();
        private static Dictionary<string, string> userChannels = new Dictionary<string, string>();
        private static SharedFiles sharedFiles = SharedFiles.Instance;


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
                        Log("User ID already taken. Sign in rejected");
                        return new SignInResult { Success = false, 
                            Message = "That user ID is already signed in." };
                    }
                    // The ID is available, so reserve it for this user
                    signedInUsers.Add(cleanUserID);
                }
                Log($"Login for user {cleanUserID} succesful");
                return new SignInResult { Success = true, 
                    Message = "Sign-in successful." };
            } else {
                Log($"Login attempt contained null or whitespace");
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
                        Log($"Signout for user {cleanUserID} sucessful");
                        return new ChannelActionResult { Success = true, 
                            Message = "Sucsessfully signed Out User" };
                    } else {
                        Log($"STATE ERROR. User {cleanUserID} attempted to sign out but was never signed in");
                        return new ChannelActionResult { Success = false,
                            Message = "The user is not currently signed in." };
                    }
                }
            } else {
                Log($"Invalid username was for user signout");
                return new ChannelActionResult { Success = false, 
                    Message = "Please enter a valid username" };
            }
        }

        public ChannelActionResult JoinChannel(string userID, string channelName)
        {
            // Reject invalid input
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName))
            {
                Log($"User or channel was attempted to join a channel that had null or whitespace");
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
                    Log($"STATE ERROR. User {cleanUserID} attempted to join channel but isnt signed in");
                    return new ChannelActionResult { Success = false, 
                        Message = "The user is not currently signed in." };
                }
            }

            // Ensure requested channel still exists
            lock (channelsLock)
            {
                if (!channels.ContainsChannel(cleanChannelName))
                {
                    Log($"Channel {cleanChannelName} dosnt exist, couldnt join");
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
                    Log($"User {cleanUserID} tried to rejoin same channel already in");
                    return new ChannelActionResult { Success = false,
                        Message = "You are already in a channel." };
                }
                // Record user's current channel
                userChannels.Add(cleanUserID, cleanChannelName);
            }
            Log($"User {cleanUserID} sccessfully joined channel {cleanChannelName}");
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
                        Log($"STATE ERROR. User {cleanUserID} Cant leave channel due to not being signed in");
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
                        Log($"STATE ERROR. User {cleanUserID} cant leave channel as not appart of channel");
                        return new ChannelActionResult { Success = false,
                            Message = "You are not currently in a channel." };
                    }

                    // Remove the user's channel membership
                    userChannels.Remove(cleanUserID);
                }
                // Tell the client that leaving succeeded
                Log($"User {cleanUserID} successfully left channel");
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
                Log($"User tried to create new channel with null or whitespace");
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
                    Log($"User {cleanUserID} tried to create new channel that already exists");
                    return new ChannelActionResult { Success = false,
                        Message = "A channel with that name already exists." };
                } else {
                    channels.SetNewChannel(cleanChannelName);
                }
            }
            Log($"User {cleanUserID} successfully created channel {cleanChannelName}");
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
                Log($"User {userID} tried to send invalid message");
                return new ChannelActionResult { Success = false, Message = "Invalid message" };
            }

            if (!IsMemberOfChannel(userID, channelName))
            {
                Log($"INVALID STATE. User tried to send message to channel not currently appart of");
                return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
            }

            channels.AddMessage(channelName, userID.Trim(), message); // ADD MESSAGE YIPPEE
            Log($"User {userID} successfully sent message to channel {channelName}");
            return new ChannelActionResult { Success = true, Message = "Message Sent Successfully" };
        }

        public int GetMessageCount(string channelName)
        {
            Log($"Returned Message count");
            return channels.GetMessageCount(channelName);
        }

        public List<ChatMessage> GetMessagesSince(string channelName, int sinceIndex)
        {
            List<MessageStruct> sourceMessages = channels.GetMessagesSince(channelName, sinceIndex);
            List<ChatMessage> result = new List<ChatMessage>();

            // Conversion of list from ChatMessage to MessageStruct
            foreach (MessageStruct source in sourceMessages)
            {
                ChatMessage converted = new ChatMessage();
                converted.SenderId = source.SenderId;
                converted.Text = source.Text;
                converted.Timestamp = source.Timestamp;

                result.Add(converted);
            }
            Log($"Converted Messages from since");
            return result;
        }


        public ChannelActionResult ShareFile(string userID, string channelName, string fileName, byte[] fileBytes)
        {
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName) || fileBytes == null)
            {
                Log($"File share request contained null or whitespace");
                return new ChannelActionResult { Success = false, Message = "Invalid file share request." };
            }

            string cleanUserID = userID.Trim();

            // CHannel Validation
            if (!IsMemberOfChannel(cleanUserID, channelName))
            {
                Log($"STATE ERROR. User {cleanUserID} cant share file as not appart of channel");
                return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
            }

            // Extension type validation
            string ext = System.IO.Path.GetExtension(fileName);
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt" };
            if (!allowed.Contains(ext))
            {
                Log($"User {cleanUserID} Tried to upload invalid file type");
                return new ChannelActionResult { Success = false, Message = "That file type is not allowed." };
            }

            // File size validation
            if (fileBytes.Length > 2 * 1024 * 1024)
            {
                Log($"User {cleanUserID} tried to upload file excceding size limit");
                return new ChannelActionResult { Success = false, Message = "File exceeds the 2 MB limit." };
            }

            sharedFiles.AddFile(new FileStruct(fileName, cleanUserID, channelName, fileBytes));
            Log($"User {cleanUserID} successfully shared file {fileName} to channel {channelName}");
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
            Log($"Returned file list within channel {channelName}");
            return result;
        }

        public FileDownloadResult DownloadFile(string userID, Guid fileID)
        {
            string cleanUserID = (userID ?? "").Trim(); // Default to ""
            FileStruct file = sharedFiles.GetFile(fileID);

            if (file == null)
            {
                Log($"Attempted to download file that dosnt exist");
                return new FileDownloadResult { Success = false, Message = "That file no longer exists." };
            }

            if (!IsMemberOfChannel(userID, file.GetChannelName()))
            {
                Log($"STATE ERROR. User {userID} tried to download file when not appart of channel");
                return new FileDownloadResult { Success = false, Message = "You are not a member of that channel." };
            }
            Log($"User {cleanUserID} successfully downloaded file");
            return new FileDownloadResult { Success = true, FileName = file.GetFileName(), FileBytes = file.GetFileBytes() };
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
            Log($"List of members appart of channel retrieved ");
            return memberList;
        }

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

        private static uint LogNumber = 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void Log(string logString)
        {
            LogNumber++;
            var line = $"{LogNumber} : {logString}";
            File.AppendAllText("ServerLog.txt", line + Environment.NewLine);
        }
    }
}
