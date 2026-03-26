using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using RestApiKazakov.Context;
using RestApiKazakov.Models;
using BCrypt.Net;

namespace RestApiKazakov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v2")]
    public class OrdersController : ControllerBase
    {
        // 🔐 Проверка токена
        private int? ValidateToken(string token, AppDbContext db)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var users = db.Users.ToList();
                var user = users.FirstOrDefault(u => BCrypt.Net.BCrypt.Verify(u.Id.ToString(), token));
                return user?.Id;
            }
            catch { return null; }
        }

        /// <summary>
        /// Отправка заказа (поля: строки с ID и количествами через запятую)
        /// Пример: DishIds="1,3,5", Counts="2,1,1"
        /// </summary>
        [HttpPost("order")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult CreateOrder(
            [FromForm] string Token,
            [FromForm] string Address,
            [FromForm] string DishIds, 
            [FromForm] string Counts)
        {
            // 🔐 1. Проверка токена
            if (string.IsNullOrEmpty(Token))
                return StatusCode(401, "Токен не указан");

            int userId;
            try
            {
                using (var db = new AppDbContext())
                {
                    var userIdValidated = ValidateToken(Token, db);
                    if (userIdValidated == null)
                        return StatusCode(401, "Неверный токен");
                    userId = userIdValidated.Value;
                }
            }
            catch
            {
                return StatusCode(401, "Ошибка проверки токена");
            }

            if (string.IsNullOrEmpty(Address))
                return BadRequest("Адрес не указан");

            if (string.IsNullOrEmpty(DishIds) || string.IsNullOrEmpty(Counts))
                return BadRequest("Не указаны DishIds или Counts");

            int[] dishIdsArray;
            int[] countsArray;

            try
            {
                dishIdsArray = DishIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse).ToArray();
                countsArray = Counts.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToArray();
            }
            catch
            {
                return BadRequest("Неверный формат данных. Используйте числа, разделенные запятой (например: 1,3,5)");
            }

            if (dishIdsArray.Length != countsArray.Length || dishIdsArray.Length == 0)
                return BadRequest("Количество ID блюд и количеств должно совпадать и быть больше нуля");

            try
            {
                using (var db = new AppDbContext())
                {
                    var availableDishes = db.Dishes
                        .Where(d => dishIdsArray.Contains(d.Id))
                        .ToList();

                    var missingIds = dishIdsArray.Except(availableDishes.Select(d => d.Id)).ToList();
                    if (missingIds.Any())
                        return BadRequest($"Блюда с ID не найдены: {string.Join(", ", missingIds)}");

                    decimal totalAmount = 0;
                    for (int i = 0; i < dishIdsArray.Length; i++)
                    {
                        var dish = availableDishes.First(d => d.Id == dishIdsArray[i]);
                        totalAmount += dish.Price * countsArray[i];
                    }

                    var newOrder = new Orders
                    {
                        UserId = userId,
                        TotalAmount = totalAmount
                    };

                    db.Orders.Add(newOrder);
                    db.SaveChanges();

                    for (int i = 0; i < dishIdsArray.Length; i++)
                    {
                        db.OrderItems.Add(new OrderItems
                        {
                            OrderId = newOrder.Id,
                            DishId = dishIdsArray[i],
                            Quantity = countsArray[i]
                        });
                    }

                    db.SaveChanges();

                    return Ok(new
                    {
                        message = "Заказ принят",
                        orderId = newOrder.Id
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка сервера: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение истории заказов (требуется токен)
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult GetOrderHistory([FromQuery] string Token)
        {
            if (string.IsNullOrEmpty(Token))
                return StatusCode(401, "Токен не указан");

            int userId;
            try
            {
                using (var db = new AppDbContext())
                {
                    var userIdValidated = ValidateToken(Token, db);
                    if (userIdValidated == null)
                        return StatusCode(401, "Неверный токен");
                    userId = userIdValidated.Value;
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
                        .ToList(); 

                    var result = new List<object>();

                    foreach (var order in orders)
                    {
                        var orderItems = db.OrderItems
                            .Where(oi => oi.OrderId == order.Id)
                            .ToList();

                        var dishesList = new List<object>();
                        foreach (var item in orderItems)
                        {
                            var dish = db.Dishes.FirstOrDefault(d => d.Id == item.DishId);
                            if (dish != null)
                            {
                                dishesList.Add(new
                                {
                                    dishId = item.DishId,
                                    dishName = dish.Name,
                                    count = item.Quantity,
                                    price = dish.Price
                                });
                            }
                        }

                        result.Add(new
                        {
                            orderId = order.Id,
                            totalAmount = order.TotalAmount,
                            dishes = dishesList
                        });
                    }

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }
    }
}