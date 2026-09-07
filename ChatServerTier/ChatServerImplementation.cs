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


        private static readonly object usersLock = new object();    // Protects signedInUsers
        private static readonly object membershipLock = new object(); // Protects user-to-channel membership state
        private static readonly object channelsLock = new object(); // Protect the shared channel list


        public SignInResult SignIn(string userID)
        {
            if(!string.IsNullOrWhiteSpace(userID))
            {
                string cleanUserId = userID.Trim();
                lock (usersLock)
                {
                    // Reject request if this ID is already signed in
                    if (signedInUsers.Contains(cleanUserId))
                    {
                        return new SignInResult { Success = false, 
                            Message = "That user ID is already signed in." };
                    }
                    // The ID is available, so reserve it for this user
                    signedInUsers.Add(cleanUserId);
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
                string cleanUserId = userID.Trim();

                lock (usersLock)
                {
                    if (signedInUsers.Contains(cleanUserId))    // Checks if user is currently signed in
                    {
                        lock (membershipLock)   // Maybe move outside lock, but stops case where new user takes id before 
                        {
                            if (userChannels.ContainsKey(cleanUserId))
                            {
                                userChannels.Remove(cleanUserId);
                            }
                        }
                        signedInUsers.Remove(cleanUserId);
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
        public ChannelActionResult SendMessage(string channelName, string message)
        {
            Console.WriteLine(message);
            channels.AddMessageToList(channelName, message);
            return new ChannelActionResult { Success = true,
                Message = "Message Sent Successfully" };
        }
        public string GetNewestMessage(string channelName)
        {
            string temp = channels.GetNewMessageFromList(channelName);
            Console.WriteLine(temp);
            return temp;
        }
        */

        public ChannelActionResult ShareFile(string userId, string channelName, string fileName, byte[] fileBytes)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(channelName) || fileBytes == null)
            {
                return new ChannelActionResult { Success = false, Message = "Invalid file share request." };
            }

            string cleanUserId = userId.Trim();

            // CHannel Validation

            lock (membershipLock)
            {
                if (!userChannels.TryGetValue(cleanUserId, out string actualChannel) || actualChannel != channelName)
                {
                    return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
                }
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

            sharedFiles.AddFile(new FileStruct(fileName, cleanUserId, channelName, fileBytes));

            return new ChannelActionResult { Success = true, Message = "File shared successfully." };
        }

        public List<SharedFileInformation> GetSharedFiles(string channelName)
        {
            return sharedFiles.GetFilesForChannel(channelName)
                .Select(f => new SharedFileInformation { FileId = f.GetFileID(), FileName = f.GetFileName(), SharedBy = f.GetSharedBy() }).ToList();
        }

        public FileDownloadResult DownloadFile(string userId, Guid fileId)
        {
            string cleanUserId = (userId ?? "").Trim(); // Default to ""
            FileStruct file = sharedFiles.GetFile(fileId);

            if (file == null)
            {
                return new FileDownloadResult { Success = false, Message = "That file no longer exists." };
            }
                
            lock (membershipLock)
            {
                if (!userChannels.TryGetValue(cleanUserId, out string actualChannel) || actualChannel != file.GetChannelName())
                {
                    return new FileDownloadResult { Success = false, Message = "You are not a member of that channel." };
                }
            }

            return new FileDownloadResult { Success = true, FileName = file.GetFileName(), FileBytes = file.GetFileBytes() };
        }


    }
}
