using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using RestApiKazakov.Context;
using RestApiKazakov.Models;
using BCrypt.Net;

namespace RestApiKazakov.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v2")]
    public class OrdersController : ControllerBase
    {
        /// <summary>
        /// Отправка заказа (требуется токен пользователя)
        /// </summary>
        /// <param name="Token">Токен пользователя (хэшированный Id)</param>
        /// <param name="MenuId">ID версии меню</param>
        /// <param name="Items">Список блюд в формате "DishId:Quantity" (например: "1:2,3:1")</param>
        /// <returns>Подтверждение создания заказа</returns>
        /// <response code="200">Заказ успешно создан</response>
        /// <response code="401">Неверный или отсутствующий токен</response>
        /// <response code="400">Ошибка в данных заказа</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CreateOrderAsync(
        [FromForm] string Token,
        [FromForm] int MenuId,
        [FromForm] string Items)
        {
            if (string.IsNullOrEmpty(Token))
                return StatusCode(401, "Токен не указан");

            int userId;
            try
            {
                using (var db = new AppDbContext())
                {
                    var allUsers = db.Users.ToList();
                    var matchedUser = allUsers.FirstOrDefault(u =>
                        BCrypt.Net.BCrypt.Verify(u.Id.ToString(), Token));

                    if (matchedUser == null)
                        return StatusCode(401, "Неверный токен");

                    userId = matchedUser.Id;
                }
            }
            catch
            {
                return StatusCode(401, "Ошибка проверки токена");
            }

            if (string.IsNullOrEmpty(Items))
                return BadRequest("Список блюд не указан. Формат: \"1:2,3:1\"");

            var orderItems = new List<(int DishId, int Quantity)>();

            try
            {
                var rawItems = Items.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in rawItems)
                {
                    var parts = item.Split(':');
                    if (parts.Length != 2)
                        return BadRequest($"Неверный формат: \"{item}\". Ожидается \"DishId:Quantity\"");

                    if (!int.TryParse(parts[0], out int dishId) || dishId <= 0)
                        return BadRequest($"Неверный ID блюда: \"{parts[0]}\"");

                    if (!int.TryParse(parts[1], out int quantity) || quantity <= 0)
                        return BadRequest($"Неверное количество: \"{parts[1]}\"");

                    orderItems.Add((dishId, quantity));
                }

                if (orderItems.Count == 0)
                    return BadRequest("Список блюд пуст");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка парсинга: {ex.Message}");
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    if (!db.Menus.Any(m => m.Id == MenuId && m.IsActive))
                        return BadRequest($"Меню с ID {MenuId} не найдено");

                    var dishIds = orderItems.Select(i => i.DishId).ToList();

                    var availableDishes = db.Dishes
                        .Where(d => dishIds.Contains(d.Id))
                        .ToList();

                    foreach (var item in orderItems)
                    {
                        var dish = availableDishes.FirstOrDefault(d => d.Id == item.DishId);
                        if (dish == null)
                            return BadRequest($"Блюдо с ID {item.DishId} не найдено в базе");
                        if (dish.MenuId != MenuId)
                            return BadRequest($"Блюдо с ID {item.DishId} не принадлежит меню {MenuId}");
                    }

                    decimal totalAmount = 0;
                    foreach (var orderItem in orderItems)
                    {
                        var dish = availableDishes.First(d => d.Id == orderItem.DishId);
                        totalAmount += dish.Price * orderItem.Quantity;
                    }
                    var newOrder = new Orders
                    {
                        UserId = userId,
                        TotalAmount = (double)totalAmount
                    };

                    db.Orders.Add(newOrder);
                    db.SaveChanges();

                    foreach (var item in orderItems)
                    {
                        db.OrderItems.Add(new OrderItems
                        {
                            OrderId = newOrder.Id,
                            DishId = item.DishId,
                            Quantity = item.Quantity
                        });
                    }

                    db.SaveChanges();

                    return Ok(new
                    {
                        OrderId = newOrder.Id,
                        TotalAmount = totalAmount,
                        Message = "Заказ успешно создан"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка сервера: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение истории заказов пользователя (требуется токен)
        /// </summary>
        /// <param name="Token">Токен пользователя</param>
        /// <returns>Список заказов с деталями</returns>
        /// <response code="200">История получена</response>
        /// <response code="401">Неверный токен</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("history")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult GetOrderHistory([FromQuery] string Token)
        {
            // 🔐 Валидация токена (аналогично CreateOrder)
            if (string.IsNullOrEmpty(Token))
                return StatusCode(401, "Токен не указан");

            int userId;
            try
            {
                using (var db = new AppDbContext())
                {
                    var allUsers = db.Users.ToList();
                    var matchedUser = allUsers.FirstOrDefault(u =>
                        BCrypt.Net.BCrypt.Verify(u.Id.ToString(), Token));

                    if (matchedUser == null)
                        return StatusCode(401, "Неверный токен");

                    userId = matchedUser.Id;
                }
            }
            catch
            {
                return StatusCode(401, "Ошибка проверки токена");
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var orders = db.Orders
                        .Where(o => o.UserId == userId)
                        .Select(o => new
                        {
                            o.Id,
                            o.TotalAmount,
                            Items = db.OrderItems
                                .Where(oi => oi.OrderId == o.Id)
                                .Select(oi => new
                                {
                                    DishName = db.Dishes.First(d => d.Id == oi.DishId).Name,
                                    oi.Quantity,
                                    Price = db.Dishes.First(d => d.Id == oi.DishId).Price
                                })
                                .ToList()
                        })
                        .ToList();

                    return Ok(orders);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }
    }
}