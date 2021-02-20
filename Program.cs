using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace _11
{
    class Program
    {

        static void Main(string[] args)
        {

            {
                var c = new ClientsObserver(db, dbLock);
                c.Start(null);
                var cli = new Client(db, dbLock, 1, 123, 20);
                cli.Insert();
                cli.Balance += 100;
                c.Start(null);
                cli.Balance -= 50;
                c.Start(null);
                cli.Balance += 100;
                c.Start(null);
            }
            Console.WriteLine("<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>");
            db.Clear();

            var clientsObserver = new ClientsObserver(db, dbLock);
            var interval = 1_000;
            var timer = new Timer(clientsObserver.Start, null, 0, interval);

            var client = new Client(db, dbLock, 1, 123, 20);
            var t = new Thread(client.Insert);
            t.Start();
            t.Join();
            client.Insert();
            var anotherClient = new Client(db, dbLock, 2, 10_000, 34);
            anotherClient.Insert();
            print();
            Console.WriteLine("======================");

            client.Age = 23;
            t = new Thread(client.Update);
            t.Start();
            t.Join();
            print();
            Console.WriteLine("======================");

            var someClient = new Client(db, dbLock, 2);
            t = new Thread(client.Select);
            t.Start();
            t.Join();
            someClient.Select();
            Console.WriteLine(someClient);
            Console.WriteLine("======================");

            t = new Thread(client.Delete);
            t.Start();
            t.Join();
            print();

            anotherClient.Balance += 100;
            anotherClient.Update();
            Thread.Sleep(2 * interval);

            anotherClient.Balance -= 50;
            anotherClient.Update();
            Thread.Sleep(2 * interval);
        }

        static void print() => db.ForEach(x => Console.WriteLine(x));

        static public Object dbLock = new Object();
        static List<Client> db = new List<Client>();
    }



    record Client
    {
        public Client(List<Client> db, Object dbLock, int ID, decimal balance, int age)
        {
            this.dbLock = dbLock;
            this.db = db;
            this.ID = ID;
            this.Balance = balance;
            this.Age = age;
        }

        public Client() { }

        public Client(Client client)
        {
            this.ID = client.ID;
            this.Balance = client.Balance;
            this.Age = client.Age;
            this.db = client.db;
            this.dbLock = client.dbLock;
        }

        public Client(List<Client> db, Object dbLock, int ID)
        {
            this.dbLock = dbLock;
            this.db = db;
            this.ID = ID;
        }
        private Object dbLock { get; set; }
        private List<Client> db { get; set; }
        public int ID { get; set; }
        public decimal Balance { get; set; }
        public int Age { get; set; }

        public void Insert()
        {
            lock (dbLock)
            {
                db.Add(this);
            }
        }

        public void Update()
        {
            try
            {
                lock (dbLock)
                {
                    db[db.IndexOf(this)] = this;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // no such client
            }
        }


        public void Delete()
        {
            lock (dbLock)
            {
                db.Remove(this);
            }
        }

        public void Select()
        {
            lock (dbLock)
            {
                var client = db.Find((Client client) => client.ID == this.ID);
                this.Age = client.Age;
                this.Balance = client.Balance;
            }
        }

    }

    class ClientsObserver
    {
        private List<Client> db { get; set; }
        private Object dbLock { get; set; }

        private List<Client> oldDB { get; set; }
        public ClientsObserver(List<Client> db, Object dbLock)
        {
            this.db = db;
            this.dbLock = dbLock;
            oldDB = new List<Client>();
            db.ForEach((Client client) => oldDB.Add(new Client(client)));
        }
        public void Start(Object _)
        {
            lock (dbLock)
            {
                foreach (var client in db)
                {
                    var oldClient = oldDB.Find((Client c) => c.ID == client.ID);
                    if (oldClient == null)
                        continue;
                    if (oldClient.Balance != client.Balance)
                    {
                        showDifference(oldClient, client);
                    }
                }
                oldDB.Clear();
                db.ForEach((Client client) => oldDB.Add(new Client(client)));
            }
        }

        private void showDifference(Client oldClient, Client newClient)
        {
            if (newClient.Balance > oldClient.Balance)
                print(oldClient, newClient, "+", ConsoleColor.Green);
            else
                print(oldClient, newClient, "-", ConsoleColor.Red);
        }

        private void print(Client oldClient, Client newClient, string symbol, ConsoleColor color)
        {
            var tmp = Console.ForegroundColor;
            Console.ForegroundColor = color;

            Console.WriteLine($"ID: {newClient.ID}");
            Console.WriteLine($"Old Balance: {oldClient.Balance}");
            Console.WriteLine($"New Balance: {newClient.Balance}");
            Console.WriteLine($"Difference: {symbol}{Math.Abs(newClient.Balance - oldClient.Balance)}");

            Console.ForegroundColor = tmp;
        }
    }
}
