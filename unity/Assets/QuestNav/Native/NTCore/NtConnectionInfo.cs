namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Managed copy of the parts of a native NT_ConnectionInfo that QuestNav uses.
    /// </summary>
    public readonly struct NtConnectionInfo
    {
        /// <summary>
        /// IP address of the remote end (the robot, for a client)
        /// </summary>
        public string RemoteIp { get; }

        /// <summary>
        /// Port of the remote end
        /// </summary>
        public uint RemotePort { get; }

        /// <summary>
        /// Negotiated protocol version, major in the high byte (0x0401 is 4.1)
        /// </summary>
        public uint ProtocolVersion { get; }

        public NtConnectionInfo(string remoteIp, uint remotePort, uint protocolVersion)
        {
            RemoteIp = remoteIp;
            RemotePort = remotePort;
            ProtocolVersion = protocolVersion;
        }

        /// <summary>
        /// Protocol version as "major.minor"
        /// </summary>
        public string ProtocolVersionString => $"{ProtocolVersion >> 8}.{ProtocolVersion & 0xff}";
    }
}
