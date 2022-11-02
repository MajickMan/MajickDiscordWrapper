using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MajickDiscordWrapper.MajickRegex
{
    public class JsonObject
    {
        static string empty_array = "^(\"(\\w+?)\")\\s??:\\s??\\[\\]";
        static string object_array = "^(\"(\\w+?)\")\\s??:\\s??\\[{";
        static string attribute_array = "^(\"(\\w+?)\")\\s??:\\s??\\[\"??[\\w\\d]";
        static string json_object = "^(\"(\\w+?)\")\\s??:\\s??{";
        static string json_attribute = "^(\"(\\w+?)\")\\s??:\\s??\"??[\\w\\d\\s\\W]";
        static Regex EmptyArray = new Regex(empty_array);
        static Regex ObjectArray = new Regex(object_array);
        static Regex AttributeArray = new Regex(attribute_array);
        static Regex JsonObjectPattern = new Regex(json_object);
        static Regex JsonAttributePattern = new Regex(json_attribute);
        private Dictionary<string, JsonAttribute> _attributes;
        private Dictionary<string, JsonObject> _objects;
        private Dictionary<string, IReadOnlyList<JsonAttribute>> _attributeLists;
        private Dictionary<string, IReadOnlyList<JsonObject>> _objectLists;
        public string Name { get; internal set; }
        public string RawText { get; internal set; }
        public IReadOnlyDictionary<string, JsonAttribute> Attributes { get { return _attributes; } }
        public IReadOnlyDictionary<string, JsonObject> Objects { get { return _objects; } }
        public IReadOnlyDictionary<string, IReadOnlyList<JsonAttribute>> AttributeLists { get { return _attributeLists; } }
        public IReadOnlyDictionary<string, IReadOnlyList<JsonObject>> ObjectLists { get { return _objectLists; } }
        public JsonObject()
        {
            _attributes = new Dictionary<string, JsonAttribute>();
            _objects = new Dictionary<string, JsonObject>();
            _attributeLists = new Dictionary<string, IReadOnlyList<JsonAttribute>>();
            _objectLists = new Dictionary<string, IReadOnlyList<JsonObject>>();
        }
        public JsonObject(string object_text)
        {
            _attributes = new Dictionary<string, JsonAttribute>();
            _objects = new Dictionary<string, JsonObject>();
            _attributeLists = new Dictionary<string, IReadOnlyList<JsonAttribute>>();
            _objectLists = new Dictionary<string, IReadOnlyList<JsonObject>>();

            string current_attribute_name;
            string object_clone = object_text.Replace("\\\"", "~%");
            if (object_clone.StartsWith("{")) { object_clone = object_clone.Substring(1); }
            if (object_clone.EndsWith("}")) { object_clone = object_clone.Substring(0, object_clone.Length - 1); }
            object_clone = object_clone.Trim();
            while (object_clone.Length > 0)
            {
                if (object_clone.StartsWith(",")) { object_clone = object_clone.Substring(1).Trim(); }
                Match EmptyArrayMatch = EmptyArray.Match(object_clone);
                Match ObjectArrayMatch = ObjectArray.Match(object_clone);
                Match AttributeArrayMatch = AttributeArray.Match(object_clone);
                Match JsonObjectMatch = JsonObjectPattern.Match(object_clone);
                Match JsonAttributeMatch = JsonAttributePattern.Match(object_clone);
                current_attribute_name = object_clone.Substring(0, object_clone.IndexOf(":")).Replace("\"", "");
                object_clone = object_clone.Substring(object_clone.IndexOf(":") + 1).Trim();
                if (ObjectArrayMatch.Index == 0 && ObjectArrayMatch.Success)
                {
                    List<JsonObject> inner_object_list = new List<JsonObject>();
                    int inner_array_length = 0;
                    string object_array_text = "";
                    char[] data_array = object_clone.ToArray();
                    int open = 0;
                    int close = 0;
                    foreach (char current_char in data_array)
                    {
                        inner_array_length += 1;
                        object_array_text += current_char;
                        if (current_char == '[') { open += 1; }
                        if (current_char == ']') { close += 1; }
                        if (open > 0 && open == close) { break; }
                    }
                    object_clone = object_clone.Replace(object_array_text, "");
                    object_array_text = object_array_text.Substring(1, object_array_text.Length - 2);
                    while (object_array_text.Length > 0)
                    {
                        int inner_object_length = 0;
                        string inner_object_text = "";
                        char[] object_array_data = object_array_text.ToArray();
                        int object_open = 0;
                        int object_close = 0;
                        foreach (char current_char in object_array_data)
                        {
                            inner_object_length += 1;
                            inner_object_text += current_char;
                            if (current_char == '{') { object_open += 1; }
                            if (current_char == '}') { object_close += 1; }
                            if (object_open > 0 && object_open == object_close) { break; }
                        }
                        object_array_text = object_array_text.Replace(inner_object_text, "");
                        if (object_array_text.StartsWith(",")) { object_array_text = object_array_text.Substring(1).Trim(); }
                        JsonObject inner_object = new JsonObject(inner_object_text);
                        inner_object_list.Add(inner_object);
                    }
                    AddObjectList(current_attribute_name, inner_object_list);
                }
                else if (AttributeArrayMatch.Index == 0 && AttributeArrayMatch.Success)
                {
                    List<JsonAttribute> inner_attribute_array = new List<JsonAttribute>();
                    int inner_array_length = 0;
                    string attribute_array_text = "";
                    char[] data_array = object_clone.ToArray();
                    int open = 0;
                    int close = 0;
                    foreach (char current_char in data_array)
                    {
                        inner_array_length += 1;
                        attribute_array_text += current_char;
                        if (current_char == '[') { open += 1; }
                        if (current_char == ']') { close += 1; }
                        if (open > 0 && open == close) { break; }
                    }
                    if (attribute_array_text == object_clone) { object_clone = ""; }
                    else { object_clone = object_clone.Substring(inner_array_length + 1); }
                    if (attribute_array_text.StartsWith("[")) { attribute_array_text = attribute_array_text.Substring(1); }
                    if (attribute_array_text.EndsWith("]")) { attribute_array_text = attribute_array_text.Substring(0, attribute_array_text.Length - 1); }
                    if (attribute_array_text.Contains("\""))
                    {
                        while(attribute_array_text.Length > 0)
                        {
                            int array_item_length = 0;
                            string array_item_text = "";
                            char[] inner_data_array = attribute_array_text.ToArray();
                            int quotes = 0;
                            foreach (char current_char in inner_data_array)
                            {
                                array_item_length += 1;
                                array_item_text += current_char;
                                if (current_char == '\"') { quotes += 1; }
                                if (quotes == 2) { break; }
                            }
                            if (array_item_text == attribute_array_text) { attribute_array_text = ""; }
                            else { attribute_array_text = attribute_array_text.Substring(array_item_length + 1); }
                            array_item_text = array_item_text.Substring(1, array_item_text.Length - 2);
                            array_item_text = array_item_text.Replace("~%", "\\\"");
                            JsonAttribute inner_array_item = new JsonAttribute(array_item_text);
                            inner_attribute_array.Add(inner_array_item);
                        }
                    }
                    else
                    {
                        string[] attribute_data_array = attribute_array_text.Split(',');
                        foreach(string attribute_data in attribute_data_array)
                        {
                            JsonAttribute inner_array_item = new JsonAttribute(attribute_data, true);
                            inner_attribute_array.Add(inner_array_item);
                        }
                    }
                    AddAttributeList(current_attribute_name, inner_attribute_array);
                }
                else if (EmptyArrayMatch.Index == 0 && EmptyArrayMatch.Success)
                {
                    AddAttributeList(current_attribute_name, new List<JsonAttribute>());
                    if (object_clone == "[]") { object_clone = ""; }
                    else { object_clone = object_clone.Substring(3).Trim(); }
                }
                else if (JsonObjectMatch.Index == 0 && JsonObjectMatch.Success)
                {
                    int inner_object_length = 0;
                    string inner_object_text = "";
                    char[] data_array = object_clone.ToArray();
                    int open = 0;
                    int close = 0;
                    foreach (char current_char in data_array)
                    {
                        inner_object_length += 1;
                        inner_object_text += current_char;
                        if (current_char == '{') { open += 1; }
                        if (current_char == '}') { close += 1; }
                        if (open > 0 && open == close) { break; }
                    }
                    if (inner_object_text == object_clone) { object_clone = ""; }
                    else { object_clone = object_clone.Substring(inner_object_length + 1); }
                    AddObject(current_attribute_name, new JsonObject(inner_object_text));
                }
                else if (JsonAttributeMatch.Index == 0 && JsonAttributeMatch.Success)
                {
                    int attribute_length = 0;
                    string attribute_text = "";
                    if (object_clone.StartsWith("\""))
                    {
                        char[] data_array = object_clone.ToArray();
                        int quotes = 0;
                        foreach (char current_char in data_array)
                        {
                            attribute_length += 1;
                            attribute_text += current_char;
                            if (current_char == '\"') { quotes += 1; }
                            if (quotes == 2) { break; }
                        }
                        if (attribute_text == object_clone) { object_clone = ""; }
                        else { object_clone = object_clone.Substring(attribute_length + 1); }
                        attribute_text = attribute_text.Substring(1, attribute_text.Length - 2);
                        attribute_text = attribute_text.Replace("~%", "\\\"");
                        AddAttribute(current_attribute_name, attribute_text);
                    }
                    else if (object_clone.Contains(","))
                    {
                        attribute_text = object_clone.Substring(0, object_clone.IndexOf(","));
                        object_clone = object_clone.Substring(object_clone.IndexOf(",") + 1);
                        AddAttribute(current_attribute_name, attribute_text, true);
                    }
                    else
                    {
                        attribute_text = object_clone;
                        object_clone = "";
                        AddAttribute(current_attribute_name, attribute_text, true);
                    }
                }
                else
                {
                    //This should never be hit?
                }
            }
        }
        public void AddAttribute(string Name, string TextValue, bool IsNumeric = false) { _attributes.Add(Name, new JsonAttribute(Name, TextValue, IsNumeric)); }
        public void AddObject(string Name, JsonObject ObjectValue) { _objects.Add(Name, ObjectValue); }
        public void AddAttributeList(string Name, List<JsonAttribute> AttributeList) { _attributeLists.Add(Name, AttributeList); }
        public void AddObjectList(string Name, List<JsonObject> ObjectList) { _objectLists.Add(Name, ObjectList); }
        public string ToRawText(bool use_name = true)
        {
            string RawText = "";
            if (Name != "" & use_name) { RawText = "\"" + Name + "\":{"; }
            else RawText = "{";
            //fill the object values in here
            foreach (string AttributeName in Attributes.Keys)
            {
                if (Attributes[AttributeName].is_numeric) { RawText += Attributes[AttributeName].ToRawText().ToLower() + ","; }
                else
                {
                    if (Attributes[AttributeName].text_value != "") { RawText += Attributes[AttributeName].ToRawText() + ","; }
                    else { RawText += "\"" + AttributeName + "\":\"\","; }
                }
            }
            foreach (string ListName in AttributeLists.Keys)
            {
                RawText += "\"" + ListName + "\":[";
                foreach (JsonAttribute Value in AttributeLists[ListName])
                {
                    RawText += Value.ToListText() + ",";
                }
                RawText = RawText.Substring(0, RawText.Length - 1);
                RawText += "],";
            }
            foreach (string ObjectName in Objects.Keys)
            {
                RawText += "\"" + ObjectName + "\":" + Objects[ObjectName].ToRawText(false) + ",";
            }
            foreach (string ObjListName in ObjectLists.Keys)
            {
                RawText += "\"" + ObjListName + "\":[";
                foreach (JsonObject Value in ObjectLists[ObjListName])
                {
                    RawText += Value.ToRawText(false) + ",";
                }
                if (RawText.EndsWith(",")) { RawText = RawText.Substring(0, RawText.Length - 1); }
                RawText += "],";
            }
            if (RawText.EndsWith(",")) { RawText = RawText.Substring(0, RawText.Length - 1); }
            RawText += "}";
            return RawText;
        }
    }
    public class JsonAttribute
    {
        public string name;
        public bool is_numeric;
        public string text_value;
        public JsonAttribute()
        {
            name = "";
            is_numeric = false;
            text_value = "";
        }
        public JsonAttribute(string new_name, string new_value, bool numeric = false)
        {
            name = new_name;
            text_value = new_value;
            is_numeric = numeric;
        }
        public JsonAttribute(string new_value, bool numeric = false)
        {
            name = "";
            text_value = new_value;
            is_numeric = numeric;
        }
        public string ToRawText()
        {
            string RawText = "\"" + name + "\":";
            if (is_numeric) { RawText += text_value; }
            else { RawText += "\"" + text_value + "\""; }
            return RawText;
        }
        public string ToListText()
        {
            if (is_numeric) { return text_value; }
            else { return "\"" + text_value + "\""; }
        }
    }
}
