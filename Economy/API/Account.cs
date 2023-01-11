namespace Economy.API;

public class Account
{
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerType { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    private Account() { }
    
    public Account(string ownerId, string ownerType, decimal balance)
    {
        OwnerId = ownerId;
        OwnerType = ownerType;
        Balance = balance;
    }
}