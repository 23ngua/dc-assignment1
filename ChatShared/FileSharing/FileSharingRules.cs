using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatShared.FileSharing
{
    public static class FileSharingRules
    {
        public static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt" };

        public const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        public static bool IsExtensionAllowed(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string ext = System.IO.Path.GetExtension(fileName).ToLower();

            for (int i=0; i < AllowedExtensions.Length; i++)
            {
                if (ext == AllowedExtensions[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
