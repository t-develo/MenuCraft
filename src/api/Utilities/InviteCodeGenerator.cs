#nullable enable

using System.Security.Cryptography;

namespace MenuCraft.Api.Utilities;

public static class InviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    public static string Generate()
    {
        var bytes = new byte[CodeLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var code = new char[CodeLength];
        for (var i = 0; i < code.Length; i++)
        {
            code[i] = Chars[bytes[i] % Chars.Length];
        }

        return new string(code);
    }
}
