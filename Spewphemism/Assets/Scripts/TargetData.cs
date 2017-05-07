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

    string responsePattern;
    string regexPattern;

    public void SetPattern()
    {
        // Response pattern
        StringBuilder resp = new StringBuilder();
        resp.Append(name.ToLower());

        if (!string.IsNullOrEmpty(responses))
        {
            resp.Append("|");
            resp.Append(responses.ToLower());
        }

        responsePattern = resp.ToString();

        // Disallowed pattern
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
        bool result = Regex.IsMatch(clue.ToLower(), regexPattern);

        return !result;
    }
    public bool IsGuessValid(string guess)
    {
        Debug.Log("Checking guess " + guess + " against pattern: " + responsePattern);
        bool result = Regex.IsMatch(guess.ToLower(), responsePattern);
        return result;
    }

    public string FormatDisallowedWords()
    {
        return disallowedWords.Replace("|", ", ");
    }
}
