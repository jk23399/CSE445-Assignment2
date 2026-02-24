using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/**
 * This template file is created for ASU CSE445 Distributed SW Dev Assignment 2.
 * Please do not modify or delete any existing class/variable/method names. However, you can add more variables and functions.
 * Uploading this file directly will not pass the autograder's compilation check, resulting in a grade of 0.
 * **/

namespace ConsoleApp1
{
    // Delegate for price cut event: notifies investment agents of new lower stock price
    public delegate void PriceCutEvent(double stockPrice, Thread agentThread);
    // Delegate for order process event: notifies investment agent of confirmed order and charge
    public delegate void OrderProcessEvent(Order order, double orderAmount);
    // Delegate for order creation event: notifies company that an order has been created
    public delegate void OrderCreationEvent();

    public class MainClass
    {
        public static MultiCellBuffer buffer;
        public static Thread[] investmentAgentThreads;
        public static bool companyThreadRunning = true;

        public static void Main(string[] args)
        {
            Console.WriteLine("Inside Main");

            // Initialize the shared multi-cell buffer
            buffer = new MultiCellBuffer();

            // Create company and investment agent objects
            Company company = new Company();
            InvestmentAgent investmentAgent = new InvestmentAgent();

            // Create and start the company thread
            Thread companyThread = new Thread(new ThreadStart(company.StockFun));
            companyThread.Start();

            // Subscribe investment agent to company's price cut event
            Company.PriceCut += new PriceCutEvent(investmentAgent.agentOrder);
            Console.WriteLine("Price cut event has been subscribed");

            // Subscribe company to investment agent's order creation event
            InvestmentAgent.orderCreation += new OrderCreationEvent(company.takeOrder);
            Console.WriteLine("Order creation event has been subscribed");

            // Subscribe investment agent to order processing confirmation event
            OrderProcessing.OrderProcess += new OrderProcessEvent(investmentAgent.orderProcessConfirm);
            Console.WriteLine("Order process event has been subscribed");

            // Create and start 5 investment agent threads
            investmentAgentThreads = new Thread[5];
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Creating  investment agent thread {0}", (i + 1));
                investmentAgentThreads[i] = new Thread(investmentAgent.agentFun);
                investmentAgentThreads[i].Name = (i + 1).ToString();
                investmentAgentThreads[i].Start();
            }
        }
    }

    public class MultiCellBuffer
    {
        // Each cell can contain an order object
        private const int bufferSize = 3;
        int usedCells;
        private Order[] multiCells;

        // Semaphore to track available cells for writing (starts at bufferSize = 3)
        public static Semaphore setSemaph;
        // Semaphore to track available cells for reading (starts at 0)
        public static Semaphore getSemaph;

        // Per-cell locks to prevent concurrent access to the same cell
        private object[] cellLocks;

        // Constructor initializes buffer, semaphores, and cell locks
        public MultiCellBuffer()
        {
            multiCells = new Order[bufferSize];
            usedCells = 0;
            setSemaph = new Semaphore(bufferSize, bufferSize); // 3 cells available for writing
            getSemaph = new Semaphore(0, bufferSize);          // 0 cells available for reading
            cellLocks = new object[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                cellLocks[i] = new object();
            }
        }

        // Investment agent writes an order into an available buffer cell
        public void SetOneCell(Order data)
        {
            setSemaph.WaitOne(); // Wait until a cell is available for writing
            for (int i = 0; i < bufferSize; i++)
            {
                lock (cellLocks[i])
                {
                    if (multiCells[i] == null) // Find an empty cell
                    {
                        Console.WriteLine("Setting in buffer Cell");
                        multiCells[i] = data;
                        Thread.Sleep(100); // Small delay to allow all cells to be used
                        usedCells++;
                        getSemaph.Release(); // Signal that a cell is available for reading
                        break;
                    }
                }
            }
            Console.WriteLine("Exit Setting in buffer");
        }

        // Company reads an order from an available buffer cell
        public Order GetOneCell()
        {
            getSemaph.WaitOne(); // Wait until a cell has data to read
            Order order = null;
            for (int i = 0; i < bufferSize; i++)
            {
                Monitor.Enter(cellLocks[i]);
                try
                {
                    if (multiCells[i] != null) // Find a cell with data
                    {
                        order = multiCells[i];
                        multiCells[i] = null; // Clear the cell after reading
                        usedCells--;
                        Console.WriteLine("Exit reading buffer");
                        setSemaph.Release(); // Signal that a cell is now free for writing
                        break;
                    }
                }
                finally
                {
                    Monitor.Exit(cellLocks[i]);
                }
            }
            return order;
        }
    }

    public class Order
    {
        // Identity of the sender of the order (thread name or ID)
        private string senderId;
        // Credit card number for payment
        private long cardNo;
        // Unit price of stock received from the company
        private double unitPrice;
        // Number of stocks to order
        private int quantity;

        // Parameterized constructor to initialize all fields
        public Order(string senderId, long cardNo, double unitPrice, int quantity)
        {
            this.senderId = senderId;
            this.cardNo = cardNo;
            this.unitPrice = unitPrice;
            this.quantity = quantity;
        }

        // Returns the sender ID
        public string getSenderId()
        {
            return senderId;
        }

        // Returns the credit card number
        public long getCardNo()
        {
            return cardNo;
        }

        // Returns the unit price
        public double getUnitPrice()
        {
            return unitPrice;
        }

        // Returns the quantity of stocks ordered
        public int getQuantity()
        {
            return quantity;
        }
    }

    public class OrderProcessing
    {
        // Event to notify investment agent of processed order and total charge
        public static event OrderProcessEvent OrderProcess;

        private static readonly object randLock = new object();
        private static readonly Random rng = new Random();

        // Validates credit card number - must be between 5000 and 7000
        public static bool creditCardCheck(long creditCardNumber)
        {
            return creditCardNumber >= 5000 && creditCardNumber <= 7000;
        }

        // Calculates total charge: unitPrice * quantity + tax (8-12%) + processing fee ($20-$80)
        public static double calculateCharge(double unitPrice, int quantity)
        {
            double baseCharge = unitPrice * quantity;
            double taxRate;
            double processingFee;
            lock (randLock)
            {
                taxRate = 1.0 + (rng.NextDouble() * 0.04 + 0.08); // 8% to 12% tax
                processingFee = rng.Next(20, 80);                  // $20 to $80 processing fee
            }
            return Math.Round(baseCharge * taxRate + processingFee, 2);
        }

        // Processes the order: validates card, calculates charge, fires confirmation event
        public static void ProcessOrder(Order order)
        {
            if (!creditCardCheck(order.getCardNo()))
            {
                Console.WriteLine("Invalid credit card number for Agent {0}", order.getSenderId());
                return;
            }
            double totalCharge = calculateCharge(order.getUnitPrice(), order.getQuantity());
            // Fire event to notify investment agent of the confirmed order amount
            OrderProcess?.Invoke(order, totalCharge);
        }
    }

    public class InvestmentAgent
    {
        // Event fired when an investment agent creates an order
        public static event OrderCreationEvent orderCreation;

        // Latest price from company, shared across all agents
        private static double latestPrice = 0;
        private static readonly object priceLock = new object();

        private static readonly object randLock = new object();
        private static readonly Random rng = new Random();

        // Main loop: each agent thread detects price change and creates its own order
        public void agentFun()
        {
            Console.WriteLine("Starting investment agent now");
            string myId = Thread.CurrentThread.Name;
            double lastSeenPrice = 0;

            while (MainClass.companyThreadRunning)
            {
                double currentPrice;
                lock (priceLock)
                {
                    currentPrice = latestPrice;
                }
                // If price has changed, create a new order
                if (currentPrice != lastSeenPrice && currentPrice != 0)
                {
                    lastSeenPrice = currentPrice;
                    createOrder(myId);
                }
                Thread.Sleep(100);
            }
        }

        // Callback: called when order processing is confirmed, prints the charge
        public void orderProcessConfirm(Order order, double orderAmount)
        {
            Console.WriteLine("Investment Agent {0}'s order is confirmed. The amount to be charged is ${1}",
                order.getSenderId(), orderAmount);
        }

        // Creates an order and writes it to the shared buffer
        private void createOrder(string senderId)
        {
            Console.WriteLine("Inside create order");
            long cardNo;
            int qty;
            lock (randLock)
            {
                cardNo = rng.Next(5000, 7001);
                qty = rng.Next(1, 20);
            }
            Order order = new Order(senderId, cardNo, latestPrice, qty);
            // Fire order creation event to notify the company
            orderCreation?.Invoke();
            MainClass.buffer.SetOneCell(order);
        }

        // Callback from company thread: stores new price to signal all agent threads
        public void agentOrder(double stockPrice, Thread investmentAgent)
        {
            lock (priceLock)
            {
                latestPrice = stockPrice;
            }
        }
    }

    public class Company
    {
        static double currentStockPrice = 100; // Current stock price
        static int threadNo = 0;
        static int eventCount = 0;             // Counts how many price cut events have fired

        // Event fired when stock price drops below previous price
        public static event PriceCutEvent PriceCut;

        // Main company thread: generates prices, fires price cut events, reads orders
        public void StockFun()
        {
            // Run for 10 price cut events then terminate
            while (eventCount < 10)
            {
                Thread.Sleep(500); // Wait before generating new price
                double newPrice = pricingModel();
                Console.WriteLine("New price is {0}", newPrice);
                updatePrice(newPrice);
            }
            // Wait for all agent orders to be processed before terminating
            Thread.Sleep(3000);
            // Signal investment agents to stop
            MainClass.companyThreadRunning = false;
            Console.WriteLine("Company thread terminating after {0} price cut events", eventCount);
        }

        // Pricing model: generates a random stock price between 80 and 160
        public double pricingModel()
        {
            Random rnd = new Random();
            double price = rnd.Next(80, 161);
            return price;
        }

        // Updates the stock price and fires price cut event if new price is lower
        public void updatePrice(double newStockPrice)
        {
            if (newStockPrice < currentStockPrice)
            {
                currentStockPrice = newStockPrice;
                eventCount++;
                Console.WriteLine("Updating the price and calling price cut event");
                // Fire price cut event for all subscribed investment agents
                PriceCut?.Invoke(newStockPrice, Thread.CurrentThread);
                // Wait for all 5 agents to respond before generating next price
                Thread.Sleep(2000);
            }
            else
            {
                currentStockPrice = newStockPrice;
            }
        }

        // Callback from investment agent: reads order from buffer and processes it in a new thread
        public void takeOrder()
        {
            Console.WriteLine("Incoming order from stock with price {0}", currentStockPrice);

            // Run in a separate thread to avoid blocking the company thread
            Thread t = new Thread(() =>
            {
                Order order = MainClass.buffer.GetOneCell();
                if (order != null)
                {
                    threadNo++;
                    Thread orderThread = new Thread(() => OrderProcessing.ProcessOrder(order));
                    orderThread.Start();
                }
            });
            t.Start();
        }
    }
}