using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MultiProcessForOrders
{
    class Program
    {

        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(10, 10);
            Console.WriteLine("Hello World!");
            //var thread=new Thread(new ThreadStart(NewOrder));
            //thread.Start();
            ThreadPool.QueueUserWorkItem(MockGetOrder);
           ThreadPool. QueueUserWorkItem(MockNewOrder);
            
            
       
            Console.ReadKey();
        }

        /// <summary>
        /// 模拟订单创建
        /// </summary>
        static void MockNewOrder(object state)
        {
            while (true)
            {
                var t = new Thread(NewOrder);
                t.Start();

                Thread.Sleep(10000);
            }
        }

       
        /// <summary>
        /// 模拟拿取队列
        /// </summary>
        static void MockGetOrder(object state)
        {

            while (true) { 
            
            lock (lockobject)
                {
                    List<string> customerInQueue = new List<string>();
                    IEnumerable<IGrouping<string, Order>> ordersToHandle;
                   // Log($"    开始获取处理队列");
                ordersToHandle = orderQueue
                        //.Where(x => !customerInQueue.Contains(x.CustomerId))
                        .Where(x=>x.IsProcessing==false)
                     .GroupBy(x => x.CustomerId)
                     ;
                    ordersToHandle.Select(x=>x.Select(y=>y.IsProcessing=true));
                //每个用户创建一个线程
                customerInQueue.AddRange(ordersToHandle.Select(x => x.Key));
                    //Log("正在处理的用户列表:"+string.Join(" ",customerInQueue));
                    foreach (var orderToHandle in ordersToHandle)
                    {
                        string customerId=orderToHandle.Key;
                        ThreadPool.QueueUserWorkItem(OrderProcess, orderToHandle.Select(x => x).ToList());
                        var hasRemove = customerInQueue.Remove(customerId);
                        if (hasRemove)
                        {
                            //Log($"    4 成功移除客户[{customerId}]");
                        }
                        else
                        {
                           // Log($"{Thread.CurrentThread.ManagedThreadId}    5 未能移除客户[{customerId}]");

                        }
                    }
                }
           
            Thread.Sleep(10000);
            }
        }

        static void OrderProcess(object state)
        {
            var orders = state as List<Order>;
            Debug.Assert(orders.Select(x => x.CustomerId).Distinct().Count() == 1, "获取的订单应该属于同一个客户");
            string customerId = orders.First().CustomerId;
            Log($"  {Thread.CurrentThread.ManagedThreadId}        1正在处理客户[{customerId}]的订单");
            foreach (var order in orders)
            {
                Log($"        2正在处理订单:{order.ToString()}");
                Thread.Sleep(3000);

            }
           // Log($"    3客户[{customerId}]的订单处理完毕");
          

        }
        static void Log(string message)
        { 
            Console.WriteLine($"{Thread.GetCurrentProcessorId()}{message}");
            }
        static IList<Order> orderQueue = new List<Order>();
        private static void NewOrder()
        {
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(1007);
                var order = new Order { CustomerId = "customer_" + i };
                orderQueue.Add(order);
                Console.WriteLine($"创建新订单:[{order}]");
            }

        }
        static object lockobject = new object();
    }



    public class Order
    {
        public string OrderId { get; set; } = "Order_" + DateTime.Now.ToString("ddHH-mm-ss-fff");
        public string CustomerId { get; set; }
        public bool IsProcessing { get;set;}=false;
        public override string ToString()
        {
            return CustomerId + "_" + OrderId+"_"+IsProcessing;
        }
    }
}
