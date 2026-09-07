using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Database
{
    public class Channels
    {
        private Dictionary<String, ChannelStruct> _channel;
        public static Channels Instance { get; } = new Channels();

        private Channels()
        {
            _channel = new Dictionary<String, ChannelStruct>();
            // Creates First General Channel
            var temp = new ChannelStruct();
            _channel.Add("general", temp);
        }
        //Getters for data
        public List<FileStruct> GetFiles(string name) { return _channel[name].GetFile(); }
        //public List<string> GetMembers(string name) { return _channel[name].GetMembers(); }
       
        // Setters for data
        //public void AddNewMember(string name, string userID) { _channel[name].AddMember(userID); }
        //public void RemoveMember(string name, string userID) { _channel[name].PopMember(userID); }
        public void SetFile() { }

        
        // Gets # of channels in list
        public int GetNumChannels()
        {
            return _channel.Count;
        }

        public List<string> GetChannelList()
        {
            return new List<string>(this._channel.Keys);
        }

        // Adds new channel to list
        public void SetNewChannel(string name)
        {
            var temp = new ChannelStruct();
            _channel.Add(name, temp);
        }

        //Checks if channel exists
        public bool ContainsChannel(string name)
        {
            return _channel.ContainsKey(name);
        }
        public void AddMessageToList(string channelName, string msg)
        {
            _channel[channelName].AddNewMessage(msg);
        }
        public string GetNewMessageFromList(string channelName)
        {
            return _channel[channelName].GetNewMessage();
        }
    }
}
