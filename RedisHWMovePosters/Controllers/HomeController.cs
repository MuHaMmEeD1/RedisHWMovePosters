using Microsoft.AspNetCore.Mvc;
using RedisHWMovePosters.Models;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Diagnostics;

namespace RedisHWMovePosters.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDatabase _db;
        private readonly ISubscriber _subscriber;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

            // Redis'e ba?lanma
            var muxer = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { { "redis-16071.c114.us-east-1-4.ec2.redns.redis-cloud.com", 16071 } },
                    User = "default",
                    Password = "YJQ91T7S80iPSsnU5w6vD1Q7OBwrdK1x"
                }
            );

            _db = muxer.GetDatabase();
            _subscriber = muxer.GetSubscriber();

            SubscribeToMoveImagesChannel();
        }

        public IActionResult Index()
        {
            var imageList = GetMoveImagesFromRedis();

            return View(imageList);
        }

        private List<string> GetMoveImagesFromRedis()
        {
            List<string> images = new List<string>();
            var length = _db.ListLength("moveImages");

            for (int i = 0; i < length; i++)
            {
                var imageName = _db.ListGetByIndex("moveImages", i);
                images.Add(imageName);
            }

            return images;
        }

        private void SubscribeToMoveImagesChannel()
        {
            _subscriber.Subscribe("moveImages", (channel, message) =>
            {
             
                string imageName = message.ToString();
                _db.ListLeftPush("moveImages", imageName); 
            });
        }

        public IActionResult SendMessageToRedis(string imageName)
        {
            _subscriber.Publish("moveImages", imageName);

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
