using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using RabbitMQ.Client;
using Common;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class TestController : Controller
    {
        //
        // GET: /Test/
        public ActionResult Index()
        {
            string message = DateTime.Now.ToString() + " : message body";
            RPCClient client = new RPCClient(ConnectionPool.GetConnection());
            string replyMsg = client.Call(message);
            TestViewModel viewModel = new TestViewModel() { ReplyMessage = replyMsg };

            return View(viewModel);
        }
	}
}