using System;
using System.IO;
using System.Text;

namespace ASTools.Validator
{
    public class ValidatorUtils
    {
        public ValidatorUtils()
        {
        }

        public static bool IsMixamoFbx(string fbxPath)
        {
            FileStream fileStream = new FileStream(fbxPath, FileMode.Open);
            if (fileStream.Length < (long)1218)
            {
                return false;
            }
            byte[] numArray = new byte[10];
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                binaryReader.BaseStream.Seek((long)1218, SeekOrigin.Begin);
                binaryReader.Read(numArray, 0, 10);
            }
            if (Encoding.ASCII.GetString(numArray).Contains("mixamo"))
            {
                return true;
            }
            return false;
        }
    }
}