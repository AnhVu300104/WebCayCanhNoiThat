using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
[assembly: OwinStartup(typeof(WebsiteNoiThat.App_Start.Startup))]
namespace WebsiteNoiThat.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 1. Cookie authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/RegisterAndLogin/Login")
            });

            // 2. Google authentication
            app.UseExternalSignInCookie("ExternalCookie");

            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
            {
                ClientId = System.Configuration.ConfigurationManager.AppSettings["GoogleClientId"],
                ClientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleClientSecret"],
                SignInAsAuthenticationType = "ExternalCookie"
            });
            
            app.UseFacebookAuthentication(new Microsoft.Owin.Security.Facebook.FacebookAuthenticationOptions
            {
                AppId = System.Configuration.ConfigurationManager.AppSettings["FacebookAppId"],
                AppSecret = System.Configuration.ConfigurationManager.AppSettings["FacebookAppSecret"],
                SignInAsAuthenticationType = "ExternalCookie"
            });

        }
    }
}