namespace frontend.Services
{
    public class MessageNotifier
    {
        public event Action<int>? MessageSent;

        public void NotifyMessageSent(int clientId)
        {
            MessageSent?.Invoke(clientId);
        }
    }
}
