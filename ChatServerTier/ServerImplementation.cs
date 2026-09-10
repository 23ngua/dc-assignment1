using DataLibrary;
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

namespace ServerTier
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]

    internal class ServerImplementation : ServerInterface, DuplexServerInterface
    {
        private static Channels channels = Channels.Instance;
        private static HashSet<string> signedInUsers = new HashSet<string>();
        private static Dictionary<string, string> userChannels = new Dictionary<string, string>();
        private static SharedFiles sharedFiles = SharedFiles.Instance;
        private static Dictionary<string, ClientUpdateCallback> registeredClientCallbacks = new Dictionary<string, ClientUpdateCallback>();
        private static PrivateConversations privateConversations = PrivateConversations.Instance;


        private static readonly object usersLock = new object();    // Protects signedInUsers
        private static readonly object membershipLock = new object(); // Protects user-to-channel membership state
        private static readonly object channelsLock = new object(); // Protect the shared channel list
        private static readonly object callbacksLock = new object();

        public SignInResult SignIn(string userID)
        {
            if(!string.IsNullOrWhiteSpace(userID))
            {
                string cleanUserID = userID.Trim();

                if(cleanUserID.Length >= 20)
                {
                    return new SignInResult
                    {
                        Success = false,
                        Message = "Username entered to long, please choose shorter Name"
                    };
                }

                ClientUpdateCallback callback = null;

                try
                {
                    callback = OperationContext.Current.GetCallbackChannel<ClientUpdateCallback>();
                }
                catch (InvalidOperationException)
                {
                    // polling clients don't provide callback channel
                }
                catch (InvalidCastException)
                {
                    // polling clients use non-duplex service contract
                }

                lock (usersLock)
                {
                    if (signedInUsers.Contains(cleanUserID))
                    {
                        return new SignInResult 
                        { 
                            Success = false, 
                            Message = "That user ID is already signed in." 
                        };
                    }

                    signedInUsers.Add(cleanUserID);

                    if (callback != null)
                    {
                        lock (callbacksLock)
                        {
                            registeredClientCallbacks[cleanUserID] = callback;
                        }

                        ICommunicationObject callbackChannel = callback as ICommunicationObject;

                        if (callbackChannel != null)
                        {
                            callbackChannel.Faulted += (sender, e) =>
                            {
                                RemoveDisconnectedClient(cleanUserID, callback);
                            };

                            callbackChannel.Closed += (sender, e) =>
                            {
                                RemoveDisconnectedClient(cleanUserID, callback);
                            };
                        }
                    }
                }

                return new SignInResult 
                { 
                    Success = true, 
                    Message = "Sign-in successful." 
                };
            } 
            else 
            {
                return new SignInResult 
                { 
                    Success = false, 
                    Message = "Please enter a valid username" 
                };
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
                        string previousChannelName = null;

                        lock (membershipLock)   // Maybe move outside lock, but stops case where new user takes id before 
                        {
                            if (userChannels.ContainsKey(cleanUserID))
                            {
                                previousChannelName = userChannels[cleanUserID];
                                userChannels.Remove(cleanUserID);
                            }
                        }

                        lock (callbacksLock)
                        {
                            registeredClientCallbacks.Remove(cleanUserID);
                        }

                        signedInUsers.Remove(cleanUserID);

                        if (previousChannelName != null)
                        {
                            PushChannelMembersUpdate(previousChannelName);
                        }

                        return new ChannelActionResult 
                        { 
                            Success = true, 
                            Message = "Successfully Signed Out User" 
                        };
                    } 
                    else 
                    {
                        return new ChannelActionResult 
                        { 
                            Success = false,
                            Message = "The user is not currently signed in." 
                        };
                    }
                }
            } 
            else 
            {
                return new ChannelActionResult 
                { 
                    Success = false, 
                    Message = "Please enter a valid username" 
                };
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

            PushChannelMembersUpdate(cleanChannelName);

            return new ChannelActionResult { Success = true, 
                Message = "Joined channel successfully." };
        }


        public ChannelActionResult LeaveChannel(string userID)
        {
            if (!string.IsNullOrWhiteSpace(userID))
            {
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

                string previousChannelName = null;

                // Protect shared membership state while changing it
                lock (membershipLock)
                {
                    // The user must currently belong to a channel
                    if (!userChannels.ContainsKey(cleanUserID))
                    {
                        return new ChannelActionResult { Success = false,
                            Message = "You are not currently in a channel." };
                    }

                    previousChannelName = userChannels[cleanUserID];
                    userChannels.Remove(cleanUserID);
                }

                PushChannelMembersUpdate(previousChannelName);

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

            if (cleanChannelName.Length >= 20)
            {
                return new ChannelActionResult
                {
                    Success = false,
                    Message = "Channel name entered is to long, please choose shorter name"
                };
            }
            lock (channelsLock)
            {
                if (channels.ContainsChannel(cleanChannelName))
                {
                    return new ChannelActionResult 
                    { 
                        Success = false,
                        Message = "A channel with that name already exists." 
                    };
                } 
                else 
                {
                    channels.SetNewChannel(cleanChannelName);
                }
            }

            PushChannelListUpdate();

            return new ChannelActionResult 
            { 
                Success = true,
                Message = "Channel created successfully" 
            };
        }

        private void PushChannelMembersUpdate(string channelName)
        {
            List<string> members = GetMemberList(channelName);
            List<KeyValuePair<string, ClientUpdateCallback>> callbacks = GetRegisteredCallbacksSnapshot();

            foreach (KeyValuePair<string, ClientUpdateCallback> client in callbacks)
            {
                if (!IsMemberOfChannel(client.Key, channelName))
                {
                    continue;
                }

                try
                {
                    client.Value.ChannelMembersUpdated(channelName, members);
                }
                catch (CommunicationException)
                {
                    RemoveDisconnectedClient(client.Key, client.Value);
                }
                catch (TimeoutException)
                {
                    RemoveDisconnectedClient(client.Key, client.Value);
                }
            }
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

            if(message.Length >= 100) 
            { 
                return new ChannelActionResult { Success = false, Message = "Message entered is to long" }; 
            }

            if (!IsMemberOfChannel(userID, channelName))
            {
                return new ChannelActionResult { Success = false, Message = "You are not a member of that channel." };
            }

            channels.AddMessage(channelName, userID.Trim(), message); // ADD MESSAGE YIPPEE

            ChatMessage newMessage = new ChatMessage
            {
                SenderId = userID.Trim(),
                Text = message,
                Timestamp = DateTime.Now
            };

            PushPublicMessage(channelName, newMessage);

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

            PushSharedFilesUpdate(channelName);

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

            ChatMessage newPrivateMessage = new ChatMessage { SenderId = cleanSender, Text = message, Timestamp = DateTime.Now };

            PushPrivateMessage(cleanSender, cleanRecipient, newPrivateMessage);

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

        private List<KeyValuePair<string, ClientUpdateCallback>> GetRegisteredCallbacksSnapshot()
        {
            lock (callbacksLock)
            {
                return registeredClientCallbacks.ToList();
            }
        }

        private void PushChannelListUpdate()
        {
            List<string> currentChannels = GetChannelList();
            List<KeyValuePair<string, ClientUpdateCallback>> callbacks = GetRegisteredCallbacksSnapshot();

            foreach (KeyValuePair<string, ClientUpdateCallback> client in callbacks)
            {
                try
                {
                    client.Value.ChannelListUpdated(currentChannels);
                }
                catch (CommunicationException)
                {
                    RemoveDisconnectedClient(client.Key, client.Value);
                }
                catch (TimeoutException)
                {
                    RemoveDisconnectedClient(client.Key, client.Value);
                }
            }
        }

        private void PushPublicMessage(string channelName, ChatMessage message)
        {
            List<KeyValuePair<string, ClientUpdateCallback>> callbacks = GetRegisteredCallbacksSnapshot();

            foreach (KeyValuePair<string, ClientUpdateCallback> client in callbacks)
            {
                if (!IsMemberOfChannel(client.Key, channelName)) { continue; }

                try { client.Value.PublicMessageReceived(channelName, message); }

                catch (CommunicationException) { RemoveDisconnectedClient(client.Key, client.Value); }

                catch (TimeoutException) { RemoveDisconnectedClient(client.Key, client.Value); }
            }
        }

        private void PushPrivateMessage(string senderID, string recipientID, ChatMessage message)
        {
            List<KeyValuePair<string, ClientUpdateCallback>> callbacks = GetRegisteredCallbacksSnapshot();

            foreach (KeyValuePair<string, ClientUpdateCallback> client in callbacks)
            {
                // Only sender and recipient should receive this PM
                if (client.Key != senderID && client.Key != recipientID) { continue; }

                try { client.Value.PrivateMessageReceived(senderID, recipientID, message); }

                catch (CommunicationException) { RemoveDisconnectedClient(client.Key, client.Value); }

                catch (TimeoutException) { RemoveDisconnectedClient(client.Key, client.Value); }
            }
        }

        private void PushSharedFilesUpdate(string channelName)
        {
            List<SharedFileInformation> files = GetSharedFiles(channelName);
            List<KeyValuePair<string, ClientUpdateCallback>> callbacks = GetRegisteredCallbacksSnapshot();

            foreach (KeyValuePair<string, ClientUpdateCallback> client in callbacks)
            {
                if (!IsMemberOfChannel(client.Key, channelName)) { continue; }

                try { client.Value.SharedFilesUpdated(channelName, files); }

                catch (CommunicationException) { RemoveDisconnectedClient(client.Key, client.Value); }

                catch(TimeoutException) { RemoveDisconnectedClient(client.Key, client.Value); }
            }
        }

        private void RemoveDisconnectedClient(string userID, ClientUpdateCallback callback)
        {
            string previousChannelName = null;

            lock (usersLock)
            {
                if (!signedInUsers.Contains(userID))
                {
                    return;
                }
                
                lock (membershipLock)
                {
                    if (userChannels.ContainsKey(userID))
                    {
                        previousChannelName = userChannels[userID];
                        userChannels.Remove(userID);
                    }
                }

                lock (callbacksLock)
                {
                    if (registeredClientCallbacks.ContainsKey(userID) && registeredClientCallbacks[userID] == callback)
                    {
                        registeredClientCallbacks.Remove(userID);
                    }
                }

                signedInUsers.Remove(userID);
            }

            if (previousChannelName != null)
            {
                PushChannelMembersUpdate(previousChannelName);
            }
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
