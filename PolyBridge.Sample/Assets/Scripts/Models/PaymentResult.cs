using System;

[Serializable]
public struct PaymentResult
{
    public string transactionId;
    public string productId;
    public int amount;
    public string currency;
    public string status;
}
