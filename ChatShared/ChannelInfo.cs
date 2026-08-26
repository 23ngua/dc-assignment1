using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

/*
 * ChannelInfo.cs - A shared WCF data object that contains basic information about a chat channel.
 */


namespace ChatShared
{
    // Represents one channel returned by the chat server
    [DataContract]
    public class ChannelInfo
    {
        // Stores the unique name of channel
        [DataMember]
        public string Name { get; set; }
    }
}
