namespace Notifications.SendingSimulator
{
    public sealed class SimulatedSendFailedException : Exception
    {
        public SimulatedSendFailedException(string message) : base(message) { }
    }
}
