using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Common;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    public class TestController : AsyncController
    {
        //
        // GET: /Test/
        public async Task<ActionResult> Index()
        {
            string message = "消息：" + new Random().Next(1, 10000).ToString(); // DateTime.Now.ToString() + " : message body";

            RPCClient client = null;
            try
            {
                //client = new RPCClient(ConnectionPool.GetConnection());
                client = RPCClient.GetInstance();
                string replyMsg = await client.CallAsync(message);
                TestViewModel viewModel = new TestViewModel() { ReplyMessage = replyMsg };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TestViewModel viewModel = new TestViewModel() { ReplyMessage = "broken has closed." };

                return View(viewModel);
            }
            //finally
            //{
            //    if(client!=null)
            //    {
            //        client.Close();
            //    }
            //}
            
        }

        [HttpPost]
        public async Task<MvcHtmlString> DoWork()
        {
            string message = "消息：" + new Random().Next(1, 10000).ToString();
            string replyMsg = "";
            RPCClient client = null;
            try
            {
                //client = new RPCClient(ConnectionPool.GetConnection());
                client = RPCClient.GetInstance();
                replyMsg = await client.CallAsync(message);
            }
            catch (AlreadyClosedException e)
            {
                replyMsg = "broken has closed.";
            }
            catch (NoRPCConsumeException ex)
            {
                replyMsg = ex.Message;
            }
            catch (Exception e)
            {
                replyMsg = e.Message;
            }
            //finally
            //{
            //    if (client != null)
            //    {
            //        client.Close();
            //    }
            //}
            
            //TestViewModel viewModel = new TestViewModel() { ReplyMessage = replyMsg };
            return new MvcHtmlString("<p>" + replyMsg + "</p>");
        }

        public async Task<ActionResult> Serv()
        {
            string message = "消息：" + new Random().Next(1, 10000).ToString();
            ServiceReference1.WebService1SoapClient client = new ServiceReference1.WebService1SoapClient();
            string reply = (await client.HandlerMessageAsync(message)).Body.HandlerMessageResult;
            TestViewModel viewModel = new TestViewModel() { ReplyMessage = reply };
            return View("Index", viewModel);
        }
	}
}