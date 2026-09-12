using DataLibrary;
using System;
using System.Collections.Generic;
using System.ServiceModel;


namespace ServerTier
{
    [ServiceContract]
    public interface ServerInterface
    {
        // Attepts to sign in using the supplied user ID
        [OperationContract]
        SignInResult SignIn(string userID);

        // Signs out the specified user and releases their user ID
        [OperationContract]
        ChannelActionResult SignOut(string userID);

        // Attempt to join the specified user to specified channel
        [OperationContract]
        ChannelActionResult JoinChannel(string userID, string channelName);

        // Removes the specified user from their current channel
        [OperationContract]
        ChannelActionResult LeaveChannel(string userID);

        // Attempts to create a new channel with the supplied name
        [OperationContract]
        ChannelActionResult CreateChannel(string userID, string channelName);

        // Returns a list of channel names for UI
        [OperationContract]
        List<string> GetChannelList();

        [OperationContract]
        List<string> GetMemberList(string channelName);

        /* --- FILE SHARING --- */
        [OperationContract]
        ChannelActionResult ShareFile(string userID, string channelName, string fileName, byte[] fileBytes);

        [OperationContract]
        List<SharedFileInformation> GetSharedFiles(string channelName);

        [OperationContract]
        FileDownloadResult DownloadFile(string userID, Guid fileID);

        /* --- MESSAGING --- */
        [OperationContract]
        int GetMessageCount(string channelName);

        [OperationContract]
        ChannelActionResult SendMessage(string userID, string channelName, string message);

        [OperationContract]
        List<ChatMessage> GetMessagesSince(string channelName, int sinceIndex);

        /* --- PRIVATE MESSAGING --- */
        [OperationContract]
        ChannelActionResult SendPrivateMessage(string senderID, string recipientID, string message);

        [OperationContract]
        List<ChatMessage> GetPrivateMessages(string userID, string otherUserID);

        [OperationContract]
        List<string> GetPrivateConversationPartners(string userID);
    }
}
