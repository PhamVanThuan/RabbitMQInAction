using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MessageException:Exception
    {
        public MessageException(string message) : base(message) { }
        public MessageException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class NoRPCConsumeException:Exception
    {
        public NoRPCConsumeException(string message) :base(message){ }
        public NoRPCConsumeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
