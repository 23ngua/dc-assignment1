using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

/*
 * ChannelActionResult.cs - Shared WCF result returned after a channel-related operation.
 */

namespace ChatShared
{
    // Represents whether a channel operation succeeded
    [DataContract]
    public class ChannelActionResult
    {
        // True when requested channel operation succeeds
        [DataMember]
        public bool Success { get; set; }

        // Gives the client a readable explanation of result
        [DataMember]
        public string Message { get; set; }
    }
}
