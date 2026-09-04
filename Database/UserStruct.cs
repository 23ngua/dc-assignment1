using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    internal class UserStruct
    {
        private Guid UserID;
        private string Name;
        // private bool SignedInStatus; (ADD IF NEED)
        
        public UserStruct(string name)
        {
            UserID = Guid.NewGuid();
            Name = name;
        }
        public Guid GetUserID() { return UserID; }
        public string GetName() { return Name; }
    }
}
