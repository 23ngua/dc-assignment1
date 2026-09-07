using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChatShared
{
    [DataContract]
    public class FileUploadResult
    {
        [DataMember]
        public Boolean Success { get; set; }

        [DataMember]
        public String Message { get; set; }

        [DataMember]
        public String FileID { get; set; }
    }
}
