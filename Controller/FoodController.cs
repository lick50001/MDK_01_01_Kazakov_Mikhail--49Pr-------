using Microsoft.AspNetCore.Mvc;
using RestApiKazakov.Context;
using RestApiKazakov.Models;

namespace RestApiKazakov.Controller
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        /// <summary>
        /// Получение списков версий меню (Завтрак, Обед, Ужин)
        /// </summary>
        /// <returns>Список доступных меню с датами</returns>
        /// <response code="200">Меню успешно получено</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("versions")]
        [ProducesResponseType(typeof(IEnumerable<Menus>), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetMenuVersions()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var menus = db.Menus
                        .Where(m => m.IsActive)
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
        /// <param name="menuId">ID версии меню</param>
        /// <returns>Список блюд с ценами</returns>
        /// <response code="200">Блюда успешно получены</response>
        /// <response code="404">Меню не найдено</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("dishes")]
        [ProducesResponseType(typeof(IEnumerable<Dishes>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetDishes([FromQuery] int menuId)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var menuExists = db.Menus.Any(m => m.Id == menuId && m.IsActive);
                    if (!menuExists)
                        return NotFound($"Меню с ID {menuId} не найдено");

                    var dishes = db.Dishes
                        .Where(d => d.MenuId == menuId)
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
