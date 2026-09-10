using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataLibrary;
using System.ServiceModel;

namespace ServerTier
{
    [ServiceContract(CallbackContract = typeof(ClientUpdateCallback))]
    public interface DuplexServerInterface
    {
        [OperationContract]
        SignInResult SignIn(string userID);

        [OperationContract]
        ChannelActionResult SignOut(string userID);

        [OperationContract]
        ChannelActionResult JoinChannel(string userID, string channelName);

        [OperationContract]
        ChannelActionResult LeaveChannel(string userID);

        [OperationContract]
        ChannelActionResult CreateChannel(string userID, string channelName);

        [OperationContract]
        List<string> GetChannelList();

        [OperationContract]
        List<string> GetMemberList(string channelName);

        [OperationContract]
        ChannelActionResult ShareFile(
            string userID,
            string channelName,
            string fileName,
            byte[] fileBytes);

        [OperationContract]
        List<SharedFileInformation> GetSharedFiles(string channelName);

        [OperationContract]
        FileDownloadResult DownloadFile(string userID, Guid fileID);

        [OperationContract]
        ChannelActionResult SendMessage(
            string userID,
            string channelName,
            string message
        );

        /** PRIVATE MESSAGING */
        [OperationContract]
        ChannelActionResult SendPrivateMessage(string senderID, string recipientID, string message);

        [OperationContract]
        List<ChatMessage> GetPrivateMessages(string userID, string otherUserID);

        [OperationContract]
        List<string> GetPrivateConversationPartners(string userID);
    }
}
