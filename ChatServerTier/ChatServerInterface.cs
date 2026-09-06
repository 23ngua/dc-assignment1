using ChatResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerTier
{
    [ServiceContract]
    public interface ChatServerInterface
    {
        // Attepts to sign in using the supplied user ID
        [OperationContract]
        SignInResult SignIn(string userId);

        // Signs out the specified user and releases their user ID
        [OperationContract]
        ChannelActionResult SignOut(string userId);

        // Attempt to join the specified user to specified channel
        [OperationContract]
        ChannelActionResult JoinChannel(string userId, string channelName);

        // Removes the specified user from their current channel
        [OperationContract]
        ChannelActionResult LeaveChannel(string userId);

        // Attempts to create a new channel with the supplied name
        [OperationContract]
        ChannelActionResult CreateChannel(string userId, string channelName);

        // Returns a list of channel names for UI
        [OperationContract]
        List<string> GetChannelList();

        /* --- FILE SHARING --- */

        [OperationContract]
        ChannelActionResult ShareFile(string userId, string channelName, string fileName, byte[] fileBytes);

        [OperationContract]
        List<SharedFileInformation> GetSharedFiles(string channelName);

        [OperationContract]
        FileDownloadResult DownloadFile(string userId, Guid fileId);

        // Start Private Message
    }
}
