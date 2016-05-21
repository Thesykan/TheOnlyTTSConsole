using DotNetOpenAuth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsole
{
    public class GetAuthToken
    {
        public void start(String p_userName, String p_password)
        {
            var authServer = new DotNetOpenAuth.OAuth2.AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(@"https://api.twitch.tv/kraken/oauth2/authorize"),
            };

            authServer.TokenEndpoint = new Uri(@"https://api.twitch.tv/kraken/oauth2/token");

            try
            {
                var client = new DotNetOpenAuth.OAuth2.UserAgentClient(authServer, p_userName, p_password);
                OAuthUtilities.SplitScopes("chat");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
