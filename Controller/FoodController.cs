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
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v2")]
    public class OrdersController : ControllerBase
    {
        private int? ValidateToken(string token, AppDbContext db)
        {
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var users = db.Users.ToList();
                var user = users.FirstOrDefault(u => BCrypt.Net.BCrypt.Verify(u.Id.ToString(), token));
                return user?.Id;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Отправка заказа (требуется токен пользователя)
        /// </summary>
        /// <param name="Token">Токен пользователя (хэшированный Id)</param>
        /// <param name="MenuId">ID версии меню</param>
        /// <param name="Items">Список блюд в формате "DishId:Quantity" (например: "1:2,3:1")</param>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult CreateOrder(
            [FromForm] string Token,
            [FromForm] int MenuId,
            [FromForm] string Items)
        {

            using (var db = new AppDbContext())
            {
                var userId = ValidateToken(Token, db);
                if (userId == null)
                    return StatusCode(401, "Неверный или отсутствующий токен");

                if (string.IsNullOrEmpty(Items))
                    return BadRequest("Список блюд не указан. Формат: \"1:2,3:1\"");

                var orderItems = new List<(int DishId, int Quantity)>();
                var rawItems = Items.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in rawItems)
                {
                    var parts = item.Split(':');
                    if (parts.Length != 2)
                        return BadRequest($"Неверный формат: \"{item}\"");

                    if (!int.TryParse(parts[0], out int dishId) || dishId <= 0 ||
                        !int.TryParse(parts[1], out int quantity) || quantity <= 0)
                        return BadRequest($"Неверные данные: \"{item}\"");

                    orderItems.Add((dishId, quantity));
                }

                if (!db.Menus.Any(m => m.Id == MenuId && m.IsActive))
                    return BadRequest($"Меню с ID {MenuId} не найдено");

                var dishIds = orderItems.Select(i => i.DishId).ToList();
                var availableDishes = db.Dishes.Where(d => dishIds.Contains(d.Id)).ToList();

                foreach (var item in orderItems)
                {
                    var dish = availableDishes.FirstOrDefault(d => d.Id == item.DishId);
                    if (dish == null || dish.MenuId != MenuId)
                        return BadRequest($"Блюдо с ID {item.DishId} не найдено в этом меню");
                }

                decimal totalAmount = 0;
                foreach (var item in orderItems)
                {
                    var dish = availableDishes.First(d => d.Id == item.DishId);
                    totalAmount += dish.Price * item.Quantity;
                }

                var newOrder = new Orders
                {
                    UserId = userId.Value,
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

        /// <summary>
        /// Получение истории заказов пользователя (требуется токен)
        /// </summary>
        /// <param name="Token">Токен пользователя</param>
        [HttpGet("history")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult GetOrderHistory([FromQuery] string Token)
        {
            using (var db = new AppDbContext())
            {
                var userId = ValidateToken(Token, db);
                if (userId == null)
                    return StatusCode(401, "Неверный или отсутствующий токен");

                var orders = db.Orders
                    .Where(o => o.UserId == userId)
                    .ToList();

                var result = new List<object>();

                foreach (var order in orders)
                {
                    var items = db.OrderItems
                        .Where(oi => oi.OrderId == order.Id)
                        .Select(oi => new
                        {
                            DishName = db.Dishes.First(d => d.Id == oi.DishId).Name,
                            oi.Quantity,
                            Price = db.Dishes.First(d => d.Id == oi.DishId).Price
                        })
                        .ToList();

                    result.Add(new
                    {
                        order.Id,
                        order.TotalAmount,
                        Items = items
                    });
                }

                return Ok(result);
            }
        }
    }
}