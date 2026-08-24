using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatShared;

/**
 * ChatService.cs - Implements the WCF operations defined
 *                  in the shared IChatService contract.
 */

namespace ChatServer
{
    // Provides the server-side implementation of the chat service
    public class ChatService : IChatService
    {
        // Stores all user IDs that are currently signed in
        // Static allows list to be shared by all ChatService instances
        private static readonly HashSet<string> signedInUsers = new HashSet<string>();

        // Protect shared user list when multiple clients access it
        private static readonly object usersLock = new object();

        // Handles a sign-in request from a client
        public SignInResult SignIn(string userId)
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
    }
}
