using System.Text;
using System.Security.Cryptography;

namespace API.Services.Implementations
{
    public static class InvitationTokenHasher
    {
        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
