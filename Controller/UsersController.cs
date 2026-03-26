using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using RestApiKazakov.Context;
using RestApiKazakov.Models;
using BCrypt.Net;

namespace RestApiKazakov.Controller
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("api/usersController")]
    [ApiExplorerSettings(GroupName = "v2")]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="Email">Логин пользователя</param>
        /// <param name="Login">Логин пользователя</param>
        /// <param name="Password">Пароль пользователя</param>
        /// <returns>Данный метод преднозначен для авторизации пользователя на сайте</returns>
        /// /// <response code="200">Пользователь успешно авторизирован</response>
        /// /// <response code="403">Ошибка запроса данные не указаны</response>
        /// /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("SignIn")]
        [HttpPost]
        [ProducesResponseType(typeof(Users), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]

        public ActionResult SignIn([FromForm] string Email, [FromForm] string Password)
        {
            if (Email == null || Password == null)
                return StatusCode(403);

            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.FirstOrDefault(x => x.Email == Email);

                    if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
                        return StatusCode(401, "Неверная почта или пароль");

                    var token = BCrypt.Net.BCrypt.HashPassword(user.Id.ToString());
                    return Ok(new { user.Id, Token = token });
                }
            }
            catch (Exception exp)
            {
                return StatusCode(500);
            }
        }

        [HttpPost("RegIn")]
        public ActionResult RegIn([FromForm] string Email, [FromForm] string Login, [FromForm] string Password)
        {
            if (Email == null || Login == null || Password == null)
                return StatusCode(403);

            try
            {
                using (var db = new AppDbContext())
                {
                    if (db.Users.Any(u => u.Username == Login || u.Email == Email))
                    {
                        return BadRequest("Пользователь с таким логином или почтой уже существует");
                    }

                    var newUser = new Users
                    {
                        Email = Email,
                        Username = Login,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    };
                    db.Add(newUser);
                    db.SaveChanges();

                    return Ok(newUser);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}\nInner: {ex.InnerException?.Message}");
            }
        }
    }
}
