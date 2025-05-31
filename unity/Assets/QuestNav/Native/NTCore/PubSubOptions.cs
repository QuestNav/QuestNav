namespace QuestNav.Native.NTCore
{
    public struct PubSubOptions
    {
        public static PubSubOptions AllDefault { get; } =
            new PubSubOptions()
            {
                Periodic = 0.01,
                SendAll = true,
                KeepDuplicates = true,
            };

        public double Periodic { get; set; }
        public bool SendAll { get; set; }
        public bool KeepDuplicates { get; set; }

        public unsafe NativePubSubOptions ToNative()
        {
            NativePubSubOptions native = new NativePubSubOptions
            {
                structSize = (uint)sizeof(NativePubSubOptions),
                periodic = Periodic,
                sendAll = SendAll ? 1 : 0,
                keepDuplicates = KeepDuplicates ? 1 : 0,
            };

            return native;
        }
    }
}
