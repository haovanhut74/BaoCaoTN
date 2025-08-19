using Newtonsoft.Json;

namespace MyWebApp.Models;


public class WardResponse
{
    [JsonProperty("code")] public int Code { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("division_type")] public string DivisionType { get; set; }
    [JsonProperty("codename")] public string Codename { get; set; }
    [JsonProperty("district_code")] public int DistrictCode { get; set; }
}