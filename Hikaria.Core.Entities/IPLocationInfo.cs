using System.Net;

namespace Hikaria.Core.Entities;

public class IPLocationInfo
{
    public string Organization { get; set; }
    public float Longitude { get; set; }
    public string City { get; set; }
    public string Timezone { get; set; }
    public string ISP { get; set; }
    public int Offset { get; set; }
    public string Region { get; set; }
    public int ASN { get; set; }
    public string ASN_Organization { get; set; }
    public string Country { get; set; }
    public string IP { get; set; }
    public float Latitude { get; set; }
    public string Continent_Code { get; set; }
    public string Country_Code { get; set; }
    public string Region_Code { get; set; }
}
