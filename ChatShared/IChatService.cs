using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

/** IChatService.cs - An interface that defines the WCF service contract shared 
 *                    between the chat server and clients.
 */

namespace ChatShared
{
    // Makes this interface as a WCF service contract
    [ServiceContract]
    public interface IChatService
    {
        // Attepts to sign in using the supplied user ID
        [OperationContract]
        SignInResult SignIn(string userId);
    }
}
