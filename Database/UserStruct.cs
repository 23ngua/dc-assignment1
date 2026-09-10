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
        // private bool SignedInStatus; (ADD IF NEED)
        
        public UserStruct(string userID)
        {
            UserID = userID;
        }
        public string GetName() { return UserID; }
    }
}
