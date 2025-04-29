using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;
using MvcViajesApiNetCore.Services;
using NugetViajesSMG.Models;
using System.Security.Claims;

namespace MvcViajesApiNetCore.Controllers
{
    public class LugaresController : Controller
    {
        private ServiceViajes service;

        public LugaresController(ServiceViajes service)
        {
            this.service = service;
        }

        public async Task<IActionResult> Index()
        {
            List<Lugar> lugar = await this.service.GetLugaresAsync();

            string token = this.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                List<int> idsFavoritos = await this.service.GetIdsLugaresFavoritosPorUsuarioAsync();
                ViewBag.IdsFavoritos = idsFavoritos;
            }

            return View(lugar);
        }


        public async Task<IActionResult> Details(int idLugar)
        {
            Lugar lugar = await
                    this.service.FindLugarAsync(idLugar);
            return View(lugar);
        }

        public async Task<IActionResult> _Comentarios(int idLugar)
        {
            List<Comentario> comentarios = await this.service.GetComentarioLugarAsync(idLugar);

            if (User.Identity.IsAuthenticated)
            {
                UsuarioCompletoView perfil = await this.service.GetPerfilAsync();

                if (perfil != null)
                {
                    ViewData["IdUsuarioActual"] = perfil.IdUsuario;
                }
            }
            else
            {
                ViewData["IdUsuarioActual"] = null;
            }

            return PartialView("_Comentarios", comentarios);
        }

        public IActionResult Create()
        {
            return View();
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Create(Lugar lugar)
        {
            await this.service.InsertLugarAsync(
                lugar.Nombre, lugar.Descripcion, lugar.Ubicacion,
                lugar.Categoria, lugar.Horario, lugar.Imagen,
                lugar.Tipo);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int idLugar)
        {
            Lugar lugar = await this.service.FindLugarAsync(idLugar);
            return View(lugar);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Edit(Lugar lugar)
        {
            await this.service.UpdateLugarAsync(
                lugar.IdLugar, lugar.Nombre, lugar.Descripcion, lugar.Ubicacion,
                lugar.Categoria, lugar.Horario, lugar.Imagen,
                lugar.Tipo);

            return RedirectToAction("Perfil", "Usuarios");
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> CreateComment(int idlugar, string comentario)
        {
            try
            {
                await this.service.InsertComentarioAsync(idlugar, comentario);
                return RedirectToAction("Details", new { idLugar = idlugar });
            }
            catch (UnauthorizedAccessException ex)
            {
                return RedirectToAction("Login", "Managed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al publicar el comentario: " + ex.Message;
                return RedirectToAction("Details", new { idLugar = idlugar });
            }
        }


        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> EditComment(int idlugar, Comentario comentario)
        {
            await this.service.UpdateComentarioAsync(
                comentario.IdComentario,
                comentario.IdLugar,
                comentario.Comentarios,
                comentario.NombreUsuario
            );

            return RedirectToAction("Details", new { idLugar = idlugar });
        }



        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteComment(int idlugar, int idcomentario)
        {
            await this.service.DeleteComentarioAsync(idcomentario);
            return RedirectToAction("Details", new { idLugar = idlugar });
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> AddToFavoritos(int idLugar)
        {


            UsuarioCompletoView usuario = await this.service.GetPerfilAsync();
            int idUsuario = usuario.IdUsuario;

            bool yaEsFavorito = await this.service.ExisteFavoritoAsync(idUsuario, idLugar);

            if (yaEsFavorito)
            {
                TempData["YaEsFavorito"] = true;
                TempData["LugarFavoritoId"] = idLugar;
            }
            else
            {
                await this.service.InsertFavoritoAsync(idLugar);
                TempData["AgregadoAFavoritos"] = true;
                TempData["LugarFavoritoId"] = idLugar;
            }

            return RedirectToAction("Index");

        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteFavorito(int idUsuario, int idLugar)
        {
            await this.service.DeleteFavoritosAsync(idUsuario, idLugar);
            return RedirectToAction("Index");
            try
            {
                await this.service.InsertFavoritoAsync(idLugar);
                return RedirectToAction("Details", new { idLugar = idLugar });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Managed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al agregar a favoritos: " + ex.Message;
                return RedirectToAction("Details", new { idLugar = idLugar });
            }
        }

    }

}

