namespace ErsatzTV.Core.Hdhr;

public record DeviceXml(string Scheme, string Host)
{
    public string ToXml() =>
        @$"<root xmlns=""urn:schemas-upnp-org:device-1-0"">
    <URLBase>{Scheme}://{Host}</URLBase>
    <specVersion>
        <major>1</major>
        <minor>0</minor>
    </specVersion>
    <device>
        <deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>
        <friendlyName>ErsatzTV</friendlyName>
        <manufacturer>Silicondust</manufacturer>
        <modelName>HDTC-2US</modelName>
        <modelNumber>HDTC-2US</modelNumber>
        <serialNumber/>
        <UDN>uuid:2020-03-S3LA-BG3LIA:2</UDN>
    </device>
</root>";
}