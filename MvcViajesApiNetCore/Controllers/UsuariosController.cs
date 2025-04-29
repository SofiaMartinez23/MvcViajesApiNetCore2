using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;
using MvcViajesApiNetCore.Services;
using NugetViajesSMG.Models;

namespace MvcViajesApiNetCore.Controllers
{
    public class UsuariosController : Controller
    {
        private ServiceViajes service;
        

        public UsuariosController(ServiceViajes service)
        {
            this.service = service;
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Index()
        {
            UsuarioCompletoView usuarioActual = await this.service.GetPerfilAsync();
            int idActual = usuarioActual.IdUsuario;

            List<UsuarioCompletoView> usuarios = await this.service.GetUsuariossAsync();
            
            List<int> idsSeguidos = await this.service.GetIdsSeguidosPorUsuarioAsync();
            ViewBag.IdsSeguidos = idsSeguidos;

            return View(usuarios);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Details(int idUsuario)
        {
            UsuarioCompletoView usuario = await
                    this.service.FindUsuarioAsync(idUsuario);

            return View(usuario);
        }

        public async Task<IActionResult> Edit(int idUsuario)
        {
            // Obtener la información actual del usuario desde el servicio.
            UsuarioCompletoView usuario = await this.service.FindUsuarioAsync(idUsuario);
            return View(usuario);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioCompletoView usuario)
        {
            if (ModelState.IsValid)
            {
                // Actualizar los datos del usuario en la base de datos.
                await this.service.UpdateUsuarioAsync(
                    usuario.Nombre, usuario.Email, usuario.Edad,
                    usuario.Nacionalidad, usuario.PreferenciaViaje, usuario.Clave,
                    usuario.ConfirmarClave, usuario.AvatarUrl);

                // Redirigir al perfil del usuario con los datos actualizados.
                return RedirectToAction("Perfil", new { idUsuario = usuario.IdUsuario });
            }

            return View(usuario);
        }


        [AuthorizeUsuarios]
        public async Task<IActionResult> Perfil()
        {
            UsuarioCompletoView usuario = await
                this.service.GetPerfilAsync();

            var lugares = await this.service.GetLugaresPorUsuarioAsync(usuario.IdUsuario);
            ViewBag.LugaresCreadosCount = lugares?.Count ?? 0;

            var lugaresFavoritos = await this.service.GetFavoritosUsuarioAsync(usuario.IdUsuario);
            ViewBag.LugaresFavoritosCount = lugaresFavoritos?.Count ?? 0;

            return View(usuario);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _Lugares(int idUsuario)
        {
            List<Lugar> lugares = await
                    this.service.GetLugaresPorUsuarioAsync(idUsuario);
            return PartialView("_Lugares", lugares);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _LugaresUsuario(int idUsuario)
        {
            List<Lugar> lugares = await
                    this.service.GetLugaresPorUsuarioAsync(idUsuario);
            return PartialView("_LugaresUsuario", lugares);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _Favoritos(int idUsuario)
        {
            List<LugarFavorito> fav = await
                    this.service.GetFavoritosUsuarioAsync(idUsuario);
            return PartialView("_Favoritos", fav);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _Seguidos(int idusuario)
        {
            List<UsuarioSeguidoPerfil> seguidors = await this.service.GetSeguidoresUsuarioAsync(idusuario);
            return PartialView("_Seguidos", seguidors);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Seguir(int seguidoId)
        {
            UsuarioCompletoView usuarioActual = await this.service.GetPerfilAsync();
            int idSeguidor = usuarioActual.IdUsuario;

            bool yaExiste = await this.service.ExisteRelacionSeguidorAsync(idSeguidor, seguidoId);

            if (yaExiste)
            {
                TempData["YaSigue"] = true;
                TempData["NombreSeguido"] = seguidoId.ToString();
            }
            else if (idSeguidor != seguidoId)
            {
                await this.service.InsertSeguidorAsync(idSeguidor, seguidoId);
                TempData["Seguido"] = true;
            }

            return RedirectToAction("Index");
        }



        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteLugar(int idLugar)
        {
            await this.service.DeleteLugarAsync(idLugar);
            return RedirectToAction("Perfil");
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteFavorito(int idUsuario, int idLugar)
        {
            await this.service.DeleteFavoritosAsync(idUsuario, idLugar);
            return RedirectToAction("_Favoritos");
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteSeguidor(int seguidoId)
        {
            await this.service.DeleteSeguidorAsync(seguidoId);
            return RedirectToAction("Index");
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> ChatUser(int idusuario, int userDestinatario)
        {
            List<Chat> mensajes = await this.service.GetChatUsuarioAsync(idusuario, userDestinatario);

            return View(mensajes);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> InsertMensajeChat(Chat chat)
        {
           
            await this.service.InsertMensajeChat(chat.IdUsuarioRemitente, 
                chat.IdUsuarioDestinatario, chat.Mensaje);
            return RedirectToAction("Index");

               
        }
    }
}
