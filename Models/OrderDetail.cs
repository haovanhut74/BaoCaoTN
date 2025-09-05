namespace MyWebApp.Models;

public class OrderDetail
{
    public Guid OrderDetailId { get; set; }
    public Guid OrderId { get; set; }  
    public string OrderCode { get; set; }
    public string UserName { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public Product Product { get; set; }

}