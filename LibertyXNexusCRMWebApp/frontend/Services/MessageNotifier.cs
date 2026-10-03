namespace frontend.Services
{
    /// <summary>
    /// A simple notifier class that allows components to subscribe to message sent events.
    /// </summary>
    public class MessageNotifier
    {
        public event Action<int>? MessageSent;

        public void NotifyMessageSent(int clientId)
        {
            MessageSent?.Invoke(clientId);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
