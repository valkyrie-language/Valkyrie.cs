namespace Asgard.CLI;

public sealed class VoaServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3000;
    public int Workers { get; set; } = 0;
    public int Timeout { get; set; } = 30;
}