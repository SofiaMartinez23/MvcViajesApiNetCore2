using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NugetViajesSMG.Models;
using MvcViajesApiNetCore.Services;
using System.Security.Claims;

namespace MvcOAuthEmpleados.Controllers
{
    public class ManagedController : Controller
    {
        private ServiceViajes service;

        public ManagedController(ServiceViajes service)
        {
            this.service = service;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            string token = await this.service
                .GetTokenAsync(model.Email, model.Clave);
            if (token == null)
            {
                ViewData["MENSAJE"] = "Usuario/Password incorrectos";
                return View();
            }
            else
            {
                ViewData["MENSAJE"] = "Ya tienes tu Token!!!";
                HttpContext.Session.SetString("TOKEN", token);
                ClaimsIdentity identity =
                    new ClaimsIdentity
                    (CookieAuthenticationDefaults.AuthenticationScheme
                    , ClaimTypes.Name, ClaimTypes.Role);
                identity.AddClaim(new Claim
                    (ClaimTypes.Name, model.Email));
                identity.AddClaim(new Claim
                    (ClaimTypes.NameIdentifier, model.Clave));
                identity.AddClaim(new Claim
                    ("TOKEN", token));
                ClaimsPrincipal principal =
                    new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync
                    (CookieAuthenticationDefaults.AuthenticationScheme
                    , principal, new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                    });
                return RedirectToAction("Perfil", "Usuarios");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioCompletoView model)
        {

            await this.service.InsertUsuarioAsync(
                model.Nombre,
                model.Email,
                model.Edad,
                model.Nacionalidad,
                model.PreferenciaViaje,
                model.Clave,
                model.ConfirmarClave,
                model.AvatarUrl
            );
               
            return RedirectToAction("Login", "Managed");

        }



        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync
                (CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Managed");
        }
    }
}
