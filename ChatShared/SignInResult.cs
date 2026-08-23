using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

/** SignInResult.cs - A shared WCF data object returned by server after sign-in request.
 *                  - Contains whether operation succeeded and message client can display.
 */

namespace ChatShared
{
    // Represents result returned by server after sign-in attempt
    [DataContract]
    public class SignInResult
    {
        // True when user ID accepted by server
        [DataMember]
        public bool Success { get; set; }

        // Gives client a readable explanation of result
        [DataMember]
        public string Message { get; set; }
    }
}
