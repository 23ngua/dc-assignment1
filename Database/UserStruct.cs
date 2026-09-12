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
