using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class UserStruct
    {

        private string UserID;
        private bool SignedIn;
        // private bool SignedInStatus; (ADD IF NEED)
        
        public UserStruct(string userID)
        {
            UserID = userID;
            SignedIn = false;
        }
        public string GetName() { return UserID; }
        public void SignIn() { SignedIn = true; }
        public void SignOut() { SignedIn = false; }
    }
}
