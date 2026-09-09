using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChatResult;
using System.ServiceModel;

namespace ChatServerTier
{
    public interface ClientUpdateCallback
    {
        [OperationContract(IsOneWay = true)]
        void ChannelListUpdated(List<string> channels);
        
        [OperationContract(IsOneWay = true)]
        void ChannelMembersUpdated(string channelName, List<string> members);
        
        [OperationContract(IsOneWay = true)]
        void PublicMessageReceived(string channelName, ChatMessage message);
        
        [OperationContract(IsOneWay = true)]
        void SharedFilesUpdated(string channelName, List<SharedFileInformation> files);

        [OperationContract(IsOneWay = true)]
        void PrivateMessageReceived(string senderID, string recipientID, ChatMessage message);
    }
}