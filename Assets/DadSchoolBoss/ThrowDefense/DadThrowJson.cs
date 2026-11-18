using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Small JSON reader for the throw prototype's own data files (art manifests, stage tables).
// It runs without the engine, so the window-free checks read the very files the game reads, and a broken
// file is refused with the place it broke instead of loading as zeros.
// Values come back as Dictionary<string,object>, List<object>, string, double, bool or null.
public static class DadThrowJson
{
    public static object Parse(string text)
    {
        if(text==null)throw new ArgumentException("JSON text is missing");
        int at=0;object value=Value(text,ref at);Space(text,ref at);
        if(at!=text.Length)throw Bad(at,"has text after the end");
        return value;
    }
    static ArgumentException Bad(int at,string what){return new ArgumentException("JSON "+what+" (character "+at+")");}
    static void Space(string s,ref int at){while(at<s.Length&&char.IsWhiteSpace(s[at]))at++;}
    static bool Word(string s,ref int at,string word)
    {if(string.CompareOrdinal(s,at,word,0,word.Length)!=0)return false;at+=word.Length;return true;}
    static object Value(string s,ref int at)
    {
        Space(s,ref at);if(at>=s.Length)throw Bad(at,"ends early");
        char c=s[at];
        if(c=='{')
        {
            var map=new Dictionary<string,object>();at++;Space(s,ref at);
            if(at<s.Length&&s[at]=='}'){at++;return map;}
            while(true)
            {
                Space(s,ref at);if(at>=s.Length||s[at]!='"')throw Bad(at,"needs a key");
                string key=Quoted(s,ref at);Space(s,ref at);
                if(at>=s.Length||s[at]!=':')throw Bad(at,"needs ':' after "+key);
                at++;
                // A repeated key would silently keep one of two values; refuse it.
                if(map.ContainsKey(key))throw Bad(at,"repeats the key "+key);
                map[key]=Value(s,ref at);Space(s,ref at);
                if(at<s.Length&&s[at]==','){at++;continue;}
                if(at<s.Length&&s[at]=='}'){at++;return map;}
                throw Bad(at,"needs ',' or '}'");
            }
        }
        if(c=='[')
        {
            var list=new List<object>();at++;Space(s,ref at);
            if(at<s.Length&&s[at]==']'){at++;return list;}
            while(true)
            {
                list.Add(Value(s,ref at));Space(s,ref at);
                if(at<s.Length&&s[at]==','){at++;continue;}
                if(at<s.Length&&s[at]==']'){at++;return list;}
                throw Bad(at,"needs ',' or ']'");
            }
        }
        if(c=='"')return Quoted(s,ref at);
        if(Word(s,ref at,"true"))return true;
        if(Word(s,ref at,"false"))return false;
        if(Word(s,ref at,"null"))return null;
        int from=at;while(at<s.Length&&"+-0123456789.eE".IndexOf(s[at])>=0)at++;
        double number;
        if(at==from||!double.TryParse(s.Substring(from,at-from),NumberStyles.Float,CultureInfo.InvariantCulture,out number)||double.IsNaN(number)||double.IsInfinity(number))
            throw Bad(from,"has an unreadable value");
        return number;
    }
    static string Quoted(string s,ref int at)
    {
        var text=new StringBuilder();at++;
        while(true)
        {
            if(at>=s.Length)throw Bad(at,"has an unclosed string");
            char c=s[at++];
            if(c=='"')return text.ToString();
            if(c!='\\'){text.Append(c);continue;}
            if(at>=s.Length)throw Bad(at,"has an unclosed string");
            char e=s[at++];
            switch(e)
            {
                case 'n':text.Append('\n');break;case 't':text.Append('\t');break;case 'r':text.Append('\r');break;
                case 'b':text.Append('\b');break;case 'f':text.Append('\f');break;
                case 'u':
                    int code;
                    if(at+4>s.Length||!int.TryParse(s.Substring(at,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out code))throw Bad(at,"has a bad \\u escape");
                    text.Append((char)code);at+=4;break;
                default:text.Append(e);break;   // \" \\ \/
            }
        }
    }

    // ── typed access; `owner` names the thing being read so the message says which entry is wrong ──
    public static Dictionary<string,object> Map(object value,string owner)
    {var map=value as Dictionary<string,object>;if(map==null)throw new ArgumentException(owner+" must be an object");return map;}
    public static List<object> List(object value,string owner)
    {var list=value as List<object>;if(list==null)throw new ArgumentException(owner+" must be a list");return list;}
    public static bool Has(Dictionary<string,object> map,string key){object value;return map.TryGetValue(key,out value)&&value!=null;}
    public static float Number(Dictionary<string,object> map,string key,string owner)
    {
        object value;if(!map.TryGetValue(key,out value)||!(value is double))throw new ArgumentException(owner+" needs the number "+key);
        return (float)(double)value;
    }
    public static float NumberOr(Dictionary<string,object> map,string key,float fallback,string owner)
    {return Has(map,key)?Number(map,key,owner):fallback;}
    public static int Whole(Dictionary<string,object> map,string key,string owner)
    {
        float value=Number(map,key,owner);
        if(value!=(float)Math.Floor(value))throw new ArgumentException(owner+" needs a whole number for "+key);
        return (int)value;
    }
    public static string Text(Dictionary<string,object> map,string key,string owner)
    {
        object value;if(!map.TryGetValue(key,out value)||!(value is string)||((string)value).Length==0)throw new ArgumentException(owner+" needs the text "+key);
        return (string)value;
    }
    public static string TextOr(Dictionary<string,object> map,string key,string fallback,string owner)
    {return Has(map,key)?Text(map,key,owner):fallback;}
}
