using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public static class WowGlobalStringText
{
    public static string DecodeDatabaseEscapes(string value)
    {
        if (value.IndexOf('\\') < 0)
            return value;

        var decoded = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character != '\\' || index + 1 >= value.Length)
            {
                decoded.Append(character);
                continue;
            }

            var escaped = value[++index];
            if (escaped is >= '0' and <= '9')
            {
                var decimalValue = escaped - '0';
                var digitCount = 1;
                while (digitCount < 3 &&
                       index + 1 < value.Length &&
                       value[index + 1] is >= '0' and <= '9')
                {
                    decimalValue = decimalValue * 10 + value[++index] - '0';
                    digitCount++;
                }

                if (decimalValue <= byte.MaxValue)
                    decoded.Append((char)decimalValue);
                else
                    decoded.Append('\\').Append(value, index - digitCount + 1, digitCount);
                continue;
            }

            decoded.Append(escaped switch
            {
                'a' => '\a',
                'b' => '\b',
                'f' => '\f',
                'n' or 'r' => '\n',
                't' => '\t',
                'v' => '\v',
                '\\' => '\\',
                '\'' => '\'',
                '"' => '"',
                _ => '\\'
            });
            if (escaped is not ('a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' or '\\' or '\'' or '"'))
                decoded.Append(escaped);
        }
        return decoded.ToString();
    }
}
