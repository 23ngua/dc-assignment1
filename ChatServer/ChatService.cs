using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatShared;

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
    }
}
