using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ConnectionConfig
    {
        /// <summary>
        /// Dictionary of client properties to be sent to the server.
        /// </summary>
        public IDictionary<String, object> ClientProperties { get; set; }

        /// <summary>
        /// Password to use when authenticating to the server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Maximum channel number to ask for.
        /// </summary>
        public ushort RequestedChannelMax { get; set; }

        /// <summary>
        /// Frame-max parameter to ask for (in bytes).
        /// </summary>
        public uint RequestedFrameMax { get; set; }

        /// <summary>
        /// Heartbeat setting to request (in seconds).
        /// </summary>
        public ushort RequestedHeartbeat { get; set; }

        /// <summary>
        /// When set to true, background threads will be used for I/O and heartbeats.
        /// </summary>
        public bool UseBackgroundThreadsForIO { get; set; }

        public string HostName { get; set; }

        /// <summary>
        /// Username to use when authenticating to the server.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Virtual host to access during this connection.
        /// </summary>
        public string VirtualHost { get; set; }

        /// <summary>
        /// Advanced option.
        /// 
        /// What task scheduler should consumer dispatcher use.
        /// </summary>
        public TaskScheduler TaskScheduler { get; set; }

        /// <summary>
        /// Amount of time protocol handshake operations are allowed to take before
        /// timing out.
        /// </summary>
        public TimeSpan HandshakeContinuationTimeout { get; set; }

        /// <summary>
        /// Amount of time protocol  operations (e.g. <code>queue.declare</code>) are allowed to take before
        /// timing out.
        /// </summary>
        public TimeSpan ContinuationTimeout { get; set; }
    }
}
