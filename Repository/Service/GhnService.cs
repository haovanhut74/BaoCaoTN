using System.Text;
using Microsoft.Extensions.Options;
using MyWebApp.Interface.Service;
using MyWebApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyWebApp.Repository.Service;

public class GhnService : IGhnService
{
    private readonly HttpClient _httpClient;
    private readonly GhnConfig _config;

    public GhnService(HttpClient httpClient, IOptions<GhnConfig> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<string> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"{_config.BaseUrl}/shipping-order/create";
        var itemss = order.OrderDetails.Select(d => new
        {
            name = d.Product.Name,
            quantity = d.Quantity,
            price = (int)d.Price // GHN yêu cầu int (VNĐ)
        }).ToList();
        var payload = new
        {
            shop_id = _config.ShopId,
            from_name = "MNStore",
            from_phone = "0972806362",
            from_address = "Phú Lợi, Thủ Dầu Một, Bình Dương",
            from_ward_name = "Phú Lợi",
            from_district_name = "Thủ Dầu Một",
            from_province_name = "Bình Dương",

            // thông tin người nhận
            to_name = order.UserName,
            to_phone = order.PhoneNumber,
            to_address = "Phú Lợi, Thủ Dầu Một, Bình Dương",
            to_ward_name = "Phú Lợi",
            to_district_name = "Thủ Dầu Một",
            to_province_name = "Bình Dương",

            weight = 500, // gram
            length = 20,
            width = 10,
            height = 10,
            service_type_id = 2,
            payment_type_id = 2, // người nhận trả phí ship
            note = "Đơn hàng từ website",
            // BẮT BUỘC
            required_note = "KHONGCHOXEMHANG",
            items = itemss
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("Token", _config.Token);
        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        var json = JObject.Parse(result);
        return json["data"]?["order_code"]?.ToString();
    }
}