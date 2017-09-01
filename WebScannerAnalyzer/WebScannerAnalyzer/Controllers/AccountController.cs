using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using WebScannerAnalyzer.Models;

namespace WebScannerAnalyzer.Controllers
{
    public class AccountController : BaseController
    {
        private IAuthenticationManager AuthenticationManager
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (OrderBusiness.ValidateUser(model.UserName, model.Password))
            {
                SignIn(model.UserName);
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);

        }

        [AllowAnonymous]
        public ActionResult LogOut()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Product");
        }

        private ActionResult RedirectToLocal(string url)
        {
            if (Url.IsLocalUrl(url))
            {
                return Redirect(url);
            }

            return RedirectToAction("Index", "Product");
        }

        private void SignIn(string userName)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, userName));
            var claimIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);

            AuthenticationManager.SignIn(new AuthenticationProperties() {IsPersistent = false}, claimIdentity);
        }
    }
}