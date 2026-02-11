using System.Runtime.InteropServices;
using System.Security;

namespace QRiskTreeEditor.SecondaryWindows
{
    internal static class PasswordUtils
    {
        public static int GetScore(this SecureString password)
        {
            if (password == null || password.Length == 0)
                return 0;

            int result;

            var len = password.Length;
            var category = new List<CharacterCategory>();

            // Convert SecureString to plain text temporarily for analysis
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(password);

                for (int i = 0; i < len; i++)
                {
                    char c = (char)Marshal.ReadInt16(unmanagedString, i * 2);
                    category.Add(CategorizeCharacter(c));
                }
            }
            finally
            {
                // Zero out and free unmanaged memory
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }

            int count = category.Count;

            if (count < 4)
            {
                result = 5;
            }
            else if (count <= 6)
            {
                result = 10;
            }
            else
            {
                var score = Math.DivRem(count, 4, out var remainder) - 1;

                var digit = category.Count(x => x == CharacterCategory.Digit);
                var lowercase = category.Count(x => x == CharacterCategory.AlphaLowercase);
                var uppercase = category.Count(x => x == CharacterCategory.AlphaUppercase);
                var common = category.Count(x => x == CharacterCategory.CommonSymbol);
                var other = category.Count(x => x == CharacterCategory.Other);

                if (digit > 0)
                    score++;
                if (digit > 3)
                    score++;
                if (lowercase > 0)
                    score++;
                if (lowercase > 3)
                    score++;
                if (uppercase > 0)
                    score++;
                if (uppercase > 3)
                    score++;
                if (common > 0)
                    score++;
                if (common > 3)
                    score++;
                if (other > 0)
                    score++;
                if (other > 3)
                    score++;

                if (score < 4)
                    result = 10;
                else if (score < 5)
                    result = 25;
                else if (score < 6)
                    result = 50;
                else if (score < 8)
                    result = 75;
                else if (score < 10)
                    result = 90;
                else
                    result = 100;
            }

            return result;
        }

        private static CharacterCategory CategorizeCharacter(char c)
        {
            if (char.IsDigit(c))
                return CharacterCategory.Digit;
            if (char.IsLower(c))
                return CharacterCategory.AlphaLowercase;
            if (char.IsUpper(c))
                return CharacterCategory.AlphaUppercase;
            if ("!@#$%^&*()-_=+[]{}|;:,.<>?/~`".Contains(c))
                return CharacterCategory.CommonSymbol;

            return CharacterCategory.Other;
        }
    }
}
