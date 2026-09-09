using System;
using System.Collections.Generic;

namespace Database
{
    /*
     * PrivateConversations.cs 
     * Each conversation is keyed by pair of the user IDs involved
     */
    public class PrivateConversations
    {
        public static PrivateConversations Instance { get; } = new PrivateConversations();

        // Key = a combined string of two user IDs
        private readonly Dictionary<string, List<MessageStruct>> _conversations = new Dictionary<string, List<MessageStruct>>();
        private readonly object _lock = new object();

        private const string Delimiter = "::";

        private PrivateConversations()
        {
        }

        // Builds key pair of users
        private string GetConversationKey(string userA, string userB)
        {
            int comparisonResult = string.Compare(userA, userB, StringComparison.OrdinalIgnoreCase); // Sort alphabetically

            string firstUser;
            string secondUser;

            if (comparisonResult <= 0)
            {
                firstUser = userA;
                secondUser = userB;
            }
            else
            {
                firstUser = userB;
                secondUser = userA;
            }

            string combinedKey = firstUser + Delimiter + secondUser;

            return combinedKey;
        }

        public void AddMessage(string senderId, string recipientId, string text)
        {
            string key = GetConversationKey(senderId, recipientId);

            lock (_lock)
            {
                if (!_conversations.ContainsKey(key))
                {
                    _conversations.Add(key, new List<MessageStruct>());
                }

                _conversations[key].Add(new MessageStruct(senderId, text, DateTime.UtcNow));
            }
        }

        public List<MessageStruct> GetMessagesSince(string userA, string userB, int sinceIndex)
        {
            string key = GetConversationKey(userA, userB);

            lock (_lock)
            {
                if (!_conversations.ContainsKey(key))
                {
                    return new List<MessageStruct>();
                }

                List<MessageStruct> allMessages = _conversations[key];

                if (sinceIndex >= allMessages.Count)
                {
                    return new List<MessageStruct>();
                }

                List<MessageStruct> newMessages = new List<MessageStruct>();
                for (int i = sinceIndex; i < allMessages.Count; i++)
                {
                    newMessages.Add(allMessages[i]);
                }

                return newMessages;
            }
        }

        public List<string> GetPartners(string userID)
        {
            List<string> partners = new List<string>();

            lock (_lock)
            {
                foreach (string key in _conversations.Keys)
                {
                    string[] parts = key.Split(new string[] { Delimiter }, StringSplitOptions.None); // Break by the set delimiter
                    string userOne = parts[0];
                    string userTwo = parts[1];

                    if (userOne == userID)
                    {
                        partners.Add(userTwo);
                    }
                    else if (userTwo == userID)
                    {
                        partners.Add(userOne);
                    }
                }
            }

            return partners;
        }
    }
}