using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonCompare
{
    internal class Program
    {
        //const string Filename1 = @"C:\src\ceg\Air\Air.WebApp\ClientApp\angular.json";
        //const string Filename2 = @"C:\src\ceg\Air2\Air.Web.Client\angular.json";

        const string Filename1 = @"C:\src\ceg\Air\Air.WebApp\ClientApp\tsconfig.json";
        const string Filename2 = @"C:\src\ceg\Air2\Air.Web.Client\tsconfig.json";

        static int lvl = 0;

        static void Main(string[] args)
        {
            //using StreamReader reader1 = File.OpenText(Filename1);
            //using StreamReader reader2 = File.OpenText(Filename2);
            //var root1 = (JObject)JToken.ReadFrom(new JsonTextReader(reader1));
            //var root2 = (JObject)JToken.ReadFrom(new JsonTextReader(reader2));

            var root1 = GetFirstObject(Filename1);
            var root2 = GetFirstObject(Filename2);

            //Walk(root1);
            CompareObjects(root1, root2);
        }

        static JObject GetFirstObject(string filename)
        {
            //var serializer = new JsonSerializer();
            using StreamReader reader = File.OpenText(filename);
            using var jsonReader = new JsonTextReader(reader);
            jsonReader.SupportMultipleContent = true;
            JObject result = null;
            while (jsonReader.Read() && result == null)
            {
                var item = JToken.ReadFrom(jsonReader);
                if (item is JObject o)
                {
                    result = o;
                }
            }
            if (result == null)
            {
                throw new Exception($"Could not find a JObject in {filename}");
            }
            return result;
        }

        static void CompareObjects(JObject o1, JObject o2)
        {
            Log(">" + o1.Path);
            lvl++;

            var keys1 = o1.Properties().Select(x => x.Name);
            var keys2 = o2.Properties().Select(x => x.Name);

            var missingFrom2 = keys1.Where(k => !keys2.Contains(k));
            var missingFrom1 = keys2.Where(k => !keys1.Contains(k));

            foreach (var m in missingFrom2)
            {
                Log($"missing from 2: {m}");
            }

            foreach (var m in missingFrom1)
            {
                Log($"missing from 1: {m}");
            }

            foreach (var k in keys1.Where(k => keys2.Contains(k)))
            {
                if (o1[k] is JValue)
                {
                    if (o2[k] is not JValue)
                    {
                        Log($"different token types at {k}: {o1[k].Type} vs {o2[k].Type}");
                    }
                    else
                    {
                        CompareValues(k, (JValue)o1[k], (JValue)o2[k]);
                    }
                }

                else if (o1[k] is JObject)
                {
                    CompareObjects((JObject)o1[k], (JObject)o2[k]);
                }

                else if (o1[k] is JArray)
                {
                    if (o2[k] is not JArray)
                    {
                        Log($"different token types at {k}: {o1[k].Type} vs {o2[k].Type}");
                    }
                    else
                    {
                        CompareArrays(k, (JArray)o1[k], (JArray)o2[k]);
                    }
                }

                else
                {
                    throw new Exception($"Token type not recog: {o1[k].Type}");
                }
            }

            lvl--;
        }

        private static void CompareArrays(string key, JArray a1, JArray a2)
        {
            if (a1.All(x => x is JValue) && a2.All(x => x is JValue))
            {
                var v1 = string.Join("|", a1.Values().Select(x => ((JValue)x).Value.ToString()).OrderBy(x => x));
                var v2 = string.Join("|", a2.Values().Select(x => ((JValue)x).Value.ToString()).OrderBy(x => x));
                if (v1 != v2)
                {
                    Log($"different arrays in {key}");
                }
            }
            else
            {
                Log($"complex arrays cannot be compared in {key}");
            }
        }

        static void CompareValues(string key, JValue v1, JValue v2)
        {
            if (v1.Type != v2.Type)
            {
                Log($"different value types at {v1.Path}");
            }

            bool isDifferent() => v1.Type switch
            {
                JTokenType.String => (string)v1.Value != (string)v2.Value,
                JTokenType.Boolean => (bool)v1.Value != (bool)v2.Value,
                JTokenType.Integer => (long)v1.Value != (long)v2.Value,
                _ => throw new Exception($"Value type not implemented: {v1.Type}"),
            };

            if (isDifferent())
            {
                Log($"different values for {key}: {v1.Value} vs {v2.Value}");
            }
        }

        static void Walk(JToken t)
        {
            lvl++;

            if (t is JObject o && t.Type == JTokenType.Object)
            {
                WalkObject(o);
            }
            else if (t is JProperty p && t.Type == JTokenType.Property)
            {
                WalkProperty(p);
            }
            else if (t is JArray a && t.Type == JTokenType.Array)
            {
                WalkArray(a);
            }
            else if (t is JValue v)
            {
                WalkValue(v);
            }
            else
            {
                throw new Exception($"Token type not recognized: {t.GetType().Name}, {t.Type}");
            }

            if (t.HasValues)
            {
                foreach (var c in t)
                {
                    Walk(c);
                }
            }

            lvl--;
        }

        private static void WalkObject(JObject o)
        {
            Log("{ object }");
            foreach (var c in (JToken)o)
            {
                if (c is not JProperty)
                {
                    throw new Exception("child of object is not a property");

                }
            }
        }

        private static void WalkProperty(JProperty p) => Log($"Property {p.Name}");

        private static void WalkValue(JValue v) => Log($"Value {v.Value}, {v.Value.GetType().Name}");

        private static void WalkArray(JArray a) => Log($"Array {a.Count}");

        private static void Log(string s)
        {
            Console.WriteLine(new string(' ', lvl * 4) +  s);
        }
    }
}
