using ChatShared;
using ChatShared.FileSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

/*
 * ChatService.cs - Implements the WCF operations defined
 *                  in the shared IChatService contract.
 */

namespace ChatServer
{
    // Provides the server-side implementation of the chat service
    public class ChatService : IChatService
    {
        // Stores all user IDs that are currently signed in - static allows list to be shared by all ChatService instances.
        private static readonly HashSet<string> signedInUsers = new HashSet<string>();
        private static readonly object usersLock = new object(); // Protect shared user list when multiple clients access it
        private static readonly List<ChannelInfo> channels = new List<ChannelInfo> // Store all channels that currently exist on the server
        {
            new ChannelInfo { Name = "General" } // Temporary test channel until channel creation is implemented
        };
        
        // Tracks which channel each signed-in user currently belongs to
        private static readonly Dictionary<string, string> userChannels = new Dictionary<string, string>();

        private static readonly object membershipLock = new object(); // Protects user-to-channel membership state

        private static readonly object channelsLock = new object(); // Protect the shared channel list when multiple clients access it

        // FILE RELATED VARIABLES
        private static readonly Dictionary<string, SharedFileInfo> fileMetadata = new Dictionary<string, SharedFileInfo>();
        private static readonly Dictionary<string, byte[]> fileContents = new Dictionary<string, byte[]>(); // Stores raw bytes of each shared file, key is FileID
        private static readonly object filesLock = new object(); // lock to protect the shared file dictionaries

        public SignInResult SignIn(string userId) // Handles a sign-in request from a client
        {
            // Reject an empty user ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new SignInResult
                {
                    Success = false,
                    Message = "Please enter a user ID."
                };
            }

            // Remove accidental spaces before or after user ID
            string cleanUserId = userId.Trim();

            // Only one client at a time may check/change shared user list
            lock (usersLock)
            {
                // Reject request if this ID is already signed in
                if (signedInUsers.Contains(cleanUserId))
                {
                    return new SignInResult
                    {
                        Success = false,
                        Message = "That user ID is already signed in."
                    };
                }

                // The ID is available, so reserve it for this user
                signedInUsers.Add(cleanUserId);
            }

            // Tell client that sign-in was successful
            return new SignInResult
            {
                Success = true,
                Message = "Sign-in successful."
            };
        }

        // Returns the channels that currently exist on the server
        public List<ChannelInfo> GetChannels()
        {
            // Protect the shared channel list while reading it
            lock (channelsLock)
            {
                // Return a separate copy of current channel information
                List<ChannelInfo> channelList = new List<ChannelInfo>();

                foreach (ChannelInfo channel in channels)
                {
                    channelList.Add(new ChannelInfo
                    {
                        Name = channel.Name
                    });
                }

                return channelList;
            }
        }

        // Attempts to join a signed-in user to an existing channel
        public ChannelActionResult JoinChannel(string userId, string channelName)
        {
            // Reject invalid input
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(channelName))
            {
                return new ChannelActionResult
                {
                    Success = false,
                    Message = "A valid user ID and channel must be provided."
                };
            }

            // Remove accidental spaces
            string cleanUserId = userId.Trim();
            string cleanChannelName = channelName.Trim();

            // Ensure user is actually signed in
            lock (usersLock)
            {
                if (!signedInUsers.Contains(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "The user is not currently signed in."
                    };
                }
            }

            // Ensure requested channel still exists
            lock (channelsLock)
            {
                bool channelExists = false;

                foreach (ChannelInfo channel in channels)
                {
                    if (channel.Name == cleanChannelName)
                    {
                        channelExists = true;
                        break;
                    }
                }

                if (!channelExists)
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "That channel no longer exists."
                    };
                }
            }

            // Protect membership state while checking and updating it
            lock (membershipLock)
            {
                // A user may belong to at most one channel
                if (userChannels.ContainsKey(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "You are already in a channel."
                    };
                }

                // Record user's current channel
                userChannels.Add(cleanUserId, cleanChannelName);
            }

            return new ChannelActionResult
            {
                Success = true,
                Message = "Joined channel successfully."
            };
        }

        // Removes a signed-in user from their current channel
        public ChannelActionResult LeaveChannel(string userId)
        {
            // Reject an empty or invalid user ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ChannelActionResult
                {
                    Success = false,
                    Message = "A valid user ID must be provided."
                };
            }

            // Remove accidental spaces around the user ID
            string cleanUserId = userId.Trim();

            // Make sure user is currently signed in
            lock (usersLock)
            {
                if (!signedInUsers.Contains(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "The user is not currently signed in."
                    };
                }
            }

            // Protect shared membership state while changing it
            lock (membershipLock)
            {
                // The user must currently belong to a channel
                if (!userChannels.ContainsKey(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "You are not currently in a channel."
                    };
                }

                // Remove the user's channel membership
                userChannels.Remove(cleanUserId);
            }

            // Tell the client that leaving succeeded
            return new ChannelActionResult
            {
                Success = true,
                Message = "Left channel successfully."
            };
        }

        // Attempts to create a new channel on the server
        public ChannelActionResult CreateChannel(string userId, string channelName)
        {
            // Reject missing user IDs or channel names
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(channelName))
            {
                return new ChannelActionResult
                {
                    Success = false,
                    Message = "A valid user ID and channel name must be provided."
                };
            }

            // Remove accidental spaces around the supplied values
            string cleanUserId = userId.Trim();
            string cleanChannelName = channelName.Trim();

            // Make sure the requesting user is currently signed in
            lock (usersLock)
            {
                if (!signedInUsers.Contains(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "The user is not currently signed in."
                    };
                }
            }

            // Protect the shared channel list while checking and adding
            lock (channelsLock)
            {
                // Channel names must be unique
                foreach (ChannelInfo channel in channels)
                {
                    if (string.Equals(channel.Name, cleanChannelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new ChannelActionResult
                        {
                            Success = false,
                            Message = "A channel with that name already exists."
                        };
                    }
                }

                // Add the newly created channel to server state
                channels.Add(new ChannelInfo
                {
                    Name = cleanChannelName
                });
            }

            // Tell that client the creation suceeded
            return new ChannelActionResult
            {
                Success = true,
                Message = "Channel created successfully."
            };
        }

        // Signs out a user and releases their user ID
        public ChannelActionResult SignOut(string userId)
        {
            // Reject an empty or invalid user ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ChannelActionResult
                {
                    Success = false,
                    Message = "A valid user ID must be provided."
                };
            }

            // Remove accidental spaces around the supplied user ID
            string cleanUserId = userId.Trim();

            // First make sure the user is currently signed in
            lock (usersLock)
            {
                if (!signedInUsers.Contains(cleanUserId))
                {
                    return new ChannelActionResult
                    {
                        Success = false,
                        Message = "The user is not currently signed in."
                    };
                }
            }

            // Remove the user from their current channel, if they have one..
            lock (membershipLock)
            {
                if (userChannels.ContainsKey(cleanUserId))
                {
                    userChannels.Remove(cleanUserId);
                }
            }

            // Finally release the user ID from the signed-in user list
            lock (usersLock)
            {
                signedInUsers.Remove(cleanUserId);
            }

            // Tell the client that sign-out succeeded
            return new ChannelActionResult
            {
                Success = true,
                Message = "Signed out successfully."
            };
        }

        // FILE SHARING METHODS
        /* UploadFile()
         * Performs file validity checks and uploads file with a unique id
         */
        public FileUploadResult UploadFile(string userID, string channelName, string fileName, byte[] content)
        {
            {
                FileUploadResult upResult = new FileUploadResult();

                // Check missing input
                if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(channelName) || string.IsNullOrWhiteSpace(fileName) || content == null)
                {
                    upResult.Success = false;
                    upResult.Message = "A valid user, channel, file name and file content must be provided.";
                    return upResult;
                }

                string cleanUserId = userID.Trim();
                string cleanChannelName = channelName.Trim();
                string cleanFileName = fileName.Trim();

                // Check uploader is signed in and a member of the channel
                lock (membershipLock)
                {
                    if (!userChannels.TryGetValue(cleanUserId, out string actualChannel) || actualChannel != cleanChannelName)
                    {
                        upResult.Success = false;
                        upResult.Message = "Must be a valid memeber of channel to upload file.";
                        return upResult;
                    }
                }

                // Check file type 
                if (!FileSharingRules.IsExtensionAllowed(cleanFileName))
                {
                    upResult.Success = false;
                    upResult.Message = "That file type is not allowed. Allowed types: " + string.Join(", ", FileSharingRules.AllowedExtensions);
                    return upResult;
                }

                // Check file size
                if (content.Length > FileSharingRules.MaxFileSizeBytes)
                {
                    upResult.Success = false;
                    upResult.Message = "File was too large. Max size: 2MB";
                    return upResult;
                }


                // Finally, Store the file
                string fileID = Guid.NewGuid().ToString(); // create unique id

                lock (filesLock)
                {
                    fileMetadata[fileID] = new SharedFileInfo
                    {
                        FileID = fileID,
                        FileName = cleanFileName,
                        SharedBy = cleanUserId,
                        ChannelName = cleanChannelName
                    };

                    fileContents[fileID] = content;
                }

                upResult.Success = true;
                upResult.Message = "File shared successfullly.";
                upResult.FileID = fileID;

                return upResult;
            }
        }

        /* GetChannelFiles()
         * Returns the metadata (not the bytes) for every file shared into a channel
         */
        public List<SharedFileInfo> GetChannelFiles(string channelName)
        {
            List<SharedFileInfo> result = new List<SharedFileInfo>();

            if (string.IsNullOrWhiteSpace(channelName))
            {
                return result;
            }

            string cleanChannelName = channelName.Trim();

            lock (filesLock)
            {
                foreach (SharedFileInfo file in fileMetadata.Values)
                {
                    if (file.ChannelName == cleanChannelName)
                    {
                        // returns a copy so callers can't change the server state
                        result.Add(new SharedFileInfo
                        {
                            FileID = file.FileID,
                            FileName = file.FileName,
                            SharedBy = file.SharedBy,
                            ChannelName = file.ChannelName
                        });
                    }
                }
            }

            return result;
        }

        /* DownloadFile()
         * Get bytes of a shared file, if the user is allowed to
         */
        public FileDownloadResult DownloadFile(string userID, string fileID)
        {
            FileDownloadResult downResult = new FileDownloadResult();

            // Check for valid inputs
            if (string.IsNullOrWhiteSpace(userID) || string.IsNullOrWhiteSpace(fileID))
            {
                downResult.Success = false;
                downResult.Message = "A valid user and file id must be provided.";

                return downResult;
            }

            string cleanUserId = userID.Trim();
            SharedFileInfo metadata;
            byte[] content;

            //Check if file exists
            lock (filesLock)
            {
                if (!fileMetadata.TryGetValue(fileID, out metadata) || !fileContents.TryGetValue(fileID, out content))
                {
                    downResult.Success = false;
                    downResult.Message = "File no longer exists on the server.";
                    return downResult;
                }
            }

            // Check if requester is a member of channel
            lock (membershipLock)
            {
                if (!userChannels.TryGetValue(cleanUserId, out string actualChannel) || actualChannel != metadata.ChannelName)
                {
                    downResult.Success = false;
                    downResult.Message = "You must be a member of the channel this file was shared to.";
                    return downResult;
                }
            }

            // Finally, Return file
            downResult.Success = true;
            downResult.Message = "File retrieved successfully.";
            downResult.FileName = metadata.FileName;
            downResult.Content = content;
            return downResult;
        }
    }
}

