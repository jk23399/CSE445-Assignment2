﻿using System;
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
    //delegate declaration for creating events
    public delegate void PriceCutEvent(double stockPrice, Thread agentThread);
    public delegate void OrderProcessEvent(Order order, double orderAmount);
    public delegate void OrderCreationEvent();

    public class MainClass
    {
        public static MultiCellBuffer buffer;
        public static Thread[] investmentAgentThreads;
        public static bool companyThreadRunning = true;
        public static void Main(string[] args)
        {
            
            Console.WriteLine("Inside Main");
            buffer = new MultiCellBuffer();

            Company company = new Company();
            InvestmentAgent investmentAgent = new InvestmentAgent();

            Thread companyThread = new Thread(new ThreadStart(company.StockFun));
            companyThread.Start();

            Company.PriceCut += new PriceCutEvent(investmentAgent.agentOrder);
            Console.WriteLine("Price cut event has been subscribed");
            InvestmentAgent.orderCreation += new OrderCreationEvent(company.takeOrder);
            Console.WriteLine("Order creation event has been subscribed");
            OrderProcessing.OrderProcess += new OrderProcessEvent(investmentAgent.orderProcessConfirm);
            Console.WriteLine("Order process event has been subscribed");

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
        private const int bufferSize = 3; //buffer size
        int usedCells;
        private Order[] multiCells; // ? mark make the type nullable: allow to assign null value
        public static Semaphore getSemaph;
        public static Semaphore setSemaph;

        // extra: per-cell locks to guard reads/writes
        private object[] cellLocks;

        public MultiCellBuffer() //constructor 
        {
            // add your implementation here
        }

        public void SetOneCell(Order data)
        {       
            // add your implementation here
        }

        public Order GetOneCell()
        {
            // add your implementation here
        }
    }
    public class Order
    {
        //identity of sender of order
        private string senderId;
        //credit card number
        private long cardNo;
        //unit price of stock from company
        private double unitPrice;
        //quantity of stocks to order
        private int quantity;

        //parametrized constructor
        public Order(string senderId, long cardNo, double unitPrice, int quantity)
        {
            // add your implementation here
        }

        //getter methods
        public string getSenderId()
        {
            // add your implementation here
        }

        public long getCardNo()
        {
            // add your implementation here
        }
        public double getUnitPrice()
        {
            // add your implementation here
        }
        public int getQuantity()
        {
            // add your implementation here
        }

    }
    public class OrderProcessing
    {
        public static event OrderProcessEvent OrderProcess;
       
        // shared RNG
        private static readonly object randLock = new object();
        private static readonly Random rng = new Random();

        //method to check for valid credit card number input
        public static bool creditCardCheck(long creditCardNumber)
        {
            // add your implementation here
        }

        //method to calculate the final charge after adding taxes, location charges, etc
        public static double calculateCharge(double unitPrice, int quantity)
        {
            // add your implementation here
        }

        //method to process the order
        public static void ProcessOrder(Order order)
        {
            // add your implementation here
        }
    }
    public class InvestmentAgent
    {
        public static event OrderCreationEvent orderCreation;

        // last seen price per agent id (thread name) for order creation
        private static readonly Dictionary<string, double> lastPrice = new Dictionary<string, double>();
        private static readonly object priceLock = new object();

        // RNG
        private static readonly object randLock = new object();
        private static readonly Random rng = new Random();

        public void agentFun()
        {
            // add your implementation here
        }

        public void orderProcessConfirm(Order order, double orderAmount)
        {
            // add your implementation here
        }

        private void createOrder(string senderId)
        {
            // add your implementation here
        }

        public void agentOrder(double stockPrice, Thread investmentAgent) // Callback from company thread
        {
            // add your implementation here
        }
    }
    public class Company
    {
        static double currentStockPrice = 100; //random current agent price
        static int threadNo = 0;
        static int eventCount = 0;
        public static event PriceCutEvent PriceCut;

        public void StockFun()
        {
            // add your implementation here
        }
        //using random method to generate random stock prices
        public double pricingModel()
        {
            Random rnd = new Random(); 
            double price = rnd.Next(70, 150);
            return price;
        }

        public void updatePrice(double newStockPrice)
        {
            // add your implementation here
        }

        public void takeOrder() // callback from investment agent
        {
            // add your implementation here
        } 
    }
}
