namespace ZKTecoWindowsService.Models.Options;

public sealed class SmsSettings
{
    public string ApiUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Sender { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public bool UseTurkish { get; set; } = true;
}
