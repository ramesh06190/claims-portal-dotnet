using System;
using System.Web;

namespace ClaimsPortal.WebForms
{
    /// <summary>
    /// Per-user token for Page.ViewStateUserKey. Tying view state to a token only this browser has
    /// means a postback forged from another site fails MAC validation (CSRF protection for Web Forms).
    /// </summary>
    public static class AntiForgery
    {
        private const string CookieName = "__AntiXsrfToken";

        public static string GetOrCreateToken(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var existing = context.Request.Cookies[CookieName]?.Value;
            if (!string.IsNullOrEmpty(existing) && Guid.TryParseExact(existing, "N", out _))
            {
                return existing!;
            }

            var token = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Set(new HttpCookie(CookieName, token)
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
            });
            return token;
        }
    }
}
