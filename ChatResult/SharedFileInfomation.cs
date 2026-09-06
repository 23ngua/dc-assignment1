using System;
using System.Runtime.Serialization;

namespace ChatResult
{
    [DataContract]
    public class SharedFileInformation
    {
        [DataMember] 
        public Guid FileId { get; set; }

        [DataMember] 
        public string FileName { get; set; }

        [DataMember] 
        public string SharedBy { get; set; }

        public override string ToString()
        {
            return $"{FileName}";
        }
    }
}