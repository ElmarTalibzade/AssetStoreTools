using System;
using System.Collections.Generic;
using System.Globalization;

internal class JSONParser
{
    private string json;

    private int line;

    private int linechar;

    private int len;

    private int idx;

    private int pctParsed;

    private char cur;

    private static char[] endcodes;

    static JSONParser()
    {
        JSONParser.endcodes = new char[] {'\\', '\"'};
    }

    public JSONParser(string jsondata)
    {
        this.json = string.Concat(jsondata, "    ");
        this.line = 1;
        this.linechar = 1;
        this.len = this.json.Length;
        this.idx = 0;
        this.pctParsed = 0;
    }

    private char Next()
    {
        if (this.cur == '\n')
        {
            this.line++;
            this.linechar = 0;
        }

        this.idx++;
        if (this.idx >= this.len)
        {
            throw new JSONParseException(string.Concat("End of json while parsing at ", this.PosMsg()));
        }

        this.linechar++;
        int num = (int) ((float) this.idx * 100f / (float) this.len);
        if (num != this.pctParsed)
        {
            this.pctParsed = num;
        }

        this.cur = this.json[this.idx];
        return this.cur;
    }

    public JSONValue Parse()
    {
        this.cur = this.json[this.idx];
        return this.ParseValue();
    }

    private JSONValue ParseArray()
    {
        this.Next();
        this.SkipWs();
        List<JSONValue> jSONValues = new List<JSONValue>();
        while (this.cur != ']')
        {
            jSONValues.Add(this.ParseValue());
            this.SkipWs();
            if (this.cur != ',')
            {
                continue;
            }

            this.Next();
            this.SkipWs();
        }

        this.Next();
        return new JSONValue(jSONValues);
    }

    private JSONValue ParseConstant()
    {
        string str = string.Concat(new object[] {string.Empty, this.cur, this.Next(), this.Next(), this.Next()});
        this.Next();
        if (str == "true")
        {
            return new JSONValue(true);
        }

        if (str == "fals")
        {
            if (this.cur == 'e')
            {
                this.Next();
                return new JSONValue(false);
            }
        }
        else if (str == "null")
        {
            return new JSONValue(null);
        }

        throw new JSONParseException(string.Concat("Invalid token at ", this.PosMsg()));
    }

    private JSONValue ParseDict()
    {
        this.Next();
        this.SkipWs();
        Dictionary<string, JSONValue> strs = new Dictionary<string, JSONValue>();
        while (this.cur != '}')
        {
            JSONValue jSONValue = this.ParseValue();
            if (!jSONValue.IsString())
            {
                throw new JSONParseException(string.Concat("Key not string type at ", this.PosMsg()));
            }

            this.SkipWs();
            if (this.cur != ':')
            {
                throw new JSONParseException(string.Concat("Missing dict entry delimiter ':' at ", this.PosMsg()));
            }

            this.Next();
            strs.Add(jSONValue.AsString(false), this.ParseValue());
            this.SkipWs();
            if (this.cur != ',')
            {
                continue;
            }

            this.Next();
            this.SkipWs();
        }

        this.Next();
        return new JSONValue(strs);
    }

    private JSONValue ParseNumber()
    {
        JSONValue jSONValue;
        string empty = string.Empty;
        if (this.cur == '-')
        {
            empty = "-";
            this.Next();
        }

        while (this.cur >= '0' && this.cur <= '9')
        {
            empty = string.Concat(empty, this.cur);
            this.Next();
        }

        if (this.cur == '.')
        {
            this.Next();
            empty = string.Concat(empty, '.');
            while (this.cur >= '0' && this.cur <= '9')
            {
                empty = string.Concat(empty, this.cur);
                this.Next();
            }
        }

        if (this.cur == 'e' || this.cur == 'E')
        {
            empty = string.Concat(empty, "e");
            this.Next();
            if (this.cur != '-' && this.cur != '+')
            {
                empty = string.Concat(empty, this.cur);
                this.Next();
            }

            while (this.cur >= '0' && this.cur <= '9')
            {
                empty = string.Concat(empty, this.cur);
                this.Next();
            }
        }

        try
        {
            jSONValue = new JSONValue((object) Convert.ToSingle(empty));
        }
        catch (Exception exception)
        {
            throw new JSONParseException(string.Concat("Cannot convert string to float : '", empty, "' at ", this.PosMsg()));
        }

        return jSONValue;
    }

    private JSONValue ParseString()
    {
        string empty = string.Empty;
        this.Next();
        while (this.idx < this.len)
        {
            int num = this.json.IndexOfAny(JSONParser.endcodes, this.idx);
            if (num < 0)
            {
                throw new JSONParseException(string.Concat("missing '\"' to end string at ", this.PosMsg()));
            }

            empty = string.Concat(empty, this.json.Substring(this.idx, num - this.idx));
            if (this.json[num] != '\"')
            {
                num++;
                if (num >= this.len)
                {
                    throw new JSONParseException(string.Concat("End of json while parsing while parsing string at ", this.PosMsg()));
                }

                char chr = this.json[num];
                char chr1 = chr;
                switch (chr1)
                {
                    case 'n':
                    {
                        empty = string.Concat(empty, '\n');
                        break;
                    }

                    case 'r':
                    {
                        empty = string.Concat(empty, '\r');
                        break;
                    }

                    case 't':
                    {
                        empty = string.Concat(empty, '\t');
                        break;
                    }

                    case 'u':
                    {
                        string str = string.Empty;
                        if (num + 4 >= this.len)
                        {
                            throw new JSONParseException(string.Concat("End of json while parsing while parsing unicode char near ", this.PosMsg()));
                        }

                        str = string.Concat(str, this.json[num + 1]);
                        str = string.Concat(str, this.json[num + 2]);
                        str = string.Concat(str, this.json[num + 3]);
                        str = string.Concat(str, this.json[num + 4]);
                        try
                        {
                            int num1 = int.Parse(str, NumberStyles.AllowHexSpecifier);
                            empty = string.Concat(empty, (char) num1);
                        }
                        catch (FormatException formatException)
                        {
                            throw new JSONParseException(string.Concat("Invalid unicode escape char near ", this.PosMsg()));
                        }

                        num += 4;
                        break;
                    }

                    default:
                    {
                        if (chr1 != '\"')
                        {
                            if (chr1 != '/')
                            {
                                if (chr1 != '\\')
                                {
                                    if (chr1 == 'b')
                                    {
                                        empty = string.Concat(empty, '\b');
                                        break;
                                    }
                                    else
                                    {
                                        if (chr1 != 'f')
                                        {
                                            throw new JSONParseException(string.Concat(new object[] {"Invalid escape char '", chr, "' near ", this.PosMsg()}));
                                        }

                                        empty = string.Concat(empty, '\f');
                                        break;
                                    }
                                }
                            }
                        }

                        empty = string.Concat(empty, chr);
                        break;
                    }
                }

                this.idx = num + 1;
            }
            else
            {
                this.cur = this.json[num];
                this.idx = num;
                break;
            }
        }

        if (this.idx >= this.len)
        {
            throw new JSONParseException(string.Concat("End of json while parsing while parsing string near ", this.PosMsg()));
        }

        this.cur = this.json[this.idx];
        this.Next();
        return new JSONValue(empty);
    }

    private JSONValue ParseValue()
    {
        this.SkipWs();
        char chr = this.cur;
        switch (chr)
        {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            {
                return this.ParseNumber();
            }

            default:
            {
                if (chr == '\"')
                {
                    break;
                }
                else
                {
                    if (chr == '[')
                    {
                        return this.ParseArray();
                    }

                    if (chr == 'f' || chr == 'n' || chr == 't')
                    {
                        return this.ParseConstant();
                    }

                    if (chr != '{')
                    {
                        throw new JSONParseException(string.Concat("Cannot parse json value starting with '", this.json.Substring(this.idx, 5), "' at ", this.PosMsg()));
                    }

                    return this.ParseDict();
                }
            }
        }

        return this.ParseString();
    }

    private string PosMsg()
    {
        return string.Concat("line ", this.line.ToString(), ", column ", this.linechar.ToString());
    }

    public static JSONValue SimpleParse(string jsondata)
    {
        JSONValue jSONValue;
        JSONParser jSONParser = new JSONParser(jsondata);
        try
        {
            jSONValue = jSONParser.Parse();
        }
        catch (JSONParseException jSONParseException)
        {
            Console.WriteLine(jSONParseException.Message);
            return new JSONValue(null);
        }

        return jSONValue;
    }

    private void SkipWs()
    {
        while (" \n\t\r".IndexOf(this.cur) != -1)
        {
            this.Next();
        }
    }
}