using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using OwinWebApiTest.Models;

namespace OwinWebApiTest.Controllers
{
    public class ItemsController : ApiController
    {
        Item[] items = new Item[]
        {
            new Item { id = 1, name = "Tomato Soup", type = "Groceries" },
            new Item { id = 2, name = "Yo-yo", type = "Toys" },
            new Item { id = 3, name = "Hammer", type = "Hardware" }
        };

        public IEnumerable<Item> GetAllItems()
        {
            return items;
        }

        [Authorize]
        public IHttpActionResult GetItem(int id)
        {
            var item = items.FirstOrDefault((i) => i.id == id);
            if (item == null) {
                return NotFound();
            }
            return Ok(item);
        }
    }
}
