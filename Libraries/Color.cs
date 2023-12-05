using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DojoArenaKT;

public static class Color
{
    /*private static string ColorText(string color, object text)
    {
        return $"<color={color}>" + text.ToString() + "</color>";
    }*/

    public static string ColorString(string input)
    {
        return $"<color={input}>";
    }

    public static string ProcessMessage(string input)
    {
        StringBuilder sb = new();
        var split = input.Split(Color.Clear);
        for (int i=0; i<split.Length; i++)
        {
            var curSlice = split[i];
            sb.Append(curSlice);
            string currentString = sb.ToString();
            int missingCloseTags = Regex.Matches(currentString, "<color=.*?>").Count - Regex.Matches(currentString, "<\\/color>").Count;
            if (missingCloseTags > 0)
            {
                for (int j=0; j<missingCloseTags; j++)
                {
                    sb.Append(Color.CloseTag);
                }
            }
        }
        return sb.ToString();
    }
    public static string White = ColorString("#ffffffff");
    public static string Black = ColorString("#000000ff");
    public static string Gray = ColorString("#404040ff");
    public static string Orange = ColorString("#c98332ff");
    public static string Yellow = ColorString("#e7ed74");
    public static string Green = ColorString("#56ad3bff");
    public static string Teal = ColorString("#3b8dadff");
    public static string Blue = ColorString("#3444a8ff");
    public static string Purple = ColorString("#8b3691ff");
    public static string Pink = ColorString("#b53c8fff");
    public static string Red = ColorString("#ff0000ff");
    public static string SoftRed = ColorString("#b53c40ff");
    public static string Clear = "<clear>";
    public static string CloseTag = "</color>";
}