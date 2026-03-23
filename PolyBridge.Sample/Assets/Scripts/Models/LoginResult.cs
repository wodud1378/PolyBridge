using System;

[Serializable]
public struct LoginResult
{
    public string userId;
    public string displayName;
    public string token;
    public long expiresIn;
}
