namespace MyWebApp.Models;

public class Order
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; }
    public string UserName { get; set; }
    public DateTime OrderDate { get; set; } 
    public int Status { get; set; }
}