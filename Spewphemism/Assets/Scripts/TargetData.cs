using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;

public class TargetData
{
    public string id;
    public string name;
    public string category;
    public string responses;
    public string disallowedWords;
    public string disallowedRegex;
    public int maxCharacters;

    string regexPattern;

    public void SetPattern()
    {
        StringBuilder sb = new StringBuilder();
        string norm = name.ToLower();
        sb.Append(norm);
        sb.Append("|");

        for (int i = 0; i < norm.Length; ++i)
        {
            sb.Append(norm[i]);

            if (i < norm.Length - 1)
                sb.AppendFormat("\\s*");
        }

        if (string.IsNullOrEmpty(disallowedRegex))
        {
            if (!string.IsNullOrEmpty(disallowedWords))
            {
                sb.Append("|");
                sb.Append(disallowedWords);
            }
        }
        else
        {
            sb.Append("|");
            sb.Append(disallowedRegex);
        }
        regexPattern = sb.ToString();
    }
    public bool IsClueValid(string clue)
    {
        Debug.Log("Checking clue " + clue + " against pattern: " + regexPattern);
        bool result = Regex.IsMatch(clue, regexPattern);

        return !result;
    }
}
