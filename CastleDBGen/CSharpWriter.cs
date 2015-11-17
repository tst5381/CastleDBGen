﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleDBGen
{
    internal class CSharpWriter : BaseDBWriter
    {
        static readonly string ASClassStart = "\r\npublic class {0} {{\r\n"; // class SHEETNAME
        static readonly string ASClassEnd = "}\r\n";
        static readonly string ASProperty = "{2}public {0} {1} {{ get; set; }}\r\n"; // Type name;\n

        public override void WriteClassDefinitions(CastleDB database, string fileBase, string sourceFileName, Dictionary<string, string> switches, List<string> errors)
        {
            string dbName = "GameDatabase";
            if (switches.ContainsKey("db"))
                dbName = switches["db"];

            int tabDepth = 0;
            string namespaceName = switches["ns"];
            string fileText = string.Format("// AUTOGENERATED C# SOURCE CODE FROM {0}\r\n", sourceFileName);
            fileText += "\r\nusing System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing System.Text;\r\nusing Newtonsoft.Json.Linq;\r\n";
            fileText += string.Format("\r\nnamespace {0} {{\r\n", namespaceName);

            // Scan for enumerations and flags
            foreach (CastleSheet sheet in database.Sheets)
            {
                foreach (CastleColumn column in sheet.Columns)
                {
                    if (column.TypeID == CastleType.Enum)
                    {
                        fileText += string.Format("\r\npublic enum E_{0} {{\r\n", column.Name.ToUpper());
                        foreach (string value in column.Enumerations)
                            fileText += string.Format("{0}{1},\r\n", GetTabString(tabDepth + 0), value.ToUpper());
                        fileText += "}\r\n";
                    }
                }
            }

            foreach (CastleSheet sheet in database.Sheets)
            {
                string sheetName = sheet.Name.Replace('@', '_');
                string classStr = String.Format(ASClassStart, sheetName);

                foreach (CastleColumn column in sheet.Columns)
                {
                    switch (column.TypeID)
                    {
                        case CastleType.UniqueIdentifier:
                            classStr += String.Format(ASProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Boolean:
                            classStr += String.Format(ASProperty, "bool", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Color:
                            classStr += String.Format(ASProperty, "Color", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Custom:
                        case CastleType.Dynamic:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.Enum:
                            classStr += String.Format(ASProperty, String.Format("E_{0}", column.Name.ToUpper()), column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.File:
                            classStr += String.Format(ASProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Flags:
                            classStr += String.Format(ASProperty, "uint", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Image:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            //classStr += String.Format(ASProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Integer:
                            classStr += String.Format(ASProperty, "int", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Layer:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.List:
                            classStr += String.Format("{0}public List<{1}> {2} = new List<{1}>();\r\n", GetTabString(tabDepth + 0), String.Format("{0}_{1}", sheet.Name, column.Name), column.Name);
                            break;
                        case CastleType.Ref:
                            classStr += String.Format("{0}public {1} {2} = null;\r\n", GetTabString(tabDepth + 0), column.Key, column.Name);
                            classStr += String.Format("{0}private String {2}Key;\r\n", GetTabString(tabDepth + 0), column.Key, column.Name);
                            break;
                        case CastleType.Text:
                            classStr += String.Format(ASProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            break;
                        case CastleType.TileLayer:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.TilePos:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                    }
                }

                // generate loading function

                classStr += string.Format("\r\n{0}public void Load(JObject value) {{\r\n", GetTabString(tabDepth + 0));
                foreach (CastleColumn col in sheet.Columns)
                {
                    switch (col.TypeID)
                    {
                        case CastleType.UniqueIdentifier:
                            classStr += string.Format("{0}{1} = value.Property(\"{1}\").Value.ToString();\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Boolean:
                            classStr += string.Format("{0}{1} = value.Property(\"{1}\").Value.ToString().Equals(\"true\");\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Color:
                            classStr += string.Format("{0}{1}.FromUInt(uint.Parse(value.Property(\"{1}\").Value.ToString());\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Enum:
                            classStr += string.Format("{0}{1} = (E_{2})int.Parse(value.Property(\"{1}\").Value.ToString());\r\n", GetTabString(tabDepth + 1), col.Name, col.Name.ToUpper());
                            break;
                        case CastleType.Image:
                            // Unimplemented
                            break;
                        case CastleType.File:
                            classStr += string.Format("{0}{1} = value.Property(\"{1}\").ToString();\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Flags:
                            classStr += string.Format("{0}{1} = uint.Parse(value.Property(\"{1}\").Value.ToString());\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Float:
                            classStr += string.Format("{0}{1} = float.Parse(value.Property(\"{1}\").Value.ToString());r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Integer:
                            classStr += string.Format("{0}{1} = int.Parse(value.Property(\"{1}\").Value.ToString());\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.List:
                            classStr += string.Format("{0}JArray {1}Array = value.Property(\"{1}\").Value as JArray;\r\n", GetTabString(tabDepth + 1), col.Name);
                            classStr += string.Format("{0}for (int i = 0; i < {1}Array.Count; ++i) {{\r\n", GetTabString(tabDepth + 1), col.Name);
                            classStr += string.Format("{0}{1} val = new {1}();\r\n", GetTabString(tabDepth + 2), string.Format("{0}_{1}", sheet.Name, col.Name));
                            classStr += string.Format("{0}val.Load({1}Array[i] as JObject);\r\n{0}{2}.Add(val);\r\n", GetTabString(tabDepth + 2), col.Name, col.Name);
                            classStr += string.Format("{0}}} \r\n", GetTabString(tabDepth + 1));
                            break;
                        case CastleType.Ref:
                            classStr += string.Format("{0}{1}Key = value.Property(\"{1}\").Value.ToString();\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                        case CastleType.Text:
                            classStr += string.Format("{0}{1} = value.Property(\"{1}\").Value.ToString();\r\n", GetTabString(tabDepth + 1), col.Name);
                            break;
                    }
                }
                classStr += string.Format("{0}}}\r\n", GetTabString(tabDepth + 0));

                classStr += string.Format("\r\n{0}public void ResolveReferences({1} db) {{\r\n", GetTabString(tabDepth + 0), dbName);
                foreach (CastleColumn col in sheet.Columns)
                {
                    if (col.TypeID == CastleType.Ref)
                    {
                        classStr += string.Format("{0}for (int i = 0; i < db.{1}List.Count; ++i) {{\r\n", GetTabString(tabDepth + 1), col.Key);
                        classStr += string.Format("{0}if (db.{1}List[i].{2}.Equals({3})) {{\r\n", GetTabString(tabDepth + 2), col.Key, database.Sheets.FirstOrDefault(s => s.Name.Equals(col.Key)).GetKeyName(), String.Format("{0}Key", col.Name));
                        classStr += string.Format("{0}{1} = db.{2}List[i];\r\n", GetTabString(tabDepth + 3), col.Name, col.Key);
                        classStr += string.Format("{0}break;\r\n", GetTabString(tabDepth + 3));
                        classStr += string.Format("{0}}}\r\n", GetTabString(tabDepth + 2));
                        classStr += string.Format("{0}}}\r\n", GetTabString(tabDepth + 1));
                    }
                }

                classStr += string.Format("{0}}}\r\n", GetTabString(tabDepth + 0));

                classStr += ASClassEnd;
                fileText += classStr;
            }

            fileText += string.Format("\r\npublic class {0} {{\r\n", dbName);
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.Name.Contains("@"))
                    continue;
                fileText += string.Format("{0}public List<{1}> {1}List = new List<{1}>();\r\n", GetTabString(tabDepth + 0), sheet.Name);
            }

            fileText += string.Format("\r\n{0}public void Load(JObject file) {{\r\n", GetTabString(tabDepth + 0));
            fileText += string.Format("{0}JArray sheetsElem = file.Property(\"sheets\").Value as JArray;\r\n", GetTabString(tabDepth + 1));
            fileText += string.Format("{0}for (int i = 0; i < sheetsElem.Count; ++i) {{\r\n", GetTabString(tabDepth + 1));
            fileText += string.Format("{0}JObject sheet = sheetsElem[i] as JObject;\r\n{0}String sheetName = sheet.Property(\"name\").Value.ToString();\r\n", GetTabString(tabDepth + 2));
            bool first = true;
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.Name.Contains("@"))
                    continue;
                fileText += string.Format("{0}{2} (sheetName.Equals(\"{1}\")) {{\r\n", GetTabString(tabDepth + 2), sheet.Name, first ? "if" : "else if");
                fileText += string.Format("{0}JArray linesElem = sheet.Property(\"lines\").Value as JArray;\r\n", GetTabString(tabDepth + 3));
                fileText += string.Format("{0}for (int j = 0; j < linesElem.Count; ++j) {{\r\n", GetTabString(tabDepth + 3));
                fileText += string.Format("{0}{1} val = new {1}();\r\n{0}val.Load(linesElem[j] as JObject);\r\n{0}{1}List.Add(val);\r\n", GetTabString(tabDepth + 4), sheet.Name);
                fileText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 3));
                fileText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 2));
                first = false;
            }
            fileText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 1));
            // Write reference resolving code
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.HasReferences())
                {
                    fileText += string.Format("{0}for (int i = 0; i < {1}List.Count; ++i)\r\n", GetTabString(tabDepth + 1), sheet.Name);
                    fileText += string.Format("{0}{1}List[i].ResolveReferences(this);\r\n", GetTabString(tabDepth + 2), sheet.Name);
                }
            }
            fileText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 0));
            fileText += "}\r\n";

            fileText += "\r\n}\r\n";
            System.IO.File.WriteAllText(fileBase, fileText);
        }
    }
}
