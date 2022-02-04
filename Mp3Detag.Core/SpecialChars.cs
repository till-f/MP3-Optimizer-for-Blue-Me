using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Mp3Detag.Core
{
  public static class SpecialChars
  {
    private static readonly string InvalidFileChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

    // ,.-;:_#'+*~!"§$%&/()=?`´\}][{
    private static readonly string InvalidBlueAndMeChars = "%()[]{}='\"`´";

    private static readonly Dictionary<string, string> RegExReplacements = new()
    {
      { "([^ ])&([^ ])", "$1 and $2" },
      { "([^ ])& ", "$1 and " },
      { " &([^ ])", " and $1" },
      { " & ", " and " },
      { "([^ ])\\$", "$1 Dollar" },
      { " \\$", " Dollar" },
    };

    private static readonly Dictionary<string, string> SpecialCharsMap = new()
    {
        { "äæǽ", "ae" },
        { "öœ", "oe" },
        { "ü", "ue" },
        { "Ä", "Ae" },
        { "Ü", "Ue" },
        { "Ö", "Oe" },
        { "ÀÁÂÃÄÅǺĀĂĄǍΑΆẢẠẦẪẨẬẰẮẴẲẶА", "A" },
        { "àáâãåǻāăąǎªαάảạầấẫẩậằắẵẳặа", "a" },
        { "Б", "B" },
        { "б", "b" },
        { "ÇĆĈĊČ", "C" },
        { "çćĉċč", "c" },
        { "Д", "D" },
        { "д", "d" },
        { "ÐĎĐΔ", "Dj" },
        { "ðďđδ", "dj" },
        { "ÈÉÊËĒĔĖĘĚΕΈẼẺẸỀẾỄỂỆЕЭ", "E" },
        { "èéêëēĕėęěέεẽẻẹềếễểệеэ", "e" },
        { "Ф", "F" },
        { "ф", "f" },
        { "ĜĞĠĢΓГҐ", "G" },
        { "ĝğġģγгґ", "g" },
        { "ĤĦ", "H" },
        { "ĥħ", "h" },
        { "ÌÍÎÏĨĪĬǏĮİΗΉΊΙΪỈỊИЫ", "I" },
        { "ìíîïĩīĭǐįıηήίιϊỉịиыї", "i" },
        { "Ĵ", "J" },
        { "ĵ", "j" },
        { "ĶΚК", "K" },
        { "ķκк", "k" },
        { "ĹĻĽĿŁΛЛ", "L" },
        { "ĺļľŀłλл", "l" },
        { "М", "M" },
        { "м", "m" },
        { "ÑŃŅŇΝН", "N" },
        { "ñńņňŉνн", "n" },
        { "ÒÓÔÕŌŎǑŐƠØǾΟΌΩΏỎỌỒỐỖỔỘỜỚỠỞỢО", "O" },
        { "òóôõōŏǒőơøǿºοόωώỏọồốỗổộờớỡởợо", "o" },
        { "П", "P" },
        { "п", "p" },
        { "ŔŖŘΡР", "R" },
        { "ŕŗřρр", "r" },
        { "ŚŜŞȘŠΣС", "S" },
        { "śŝşșšſσςс", "s" },
        { "ȚŢŤŦτТ", "T" },
        { "țţťŧт", "t" },
        { "ÙÚÛŨŪŬŮŰŲƯǓǕǗǙǛŨỦỤỪỨỮỬỰУ", "U" },
        { "ùúûũūŭůűųưǔǖǘǚǜυύϋủụừứữửựу", "u" },
        { "ÝŸŶΥΎΫỲỸỶỴЙ", "Y" },
        { "ýÿŷỳỹỷỵй", "y" },
        { "В", "V" },
        { "в", "v" },
        { "Ŵ", "W" },
        { "ŵ", "w" },
        { "ŹŻŽΖЗ", "Z" },
        { "źżžζз", "z" },
        { "ÆǼ", "AE" },
        { "ß", "ss" },
        { "Ĳ", "IJ" },
        { "ĳ", "ij" },
        { "Œ", "OE" },
        { "ƒ", "f" },
        { "ξ", "ks" },
        { "π", "p" },
        { "β", "v" },
        { "μ", "m" },
        { "ψ", "ps" },
        { "Ё", "Yo" },
        { "ё", "yo" },
        { "Є", "Ye" },
        { "є", "ye" },
        { "Ї", "Yi" },
        { "Ж", "Zh" },
        { "ж", "zh" },
        { "Х", "Kh" },
        { "х", "kh" },
        { "Ц", "Ts" },
        { "ц", "ts" },
        { "Ч", "Ch" },
        { "ч", "ch" },
        { "Ш", "Sh" },
        { "ш", "sh" },
        { "Щ", "Shch" },
        { "щ", "shch" },
        { "ЪъЬь", "" },
        { "Ю", "Yu" },
        { "ю", "yu" },
        { "Я", "Ya" },
        { "я", "ya" },

        { "\\", "/" },
    };

    public static string SanitizeByMap(this string s)
    {
      StringBuilder textBuilder = new StringBuilder();

      foreach (char c in s)
      {
        bool isCharReplaced = false;
        foreach (KeyValuePair<string, string> entry in SpecialCharsMap)
        {
          if (entry.Key.IndexOf(c) != -1)
          {
            textBuilder.Append(entry.Value);
            isCharReplaced = true;
            break;
          }
        }

        if (!isCharReplaced)
        {
          textBuilder.Append(c);
        }
      }
      return textBuilder.ToString();
    }

    public static string SanitizeByRegex(this string s)
    {
      foreach (var kvp in RegExReplacements)
      {
        s = Regex.Replace(s, kvp.Key, kvp.Value);
      }

      return s;
    }

    public static string SanitizeByEncoding(this string s)
    {
      var tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(s);
      return Encoding.UTF8.GetString(tempBytes);
    }

    public static string RemoveInvalidBlueAndMeChars1(this string s)
    {
      var textBuilder = new StringBuilder();

      foreach (char c in s)
      {
        if (c > 0x1f && c < 0x7f)
        {
          textBuilder.Append(c);
        }
      }
      return textBuilder.ToString();
    }

    public static string RemoveInvalidBlueAndMeChars2(this string s)
    {
      var textBuilder = new StringBuilder();

      foreach (char c in s)
      {
        if (!InvalidBlueAndMeChars.Contains(c))
        {
          textBuilder.Append(c);
        }
      }

      return textBuilder.ToString();
    }

    public static string RemoveInvalidFileNameChars(this string s)
    {
      var textBuilder = new StringBuilder();

      foreach (char c in s)
      {
        if (!InvalidFileChars.Contains(c))
        {
          textBuilder.Append(c);
        }
      }

      return textBuilder.ToString();
    }
  }
}
