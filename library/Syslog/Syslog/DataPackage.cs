using System.Text;

namespace Syslog
{
    public class DataPackage
    {
        public byte[] Data;
        public string Text
        {
            get { return Encoding.ASCII.GetString(Data); }
        }
        public int Length
        {
            get { return Data.Length; }
        }

        public DataPackage(byte[] Data)
        {
            this.Data = Data;
        }
    }
}
