namespace ReqResClient.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Avatar { get; set; } = null!;
}
