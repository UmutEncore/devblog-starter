namespace DevBlog.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // TODO: proper hashing needed
    public string Role { get; set; } = "Author";
}
