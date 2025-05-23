namespace Ecommerce.Application.Configuration;

public class GlobalLimitOptions
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
}
