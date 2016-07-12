using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.GData.Client;
using Google.GData.Photos;

namespace PicasaConnector.Controllers
{
    public class AuthController : Controller
    {
        public ActionResult Index(string referrer, CancellationToken cancellationToken)
        {
            cancellationToken = cancellationToken == null ? CancellationToken.None : cancellationToken;

            var result =
                new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).AuthorizeAsync(cancellationToken)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

            if (result.Credential != null)
            {
                OAuth2Parameters parameters = new OAuth2Parameters();
                parameters.ClientId = ConfigurationManager.AppSettings["GoogleAppKey"];
                parameters.ClientSecret = ConfigurationManager.AppSettings["GoogleAppSecret"];
                parameters.Scope = result.Credential.Token.Scope;
                parameters.AccessToken = result.Credential.Token.AccessToken;
                parameters.RefreshToken = result.Credential.Token.RefreshToken;

                var picasaService = new PicasaService("PlotYourPictures");

                GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, "PlotYourPictures", parameters);
                picasaService.RequestFactory = requestFactory;

                TempData["tokenType"] = result.Credential.Token.TokenType;
                TempData["accessToken"] = result.Credential.Token.AccessToken;
                return new RedirectResult(referrer);
            }
            else
            {
                return new RedirectResult(result.RedirectUri);
            }
        }
    }
}