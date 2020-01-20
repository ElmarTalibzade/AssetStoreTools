using System;
using System.Collections.Generic;
using System.Reflection;

internal struct JSONValue
{
    private object data;

    public JSONValue this[string index]
    {
        get { return this.AsDict(false)[index]; }
        set
        {
            if (this.data == null)
            {
                this.data = new Dictionary<string, JSONValue>();
            }

            this.AsDict(false)[index] = value;
        }
    }

    public JSONValue(object o)
    {
        this.data = o;
    }

    public void Add(string value)
    {
        List<JSONValue> jSONValues = this.AsList(false);
        if (value == null)
        {
            jSONValues.Add(JSONValue.NewNull());
            return;
        }

        jSONValues.Add(JSONValue.NewString(value));
    }

    public void Add(float value)
    {
        this.AsList(false).Add(JSONValue.NewFloat(value));
    }

    public void Add(bool value)
    {
        this.AsList(false).Add(JSONValue.NewBool(value));
    }

    public bool AsBool(bool nothrow = false)
    {
        if (this.data is bool)
        {
            return (bool) this.data;
        }

        if (!nothrow)
        {
            throw new JSONTypeException("Tried to read non-bool json value as bool");
        }

        return false;
    }

    public Dictionary<string, JSONValue> AsDict(bool nothrow = false)
    {
        if (this.data is Dictionary<string, JSONValue>)
        {
            return (Dictionary<string, JSONValue>) this.data;
        }

        if (!nothrow)
        {
            throw new JSONTypeException("Tried to read non-dictionary json value as dictionary");
        }

        return null;
    }

    public float AsFloat(bool nothrow = false)
    {
        if (this.data is float)
        {
            return (float) this.data;
        }

        if (!nothrow)
        {
            throw new JSONTypeException("Tried to read non-float json value as float");
        }

        return 0f;
    }

    public List<JSONValue> AsList(bool nothrow = false)
    {
        if (this.data is List<JSONValue>)
        {
            return (List<JSONValue>) this.data;
        }

        if (!nothrow)
        {
            throw new JSONTypeException(string.Concat("Tried to read ", this.data.GetType().Name, " json value as list"));
        }

        return null;
    }

    public string AsString(bool nothrow = false)
    {
        if (this.data is string)
        {
            return (string) this.data;
        }

        if (!nothrow)
        {
            throw new JSONTypeException("Tried to read non-string json value as string");
        }

        return string.Empty;
    }

    public bool ContainsKey(string index)
    {
        if (!this.IsDict())
        {
            return false;
        }

        return this.AsDict(false).ContainsKey(index);
    }

    public bool Copy(string key, ref string dest)
    {
        return this.Copy(key, ref dest, true);
    }

    public bool Copy(string key, ref string dest, bool allowCopyNull)
    {
        bool flag;
        string str;
        JSONValue jSONValue = this.Get(key, out flag);
        if (flag && (!jSONValue.IsNull() || allowCopyNull))
        {
            if (!jSONValue.IsNull())
            {
                str = jSONValue.AsString(false);
            }
            else
            {
                str = null;
            }

            dest = str;
        }

        return flag;
    }

    public bool Copy(string key, ref bool dest)
    {
        bool flag;
        JSONValue jSONValue = this.Get(key, out flag);
        if (flag && !jSONValue.IsNull())
        {
            dest = jSONValue.AsBool(false);
        }

        return flag;
    }

    public bool Copy(string key, ref int dest)
    {
        bool flag;
        JSONValue jSONValue = this.Get(key, out flag);
        if (flag && !jSONValue.IsNull())
        {
            dest = (int) jSONValue.AsFloat(false);
        }

        return flag;
    }

    private static string EncodeString(string str)
    {
        str = str.Replace("\\", "\\\\");
        str = str.Replace("\"", "\\\"");
        str = str.Replace("/", "\\/");
        str = str.Replace("\b", "\\b");
        str = str.Replace("\f", "\\f");
        str = str.Replace("\n", "\\n");
        str = str.Replace("\r", "\\r");
        str = str.Replace("\t", "\\t");
        return str;
    }

    public JSONValue Get(string key, out bool found)
    {
        found = false;
        if (!this.IsDict())
        {
            return new JSONValue(null);
        }

        JSONValue item = this;
        string[] strArrays = key.Split(new char[] {'.'});
        for (int i = 0; i < (int) strArrays.Length; i++)
        {
            string str = strArrays[i];
            if (!item.ContainsKey(str))
            {
                return new JSONValue(null);
            }

            item = item[str];
        }

        found = true;
        return item;
    }

    public JSONValue Get(string key)
    {
        bool flag;
        return this.Get(key, out flag);
    }

    public JSONValue InitDict()
    {
        this.data = new Dictionary<string, JSONValue>();
        return this;
    }

    public JSONValue InitList()
    {
        this.data = new List<JSONValue>();
        return this;
    }

    public bool IsBool()
    {
        return this.data is bool;
    }

    public bool IsDict()
    {
        return this.data is Dictionary<string, JSONValue>;
    }

    public bool IsFloat()
    {
        return this.data is float;
    }

    public bool IsList()
    {
        return this.data is List<JSONValue>;
    }

    public bool IsNull()
    {
        return this.data == null;
    }

    public bool IsString()
    {
        return this.data is string;
    }

    public static JSONValue NewBool(bool val)
    {
        return new JSONValue((object) val);
    }

    public static JSONValue NewDict()
    {
        return new JSONValue(new Dictionary<string, JSONValue>());
    }

    public static JSONValue NewFloat(float val)
    {
        return new JSONValue((object) val);
    }

    public static JSONValue NewList()
    {
        return new JSONValue(new List<JSONValue>());
    }

    public static JSONValue NewNull()
    {
        return new JSONValue(null);
    }

    public static JSONValue NewString(string val)
    {
        return new JSONValue(val);
    }

    public static implicit operator JSONValue(string s)
    {
        return new JSONValue(s);
    }

    public static implicit operator String(JSONValue s)
    {
        return s.AsString(false);
    }

    public static implicit operator JSONValue(float s)
    {
        return new JSONValue((object) s);
    }

    public static implicit operator Single(JSONValue s)
    {
        return s.AsFloat(false);
    }

    public static implicit operator JSONValue(bool s)
    {
        return new JSONValue((object) s);
    }

    public static implicit operator Boolean(JSONValue s)
    {
        return s.AsBool(false);
    }

    public static implicit operator JSONValue(int s)
    {
        return new JSONValue((object) ((float) s));
    }

    public static implicit operator Int32(JSONValue s)
    {
        return (int) s.AsFloat(false);
    }

    public static implicit operator JSONValue(List<JSONValue> s)
    {
        return new JSONValue(s);
    }

    public static implicit operator List<JSONValue>(JSONValue s)
    {
        return s.AsList(false);
    }

    public static implicit operator Dictionary<String, JSONValue>(JSONValue s)
    {
        return s.AsDict(false);
    }

    public void Set(string key, string value)
    {
        this.Set(key, value, true);
    }

    public void Set(string key, string value, bool allowNull)
    {
        if (value != null)
        {
            this[key] = JSONValue.NewString(value);
            return;
        }

        if (!allowNull)
        {
            return;
        }

        this[key] = JSONValue.NewNull();
    }

    public void Set(string key, float value)
    {
        this[key] = JSONValue.NewFloat(value);
    }

    public void Set(string key, bool value)
    {
        this[key] = JSONValue.NewBool(value);
    }

    public override string ToString()
    {
        return this.ToString(null, string.Empty);
    }

    public string ToString(string curIndent, string indent)
    {
        bool flag = curIndent != null;
        if (this.IsString())
        {
            return string.Concat("\"", JSONValue.EncodeString(this.AsString(false)), "\"");
        }

        if (this.IsFloat())
        {
            return this.AsFloat(false).ToString();
        }

        if (this.IsList())
        {
            string str = "[";
            string empty = string.Empty;
            foreach (JSONValue jSONValue in this.AsList(false))
            {
                str = string.Concat(str, empty, jSONValue.ToString());
                empty = ", ";
            }

            return string.Concat(str, "]");
        }

        if (!this.IsDict())
        {
            if (this.IsBool())
            {
                return (!this.AsBool(false) ? "false" : "true");
            }

            if (!this.IsNull())
            {
                throw new JSONTypeException("Cannot serialize json value of unknown type");
            }

            return "null";
        }

        string str1 = string.Concat("{", (!flag ? string.Empty : "\n"));
        string empty1 = string.Empty;
        foreach (KeyValuePair<string, JSONValue> keyValuePair in this.AsDict(false))
        {
            string str2 = str1;
            object[] objArray = new object[] {str2, empty1, curIndent, indent, '\"', JSONValue.EncodeString(keyValuePair.Key), "\" : ", null};
            JSONValue value = keyValuePair.Value;
            objArray[7] = value.ToString(string.Concat(curIndent, indent), indent);
            str1 = string.Concat(objArray);
            empty1 = string.Concat(", ", (!flag ? string.Empty : "\n"));
        }

        return string.Concat(str1, (!flag ? string.Empty : string.Concat("\n", curIndent)), "}");
    }
}