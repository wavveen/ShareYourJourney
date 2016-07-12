using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ShareYourJourney.Startup))]
namespace ShareYourJourney
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
