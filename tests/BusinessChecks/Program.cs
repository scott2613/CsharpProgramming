using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Application.Services;
using ComputerStore.Core.Entities;

static class Checks
{
    static readonly CancellationToken Ct = CancellationToken.None;
    static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}"); }
    static async Task Reject(Func<Task> work, int status)
    {
        try { await work(); }
        catch (OrderOperationException ex) { Equal(status, ex.StatusCode); return; }
        throw new Exception("Expected rejection, operation unexpectedly succeeded.");
    }
    static MemoryStore Store() => new() { Products = new() { [10] = new Product { ProductId=10, Name="Laptop", Price=100, StockQuantity=5 },
        [20] = new Product { ProductId=20, Name="Desktop", Price=200, StockQuantity=0 } } };
    static PlaceOrderRequest Request(params (int Id,int Qty)[] lines) => new()
    {
        CustomerName="Người nhận", CustomerPhone="0912345678", DeliveryAddress="123 Đường A", PaymentMethod="COD",
        OrderDetails=lines.Select(l=>new OrderLineRequest{ProductId=l.Id,Quantity=l.Qty}).ToList()
    };
    public static async Task Main()
    {
        var cases = new (string Name, Func<Task> Test)[]
        {
            ("Negative quantity does not increase stock",async()=>{
                var db=Store(); await Reject(()=>new OrderService(db).PlaceAsync(2,Request((10,-1)),Ct),400); Equal(5,db.Products[10].StockQuantity); Equal(0,db.Orders.Count);
            }),
            ("Duplicate IDs aggregate once at database price",async()=>{
                var db=Store(); var o=await new OrderService(db).PlaceAsync(2,Request((10,1),(10,2)),Ct);
                Equal(300m,o.TotalAmount);Equal(2,db.Products[10].StockQuantity);Equal((int?)2,db.Orders[0].UserId);Equal(1,db.Orders[0].OrderDetails.Count);
            }),
            ("Shortage on second product rolls back first product",async()=>{
                var db=Store();await Reject(()=>new OrderService(db).PlaceAsync(2,Request((10,1),(20,1)),Ct),409);Equal(5,db.Products[10].StockQuantity);Equal(0,db.Orders.Count);
            }),
            ("Changed price keeps cart and stock",async()=>{
                var db=Store();db.Cart.Add(new(){UserId=2,ProductId=10,Quantity=2});var request=Request();request.ExpectedTotal=1;
                await Reject(()=>new OrderService(db).PlaceCartAsync(2,request,Ct),409);Equal(1,db.Cart.Count);Equal(5,db.Products[10].StockQuantity);Equal(0,db.Orders.Count);
            }),
            ("Checkout commits order stock and cart consumption",async()=>{
                var db=Store();db.Cart.Add(new(){UserId=2,ProductId=10,Quantity=2});await new OrderService(db).PlaceCartAsync(2,Request(),Ct);
                Equal(0,db.Cart.Count);Equal(1,db.Orders.Count);Equal(3,db.Products[10].StockQuantity);
            }),
            ("Replayed checkout with empty cart cannot create duplicate",async()=>{
                var db=Store();db.Cart.Add(new(){UserId=2,ProductId=10,Quantity=1});var service=new OrderService(db);
                await service.PlaceCartAsync(2,Request(),Ct);await Reject(()=>service.PlaceCartAsync(2,Request(),Ct),409);Equal(1,db.Orders.Count);
            }),
            ("Wrong customer cannot cancel another customer's order",async()=>{
                var db=Store();var service=new OrderService(db);var o=await service.PlaceAsync(2,Request((10,2)),Ct);
                await Reject(()=>service.CancelOwnedAsync(3,o.OrderId,Ct),404);Equal(3,db.Products[10].StockQuantity);Equal(OrderStatuses.Pending,db.Orders[0].Status);
            }),
            ("Owner cancellation twice restores stock only once",async()=>{
                var db=Store();var service=new OrderService(db);var o=await service.PlaceAsync(2,Request((10,2)),Ct);
                await service.CancelOwnedAsync(2,o.OrderId,Ct);await service.CancelOwnedAsync(2,o.OrderId,Ct);Equal(5,db.Products[10].StockQuantity);Equal(OrderStatuses.Cancelled,db.Orders[0].Status);
            }),
            ("Customer cannot cancel an accepted order",async()=>{
                var db=Store();var service=new OrderService(db);var o=await service.PlaceAsync(2,Request((10,1)),Ct);
                await service.UpdateStatusAsync(1,o.OrderId,OrderStatuses.Confirmed,Ct);await Reject(()=>service.CancelOwnedAsync(2,o.OrderId,Ct),409);Equal(4,db.Products[10].StockQuantity);
            }),
            ("Completed orders cannot be deleted or reopened",async()=>{
                var db=Store();var service=new OrderService(db);var o=await service.PlaceAsync(2,Request((10,1)),Ct);
                await service.UpdateStatusAsync(1,o.OrderId,OrderStatuses.Completed,Ct);
                await Reject(()=>service.DeleteAsync(1,o.OrderId,Ct),409);await Reject(()=>service.UpdateStatusAsync(1,o.OrderId,OrderStatuses.Pending,Ct),409);Equal(4,db.Products[10].StockQuantity);
            }),
            ("Customer cannot call admin status update",async()=>{
                var db=Store();var service=new OrderService(db);var o=await service.PlaceAsync(2,Request((10,1)),Ct);
                await Reject(()=>service.UpdateStatusAsync(2,o.OrderId,OrderStatuses.Cancelled,Ct),403);Equal(4,db.Products[10].StockQuantity);
            }),
            ("Rebuy adds every owned order line without reducing stock", async () =>
            {
                var db = Store();
                db.Products[20].StockQuantity = 5;
                var order = await new OrderService(db).PlaceAsync(2, Request((10,1),(20,2)), Ct);
                await new CartService(db).RebuyAsync(2, order.OrderId, Ct);
                Equal(2, db.Cart.Count);
                Equal(1, db.Cart.Single(x => x.ProductId == 10).Quantity);
                Equal(2, db.Cart.Single(x => x.ProductId == 20).Quantity);
                Equal(4, db.Products[10].StockQuantity);
                Equal(3, db.Products[20].StockQuantity);
            }),
            ("Rebuy rejects another customer's order and rolls back a partial cart", async () =>
            {
                var db = Store();
                db.Products[20].StockQuantity = 1;
                var order = await new OrderService(db).PlaceAsync(2, Request((10,1),(20,1)), Ct);
                var carts = new CartService(db);
                await Reject(() => carts.RebuyAsync(3, order.OrderId, Ct), 404);
                await Reject(() => carts.RebuyAsync(2, order.OrderId, Ct), 409);
                Equal(0, db.Cart.Count);
            }),
            ("Cart remains isolated by account across service instances",async()=>{
                var db=Store();await new CartService(db).AddAsync(2,10,2,Ct);
                Equal(2,(await new CartService(db).ReadAsync(2,Ct)).Single().Quantity);Equal(0,(await new CartService(db).ReadAsync(3,Ct)).Count);
            })
        };
        foreach(var test in cases) { await test.Test();Console.WriteLine("PASS "+test.Name); }
        Console.WriteLine($"{cases.Length} business checks passed. SQL locking, HTTP, cookies and WinForms require integration testing.");
    }
}

// Transaction adapter for business behavior only; NOT a SQL/concurrency simulator.
sealed class MemoryStore : IOrderStore
{
    public Dictionary<int,Product> Products=new(); public List<Order> Orders=new(); public List<ShoppingCartItem> Cart=new();
    static Product Copy(Product p)=>new(){ProductId=p.ProductId,Name=p.Name,Price=p.Price,StockQuantity=p.StockQuantity};
    static ShoppingCartItem Copy(ShoppingCartItem p)=>new(){UserId=p.UserId,ProductId=p.ProductId,Quantity=p.Quantity};
    static Order Copy(Order o)=>new(){OrderId=o.OrderId,UserId=o.UserId,CustomerName=o.CustomerName,CustomerPhone=o.CustomerPhone,DeliveryAddress=o.DeliveryAddress,
        OrderDate=o.OrderDate,Status=o.Status,TotalAmount=o.TotalAmount,OrderDetails=o.OrderDetails.Select(d=>new OrderDetail{OrderDetailId=d.OrderDetailId,OrderId=d.OrderId,ProductId=d.ProductId,Quantity=d.Quantity,UnitPrice=d.UnitPrice}).ToList()};
    public Task<bool> IsAdminAsync(int id,CancellationToken ct)=>Task.FromResult(id==1);
    public Task<bool> IsAccountAsync(int id,CancellationToken ct)=>Task.FromResult(id is 1 or 2 or 3);
    public Task<List<Product>> CartProductsAsync(int[] ids,CancellationToken ct)=>Task.FromResult(Products.Values.Where(p=>ids.Contains(p.ProductId)).ToList());
    public Task<List<OrderSummaryDto>> ListAsync(string? search,string? phone,CancellationToken ct)=>Task.FromResult(new List<OrderSummaryDto>());
    public Task<OrderSummaryDto?> ReadAsync(int id,int actor,bool admin,CancellationToken ct)
    {
        var o=Orders.FirstOrDefault(x=>x.OrderId==id&&(admin||x.UserId==actor));
        return Task.FromResult(o==null?null:new OrderSummaryDto{OrderId=o.OrderId,UserId=o.UserId,TotalAmount=o.TotalAmount,Status=o.Status,
            OrderDetails=o.OrderDetails.Select(d => new OrderLineDto
            {
                ProductId = d.ProductId, Quantity = d.Quantity, UnitPrice = d.UnitPrice
            }).ToList()});
    }
    public Task<IOrderTransaction> BeginAsync(CancellationToken ct)=>Task.FromResult<IOrderTransaction>(new Tx(this));
    public Task<Product?> LockProductAsync(int id,CancellationToken ct)=>Task.FromResult(Products.GetValueOrDefault(id));
    public Task<Order?> LockOrderAsync(int id,CancellationToken ct)=>Task.FromResult(Orders.FirstOrDefault(o=>o.OrderId==id));
    public void Add(Order o){o.OrderId=Orders.Select(x=>x.OrderId).DefaultIfEmpty().Max()+1;Orders.Add(o);}
    public void Delete(Order o)=>Orders.Remove(o);
    public Task SaveAsync(CancellationToken ct)=>Task.CompletedTask;
    public Task<bool> LockCartOwnerAsync(int id,CancellationToken ct)=>IsAccountAsync(id,ct);
    public Task<List<ShoppingCartItem>> ReadCartAsync(int id,CancellationToken ct)=>Task.FromResult(Cart.Where(x=>x.UserId==id).ToList());
    public Task<List<ShoppingCartItem>> ReadCartForUpdateAsync(int id,CancellationToken ct)=>ReadCartAsync(id,ct);
    public void AddCartItem(ShoppingCartItem item)=>Cart.Add(item);
    public void RemoveCartItems(IEnumerable<ShoppingCartItem> items){foreach(var x in items.ToList())Cart.Remove(x);}
    sealed class Tx : IOrderTransaction
    {
        readonly MemoryStore db;readonly Dictionary<int,Product> products;readonly List<Order> orders;readonly List<ShoppingCartItem> cart;bool committed;
        public Tx(MemoryStore db){this.db=db;products=db.Products.ToDictionary(p=>p.Key,p=>Copy(p.Value));orders=db.Orders.Select(o=>Copy(o)).ToList();cart=db.Cart.Select(c=>Copy(c)).ToList();}
        public Task CommitAsync(CancellationToken ct){committed=true;return Task.CompletedTask;}
        public ValueTask DisposeAsync(){if(!committed){db.Products=products;db.Orders=orders;db.Cart=cart;}return ValueTask.CompletedTask;}
    }
}
