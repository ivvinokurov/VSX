using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSException:Exception
    {
        public int ErrorCode = 0;
        public new string Message = "";
        

        /// <summary>
        /// Thow exception
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message_ext">Message to append</param>
        public VSException(int code, string message_ext = "")
            : base(DEFS.prefix + code.ToString("D4") + " " + (GetMessage(code) + " " + message_ext).Trim())
        {
            ErrorCode = code;
            Message = DEFS.prefix + code.ToString("D4") + " " + (GetMessage(code) + " " + message_ext).Trim();
        }

        /// <summary>
        /// Get message by code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string GetMessage(int code)
        {
            return DEFS.ERROR_MESSAGES[code];
        }
    }
}
