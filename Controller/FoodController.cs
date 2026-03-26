using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using RestApiKazakov.Context;
using RestApiKazakov.Models;

namespace RestApiKazakov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class FoodController : ControllerBase
    {
        /// <summary>
        /// Получение списков версий меню (Завтрак, Обед, Ужин)
        /// </summary>
        /// <returns>Список доступных меню</returns>
        /// <response code="200">Меню успешно получено</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("versions")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetMenuVersions()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var menus = db.Menus
                        .Where(m => m.IsActive)
                        .OrderBy(m => m.Name)
                        .Select(m => new
                        {
                            m.Id,
                            m.Name
                        })
                        .ToList();

                    return Ok(menus);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение списка блюд для указанной версии меню
        /// </summary>
        /// <param name="Version">Название меню: Завтрак, Обед или Ужин</param>
        /// <returns>Список блюд с ценами</returns>
        /// <response code="200">Блюда успешно получены</response>
        /// <response code="404">Меню не найдено</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("dishes")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetDishes([FromQuery] string Version)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var menuIds = db.Menus
                        .Where(m => m.Name == Version && m.IsActive)
                        .Select(m => m.Id)
                        .ToList();

                    if (menuIds.Count == 0)
                        return NotFound($"Меню '{Version}' не найдено. Доступные: Завтрак, Обед, Ужин");

                    var dishes = db.Dishes
                        .Where(d => menuIds.Contains(d.MenuId))
                        .Select(d => new
                        {
                            d.Id,
                            d.Name,
                            d.Price,
                            d.Description
                        })
                        .ToList();

                    return Ok(dishes);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }
    }
}