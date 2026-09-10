using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataLibrary;
using ServerTier;
using System.ServiceModel;

namespace DuplexClient
{
    [CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]

    public class ClientUpdateHandler : ClientUpdateCallback
    {
        public event EventHandler<List<string>> ChannelListUpdatedReceived;
        public event EventHandler<ChannelMembersUpdatedEventArgs> ChannelMembersUpdatedReceived;
        public event EventHandler<PublicMessageReceivedEventArgs> PublicMessageReceivedReceived;
        public event EventHandler<SharedFilesUpdatedEventArgs> SharedFilesUpdatedReceived;
        public event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceivedReceived;

        public void ChannelListUpdated(List<string> channels)
        {
            ChannelListUpdatedReceived?.Invoke(this, channels);
        }

        public void ChannelMembersUpdated(string channelName, List<string> members)
        {
            ChannelMembersUpdatedReceived?.Invoke(this, new ChannelMembersUpdatedEventArgs(channelName, members));
        }

        public void PublicMessageReceived(string channelName, ChatMessage message)
        {
            PublicMessageReceivedReceived?.Invoke(this, new PublicMessageReceivedEventArgs(channelName, message));
        }

        public void SharedFilesUpdated(string channelName, List<SharedFileInformation> files)
        {
            SharedFilesUpdatedReceived?.Invoke(this, new SharedFilesUpdatedEventArgs(channelName, files));
        }

        public void PrivateMessageReceived(string senderID, string recipientID, ChatMessage message)
        {
            PrivateMessageReceivedReceived?.Invoke(this, new PrivateMessageReceivedEventArgs(senderID, recipientID, message));
        }
    }

    public class ChannelMembersUpdatedEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public List<string> Members { get; }

        public ChannelMembersUpdatedEventArgs(string channelName, List<string> members)
        {
            ChannelName = channelName;
            Members = members;
        }
    }

    public class PublicMessageReceivedEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public ChatMessage Message { get; }

        public PublicMessageReceivedEventArgs(string channelName, ChatMessage message)
        {
            ChannelName = channelName;
            Message = message;
        }
    }

    public class SharedFilesUpdatedEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public List<SharedFileInformation> Files { get; }

        public SharedFilesUpdatedEventArgs(string channelName, List<SharedFileInformation> files)
        {
            ChannelName = channelName;
            Files = files;
        }
    }

    public class PrivateMessageReceivedEventArgs : EventArgs
    {
        public string SenderID { get; }
        public string RecipientID { get; }
        public ChatMessage Message { get; }

        public PrivateMessageReceivedEventArgs(string senderID, string recipientID, ChatMessage message)
        {
            SenderID = senderID;
            RecipientID = recipientID;
            Message = message;
        }
    }
}