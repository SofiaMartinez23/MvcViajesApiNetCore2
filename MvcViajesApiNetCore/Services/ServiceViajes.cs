using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NugetViajesSMG.Models;
using System.Net.Http.Headers;
using System.Text;
using NuGet.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;

namespace MvcViajesApiNetCore.Services
{
    public class ServiceViajes
    {
        private string UrlApi;
        private MediaTypeWithQualityHeaderValue Header;
        private IHttpContextAccessor contextAccessor;

        public ServiceViajes(IConfiguration configuration
            , IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
            this.UrlApi = configuration.GetValue<string>
                ("ApiUrls:ApiViajes");
            this.Header = new
                 MediaTypeWithQualityHeaderValue("application/json");
        }

        public async Task<string> GetTokenAsync
            (string email, string clave)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/auth/login";
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                LoginModel model = new LoginModel
                {
                    Email = email,
                    Clave = clave
                };
                string json = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent
                    (json, Encoding.UTF8, "application/json");
                HttpResponseMessage response =
                    await client.PostAsync(request, content);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content
                        .ReadAsStringAsync();
                    JObject keys = JObject.Parse(data);
                    string token = keys.GetValue("response").ToString();
                    return token;
                }
                else
                {
                    return null;
                }
            }
        }

        private async Task<T> CallApiAsync<T>(string request)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                HttpResponseMessage response =
                    await client.GetAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    T data = await response.Content.ReadAsAsync<T>();
                    return data;
                }
                else
                {
                    return default(T);
                }
            }
        }

        private async Task<T> CallApiAsync<T>
            (string request, string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Add
                    ("Authorization", "bearer " + token);
                HttpResponseMessage response =
                    await client.GetAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    T data = await response.Content.ReadAsAsync<T>();
                    return data;
                }
                else
                {
                    return default(T);
                }
            }
        }

        #region ZONA USUARIOS

        public async Task<List<UsuarioCompletoView>> GetUsuariossAsync()
        {

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }


            string request = "api/usuarios";
            List<UsuarioCompletoView> usuarios = await
                this.CallApiAsync<List<UsuarioCompletoView>>(request);

            if (idUsuario != -1 && usuarios != null)
            {
                usuarios = usuarios.Where(u => u.IdUsuario != idUsuario).ToList();
            }

            return usuarios;
        }

        public async Task<List<UsuarioCompletoView>> BuscarUsuariosPorNombreAsync(string searchInput)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            string request = $"api/Usuarios/BuscarByNombre?nombre={searchInput}";
            List<UsuarioCompletoView> usuarios = await
                this.CallApiAsync<List<UsuarioCompletoView>>(request, token);
            return usuarios;
        }


        public async Task<UsuarioCompletoView> FindUsuarioAsync
            (int idUsuario)
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN").Value;
            string request = "api/usuarios/" + idUsuario;
            UsuarioCompletoView empleado = await
                this.CallApiAsync<UsuarioCompletoView>(request, token);
            return empleado;
        }

        public async Task<UsuarioCompletoView> GetPerfilAsync()
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(x => x.Type == "TOKEN").Value;
            string request = "api/usuarios/perfil";
            UsuarioCompletoView empleado = await
                this.CallApiAsync<UsuarioCompletoView>(request, token);
            return empleado;
        }

        public async Task<int> GetNextUsuarioIdAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/usuarios";

                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<UsuarioCompletoView> usuarios = JsonConvert.DeserializeObject<List<UsuarioCompletoView>>(json);
                    if (usuarios != null && usuarios.Any())
                    {
                        return usuarios.Max(u => u.IdUsuario) + 1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    throw new Exception("Error al obtener la lista de usuarios para generar el próximo ID: " + response.StatusCode);
                }
            }
        }


        public async Task InsertUsuarioAsync(
           string nombre, string email, int edad, string nacionalidad,
           string preferenciaViaje, string clave, string confirmarClave,
           string avatarUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/usuarios/insertusuario";

                UsuarioCompletoView usuario = new UsuarioCompletoView();
                usuario.IdUsuario = await GetNextUsuarioIdAsync();
                usuario.Nombre = nombre;
                usuario.Email = email;
                usuario.Edad = edad;
                usuario.Nacionalidad = nacionalidad;
                usuario.PreferenciaViaje = preferenciaViaje;
                usuario.Clave = clave;
                usuario.ConfirmarClave = confirmarClave;
                usuario.AvatarUrl = avatarUrl;
                usuario.FechaRefistro = DateTime.Now;

                string json = JsonConvert.SerializeObject(usuario);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al registrar usuario: " + response.StatusCode);
                }
            }
        }




        public async Task UpdateUsuarioAsync(
        string nombre, string email, int edad, string nacionalidad,
        string preferenciaViaje, string clave, string confirmarClave,
        string avatarUrl)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/usuarios/updateusuario/" + idUsuario;
                UsuarioCompletoView usuario = new UsuarioCompletoView();
                usuario.IdUsuario = idUsuario;
                usuario.Nombre = nombre;
                usuario.Email = email;
                usuario.Edad = edad;
                usuario.Nacionalidad = nacionalidad;
                usuario.PreferenciaViaje = preferenciaViaje;
                usuario.Clave = clave;
                usuario.ConfirmarClave = confirmarClave;
                usuario.AvatarUrl = avatarUrl;

                string json = JsonConvert.SerializeObject(usuario);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al updatear usuario: " + response.StatusCode);
                }
            }
        }

        #endregion

        #region ZONA LUGARES
        public async Task<List<Lugar>> GetLugaresAsync()
        {
            string request = "api/lugares";
            List<Lugar> lugar = await
                this.CallApiAsync<List<Lugar>>(request);
            return lugar;
        }

        public async Task<List<Lugar>> GetLugaresPorUsuarioAsync(int idUsuario)
        {
            string token = this.contextAccessor.HttpContext.User
               .FindFirst(z => z.Type == "TOKEN").Value;
            string request = "api/Lugares/lugaresusuario/" + idUsuario;
            List<Lugar> lugar = await
                this.CallApiAsync<List<Lugar>>(request, token);
            return lugar;
        }

        public async Task<Lugar> FindLugarAsync
           (int idLugar)
        {
            string request = "api/lugares/" + idLugar;
            Lugar lugar = await
                this.CallApiAsync<Lugar>(request);
            return lugar;
        }

        public async Task InsertLugarAsync(
         string nombre, string descripcion, string ubicacion,
         string categoria, DateTime horario, string imagen,
         string tipo)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    // If not found in claims, fallback to getting from the profile
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                List<Lugar> allLugares = await GetLugaresAsync();
                int newIdLugar;
                if (allLugares != null && allLugares.Any())
                {
                    newIdLugar = allLugares.Max(l => l.IdLugar) + 1;
                }
                else
                {
                    newIdLugar = 1;
                }

                string request = "api/lugares";
                Lugar lugar = new Lugar();
                lugar.IdLugar = newIdLugar;
                lugar.Nombre = nombre;
                lugar.Descripcion = descripcion;
                lugar.Ubicacion = ubicacion;
                lugar.Categoria = categoria;
                lugar.Horario = horario;
                lugar.Imagen = imagen;
                lugar.Tipo = tipo;
                lugar.IdUsuario = idUsuario;

                string json = JsonConvert.SerializeObject(lugar);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al insertar lugar: " + response.StatusCode);
                }
            }
        }


        public async Task UpdateLugarAsync(
        int idLugar, string nombre, string descripcion, string ubicacion,
        string categoria, DateTime horario, string imagen,
        string tipo)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    // If not found in claims, fallback to getting from the profile
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                string request = "api/lugares/updatelugar/" + idLugar;
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                Lugar lugar = new Lugar
                {
                    IdLugar = idLugar,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Ubicacion = ubicacion,
                    Categoria = categoria,
                    Horario = horario,
                    Imagen = imagen,
                    Tipo = tipo,
                    IdUsuario = idUsuario
                };

                string json = JsonConvert.SerializeObject(lugar);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar lugar: {response.StatusCode} - {errorContent}"); // Esto imprimirá el contenido del error en la consola
                    throw new Exception("Error al actualizar lugar: " + response.StatusCode);
                }
            }
        }

        public async Task DeleteLugarAsync(int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/lugares/" + idLugar;
                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al eliminar lugar: {response.StatusCode}");
                }
            }
        }

        #endregion

        #region ZONA COMENTARIOS

        public async Task<List<Comentario>> GetComentarioLugarAsync(int idlugar)
        {
            string request = "api/comentarios/" + idlugar;
            List<Comentario> coment = await
                this.CallApiAsync<List<Comentario>>(request);
            return coment;
        }

        public async Task<Comentario> FindComentarioAsync
          (int idComentario)
        {
            string request = "api/comentarios/findcomentarios/" + idComentario;
            Comentario coment = await
                this.CallApiAsync<Comentario>(request);
            return coment;
        }

        public async Task<int> GetNextComentarioIdAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/comentarios";


                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Comentario> comentarios = JsonConvert.DeserializeObject<List<Comentario>>(json);
                    if (comentarios != null && comentarios.Any())
                    {
                        return comentarios.Max(c => c.IdComentario) + 1;
                    }
                    else
                    {
                        return 1;
                        return 1; 
                    }
                }
                else
                {
                    throw new Exception("Error al obtener la lista de comentarios para generar el próximo ID: " + response.StatusCode);
                }
            }
        }

        public async Task InsertComentarioAsync(int idLugar, string comentario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;
            string nombreUsuario = string.Empty;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                        nombreUsuario = perfil.Nombre;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/comentarios";

                Comentario coment = new Comentario
                {
                    IdComentario = await GetNextComentarioIdAsync(), 
                    IdLugar = idLugar,
                    IdUsuario = idUsuario,
                    Comentarios = comentario,
                    FechaComentario = DateTime.Now,
                    NombreUsuario = nombreUsuario
                };

                string json = JsonConvert.SerializeObject(coment);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al updatear usuario: " + response.StatusCode);
                }
            }
        }

        public async Task UpdateComentarioAsync(int idComentario, int idLugar, string comentario, string nombreUsuario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    // If not found in claims, fallback to getting from the profile
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                        nombreUsuario = perfil.Nombre;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/comentarios/updatecomentarios/" + idComentario;
                Comentario coment = new Comentario
                {
                    IdComentario = idComentario,
                    IdLugar = idLugar,
                    IdUsuario = idUsuario,
                    Comentarios = comentario,
                    FechaComentario = DateTime.Now,
                    NombreUsuario = nombreUsuario
                };

                string json = JsonConvert.SerializeObject(coment);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar el comentario: {response.StatusCode} - {errorContent}");
                }
            }
        }

        public async Task DeleteComentarioAsync(int idComentario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = $"api/comentarios/{idComentario}"; // Endpoint para eliminar, incluyendo el ID

                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el comentario: {response.StatusCode} - {errorContent}");
                }
            }
        }
        #endregion

        #region ZONA FAVORITOS

        public async Task<List<LugarFavorito>> GetFavoritosAsync()
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Token de usuario no encontrado para obtener todos los favoritos.");
            }

            string request = "api/favoritos";
            List<LugarFavorito> favoritos = await
                this.CallApiAsync<List<LugarFavorito>>(request, token); 
            return favoritos;
        }

        public async Task<List<LugarFavorito>> GetFavoritosUsuarioAsync(int idusuario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            string request = "api/favoritos/" + idusuario;

            List<LugarFavorito> favoritos = await
                this.CallApiAsync<List<LugarFavorito>>(request, token);

            return favoritos;
        }

        public async Task<List<int>> GetIdsLugaresFavoritosPorUsuarioAsync()
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Obtener el perfil del usuario autenticado
                HttpResponseMessage response = await client.GetAsync("api/usuarios/perfil");
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error al obtener perfil del usuario: " + error);
                }

                string json = await response.Content.ReadAsStringAsync();
                UsuarioCompletoView usuario = JsonConvert.DeserializeObject<UsuarioCompletoView>(json);

                int idUsuario = usuario.IdUsuario;
                string request = "api/favoritos/IdsFavoritos/" + idUsuario;

                List<int> favoritos = await this.CallApiAsync<List<int>>(request, token);
                return favoritos;
            }
        }

        public async Task<int> GetNextFavoritoIdAsync()
        {
            List<LugarFavorito> allFavoritos = await GetFavoritosAsync();

            if (allFavoritos != null && allFavoritos.Any())
            {
                return allFavoritos.Max(f => f.IdFavorito) + 1;
            }
            else
            {
                return 1;
            }
        }

        public async Task InsertFavoritoAsync(int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
               .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            Lugar lugarDetails = await FindLugarAsync(idLugar); 
            if (lugarDetails == null)
            {
                throw new Exception($"No se encontró el lugar con IdLugar {idLugar} para marcar como favorito.");
            }

            int newIdFavorito = await GetNextFavoritoIdAsync();

            LugarFavorito favorito = new LugarFavorito
            {
                IdFavorito = newIdFavorito,
                IdUsuario = idUsuario,
                IdLugar = idLugar,
                ImagenLugar = lugarDetails.Imagen,
                NombreLugar = lugarDetails.Nombre,
                DescripcionLugar = lugarDetails.Descripcion,
                UbicacionLugar = lugarDetails.Ubicacion,
                TipoLugar = lugarDetails.Tipo,
                FechaDeVisitaLugar = DateTime.Now,
            };

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/favoritos"; 
                string json = JsonConvert.SerializeObject(favorito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al insertar favorito: {response.StatusCode} - {errorContent}");
                }

            }
        }
        public async Task DeleteFavoritosAsync(int idUsuario, int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/favoritos/" + idLugar;
                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al eliminar lugar: {response.StatusCode}");
                }
            }
        }


        #endregion

        #region ZONA SEGUIDORES
        public async Task<List<UsuarioSeguidoPerfil>> GetSeguidoresUsuarioAsync(int idusuario)
        {

            string token = this.contextAccessor.HttpContext.User
                 .FindFirst(z => z.Type == "TOKEN")?.Value;

            string request = "api/Seguidores/Seguidores/" + idusuario;

            List<UsuarioSeguidoPerfil> seguidors = await
                this.CallApiAsync<List<UsuarioSeguidoPerfil>>(request, token);

            return seguidors;

        }

        public async Task<List<int>> GetIdsSeguidosPorUsuarioAsync()
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Obtener el perfil del usuario autenticado
                HttpResponseMessage response = await client.GetAsync("api/usuarios/perfil");
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error al obtener perfil del usuario: " + error);
                }

                string json = await response.Content.ReadAsStringAsync();
                UsuarioCompletoView usuario = JsonConvert.DeserializeObject<UsuarioCompletoView>(json);

                int idUsuario = usuario.IdUsuario;
                string request = "api/Seguidores/IdsSeguidos/" + idUsuario;

                List<int> seguidors = await
               this.CallApiAsync<List<int>>(request, token);

                return seguidors;
            }
        }

        public async Task<bool> ExisteFavoritoAsync(int idUsuario, int idLugar)
        {
            var usuario = await GetFavoritosUsuarioAsync(idUsuario);
            var lugares = usuario.FirstOrDefault(s => s.IdUsuario == idUsuario && s.IdLugar == idLugar);
            return lugares != null;
        }


        public async Task<int> GetNextSeguidorIdAsync(int idUsuarioSeguidor)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                string request = "api/Seguidores/Seguidores/" + idUsuarioSeguidor;

                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Seguidor> seguidores = JsonConvert.DeserializeObject<List<Seguidor>>(json);

                    if (seguidores != null && seguidores.Any())
                    {
                        return seguidores.Max(s => s.IdSeguidor) + 1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    throw new Exception("Error al obtener la lista de seguidores para generar el próximo ID: " + response.StatusCode);
                }
            }
        }

        public async Task<bool> ExisteRelacionSeguidorAsync(int idSeguidor, int idSeguido)
        {
            var seguidores = await GetSeguidoresUsuarioAsync(idSeguidor);
            var seguidor = seguidores.FirstOrDefault(s => s.IdUsuarioSeguidor == idSeguidor && s.IdUsuarioSeguido == idSeguido);
            return seguidor != null;
        }


        public async Task InsertSeguidorAsync(int idSeguidor, int idSeguido)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Paso 1: Obtener el siguiente ID disponible para el seguidor
                int nextId = await GetNextSeguidorIdAsync(idSeguidor);

                // Paso 2: Obtener el perfil del usuario seguidor
                HttpResponseMessage responseSeguidor = await client.GetAsync($"api/usuarios/{idSeguidor}");
                if (!responseSeguidor.IsSuccessStatusCode)
                {
                    string errorResponse = await responseSeguidor.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener perfil del seguidor: {errorResponse}");
                }

                string jsonSeguidor = await responseSeguidor.Content.ReadAsStringAsync();
                UsuarioCompletoView usuarioSeguidor = JsonConvert.DeserializeObject<UsuarioCompletoView>(jsonSeguidor);

                // Paso 3: Obtener el perfil del usuario seguido
                HttpResponseMessage responseSeguido = await client.GetAsync($"api/usuarios/{idSeguido}");
                if (!responseSeguido.IsSuccessStatusCode)
                {
                    string errorResponse = await responseSeguido.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener perfil del seguido: {errorResponse}");
                }

                string jsonSeguido = await responseSeguido.Content.ReadAsStringAsync();
                UsuarioCompletoView usuarioSeguido = JsonConvert.DeserializeObject<UsuarioCompletoView>(jsonSeguido);

                // Paso 4: Construir el objeto Seguidor
                Seguidor seguir = new Seguidor
                {
                    IdSeguidor = nextId,
                    IdUsuarioSeguidor = idSeguidor,
                    IdUsuarioSeguido = idSeguido,
                    FechaSeguimiento = DateTime.UtcNow,
                    UsuarioSeguidor = usuarioSeguidor,
                    UsuarioSeguido = usuarioSeguido
                };

                // Paso 5: Insertar el seguidor a través de la API
                string request = "api/Seguidores/InsertSeguidor";
                string json = JsonConvert.SerializeObject(seguir);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error al seguir al usuario: " + error);
                }
            }
        }

        public async Task DeleteSeguidorAsync(int idSeguido)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/seguidores/" + idSeguido;
                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al eliminar seguidor: {response.StatusCode}");
                }
            }
        }


        #endregion

        #region ZONA CHAT

        public async Task<List<Chat>> GetChatUsuarioAsync(int idusuario, int userDestinatario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            string request = $"api/Chats/Conversacion?userRemitente={idusuario}&userDestinatario={userDestinatario}";

            List<Chat> mensajes = await this.CallApiAsync<List<Chat>>(request, token);

            return mensajes;
        }

        public async Task<int> GetNextMensajeIdAsync(int idusuario, int idUsuarioDestinatario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Solicitar los mensajes de la conversación entre los dos usuarios
                string request = $"api/Chats/Conversacion?userRemitente={idusuario}&userDestinatario={idUsuarioDestinatario}";

                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Chat> mensajes = JsonConvert.DeserializeObject<List<Chat>>(json);

                    // Si existen mensajes, obtener el mayor IdMensaje y sumarle 1
                    if (mensajes != null && mensajes.Any())
                    {
                        return mensajes.Max(m => m.IdMensaje) + 1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    throw new Exception("Error al obtener los mensajes de la conversación para generar el próximo ID: " + response.StatusCode);
                }
            }
        }


        public async Task InsertMensajeChat(int idusuario, int userDestinatario, string mensaje)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Paso 1: Obtener el siguiente ID disponible para el seguidor
                int nextId = await GetNextSeguidorIdAsync(idusuario);

                // Paso 2: Obtener el perfil del usuario seguidor
                HttpResponseMessage responseSeguidor = await client.GetAsync($"api/usuarios/{idusuario}");
                if (!responseSeguidor.IsSuccessStatusCode)
                {
                    string errorResponse = await responseSeguidor.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener perfil del seguidor: {errorResponse}");
                }

                string jsonSeguidor = await responseSeguidor.Content.ReadAsStringAsync();
                UsuarioCompletoView usuarioRemitente = JsonConvert.DeserializeObject<UsuarioCompletoView>(jsonSeguidor);

                // Paso 3: Obtener el perfil del usuario seguido
                HttpResponseMessage responseSeguido = await client.GetAsync($"api/usuarios/{userDestinatario}");
                if (!responseSeguido.IsSuccessStatusCode)
                {
                    string errorResponse = await responseSeguido.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener perfil del seguido: {errorResponse}");
                }

                string jsonSeguido = await responseSeguido.Content.ReadAsStringAsync();
                UsuarioCompletoView usuarioDestinatario = JsonConvert.DeserializeObject<UsuarioCompletoView>(jsonSeguido);


                int nuevoIdMensaje = await GetNextMensajeIdAsync(idusuario, userDestinatario);


                if (!string.IsNullOrEmpty(mensaje))
                {
                    var nuevoMensaje = new Chat
                    {
                        IdMensaje = nuevoIdMensaje,
                        IdUsuarioRemitente = idusuario,
                        IdUsuarioDestinatario = userDestinatario,
                        Mensaje = mensaje,
                        FechaEnvio = DateTime.UtcNow,
                        UsuarioRemitente = usuarioRemitente,
                        UsuarioDestinatario = usuarioDestinatario
                    };

                    string request = "api/Chats/Enviar";
                    string json = JsonConvert.SerializeObject(nuevoMensaje);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage responseMensaje = await client.PostAsync(request, content);

                    if (!responseMensaje.IsSuccessStatusCode)
                    {
                        string error = await responseMensaje.Content.ReadAsStringAsync();
                        throw new Exception("Error al enviar el mensaje: " + error);
                    }
                }
            }
        }
        #endregion

    }
}

