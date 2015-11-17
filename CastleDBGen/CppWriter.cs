﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleDBGen
{
    internal class CppWriter : BaseDBWriter
    {
        static readonly string CPPClassStart = "\r\nclass {0} {{\r\npublic:\r\n"; // class SHEETNAME
        static readonly string CPPClassEnd = "};\n";
        static readonly string CPPProperty = "{2}{0} {1};\n"; // Type name;\n

        public override void WriteClassDefinitions(CastleDB database, string fileBase, string sourceFileName, Dictionary<string, string> switches, List<string> errors)
        {
            string headerText = string.Format("// AUTOGENERATED C++ SOURCE CODE FROM {0}\r\n#pragma once;\r\n\r\n", sourceFileName);

            string dbName = "GameDatabase";
            if (switches.ContainsKey("db"))
                dbName = switches["db"];

            string headerPath = System.IO.Path.ChangeExtension(fileBase, ".h");
            if (switches.ContainsKey("hd"))
                headerPath = string.Format("{0}/{1}", switches["hd"], headerPath);

            string sourceText = string.Format("// AUTOGENERATED C++ SOURCE CODE FROM {0}\r\n#include \"{1}\"\r\n", sourceFileName, headerPath);
            headerText += "#include <Urho3D/Math/Color.h>\r\n";
            headerText += "#include <Urho3D/Resource/JSONFile.h>\r\n";
            headerText += "#include <Urho3D/Resource/JSONValue.h>\r\n";
            headerText += "#include <Urho3D/Container/Str.h>\r\n";
            headerText += "#include <Urho3D/Container/Vector.h>\r\n";
            headerText += "using namespace Urho3D;\r\n";

            int tabDepth = 0;
            if (switches.ContainsKey("ns"))
            {
                headerText += string.Format("\r\nnamespace {0} {{\r\n", switches["ns"]);
                sourceText += string.Format("\r\nnamespace {0} {{\r\n", switches["ns"]);
            }

        // Forward declarations
            headerText += "\r\n// Forward declarations\r\n";
            foreach (CastleSheet sheet in database.Sheets)
            {
                headerText += string.Format("class {0};\r\n", sheet.Name.Replace("@", "_"));
            }
            headerText += string.Format("class {0};\r\n", dbName);

            // Scan for enumerations and flags
            foreach (CastleSheet sheet in database.Sheets)
            {
                foreach (CastleColumn column in sheet.Columns)
                {
                    if (column.TypeID == CastleType.Enum)
                    {
                        headerText += string.Format("\r\nenum E_{0} {{\r\n", column.Name.ToUpper());
                        foreach (string value in column.Enumerations)
                            headerText += string.Format("{0}{1},\r\n", GetTabString(tabDepth + 0), value.ToUpper());
                        headerText += "};\r\n";
                    }
                    else if (column.TypeID == CastleType.Flags)
                    {
                        headerText += "\r\n";
                        int index = 0;
                        foreach (string value in column.Enumerations)
                        {
                            headerText += string.Format("static const unsigned {0}_{1} = {2};\r\n", column.Name.ToUpper(), value.ToUpper(), 1 << index);
                            ++index;
                        }
                    }
                }
            }

            foreach (CastleSheet sheet in database.Sheets)
            {
                string sheetName = sheet.Name.Replace('@', '_');
                string classStr = String.Format(CPPClassStart, sheetName);
                string cppClassStr = String.Format("\t\nvoid {0}::Load(JSONValue& value) {{\r\n", sheetName);

                foreach (CastleColumn column in sheet.Columns)
                {
                    switch (column.TypeID)
                    {
                        case CastleType.UniqueIdentifier:
                            classStr += String.Format(CPPProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetString();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Boolean:
                            classStr += String.Format(CPPProperty, "bool", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetBool();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Color:
                            classStr += String.Format(CPPProperty, "Color", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1}.FromUInt(value[\"{1}\"].GetUInt());\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Custom:
                        case CastleType.Dynamic:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.Enum:
                            classStr += String.Format(CPPProperty, String.Format("E_{0}", column.Name.ToUpper()), column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = (E_{2})value[\"{1}\"].GetInt();\r\n", GetTabString(tabDepth + 0), column.Name, column.Name.ToUpper());
                            break;
                        case CastleType.File:
                            classStr += String.Format(CPPProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetString();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Flags:
                            classStr += String.Format(CPPProperty, "unsigned", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetUInt();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Image:
                            classStr += String.Format(CPPProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetString();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Integer:
                            classStr += String.Format(CPPProperty, "int", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetInt();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Layer:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.List:
                            classStr += String.Format("{0}Vector<{1}*> {2};\r\n", GetTabString(tabDepth + 0), string.Format("{0}_{1}", sheet.Name, column.Name), column.Name);
                            cppClassStr += string.Format("{0}JSONValue& {1}Array = value[\"{1}\"];\r\n", GetTabString(tabDepth + 0), column.Name);
                            cppClassStr += string.Format("{0}for (unsigned i = 0; i < {1}Array.Size(); ++i) {{\r\n", GetTabString(tabDepth + 0), column.Name);
                            cppClassStr += string.Format("{0}{1}* val = new {1}();\r\n", GetTabString(tabDepth + 1), string.Format("{0}_{1}", sheet.Name, column.Name));
                            cppClassStr += string.Format("{0}val->Load({1}Array[i]);\r\n{0}{2}.Push(val);\r\n", GetTabString(tabDepth + 1), column.Name, column.Name);
                            cppClassStr += string.Format("{0}}} \r\n", GetTabString(tabDepth + 0));
                            break;
                        case CastleType.Ref:
                            classStr += String.Format("{0}{1}* {2};\r\n", GetTabString(tabDepth + 0), column.Key, column.Name);
                            classStr += String.Format("{0}String {2}Key;\r\n", GetTabString(tabDepth + 0), column.Key, column.Name);
                            cppClassStr += String.Format("{0}{1} = 0x0;\r\n", GetTabString(tabDepth + 0), column.Name);
                            cppClassStr += String.Format("{0}{1}Key = value[\"{1}\"].GetString();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.Text:
                            classStr += String.Format(CPPProperty, "String", column.Name, GetTabString(tabDepth + 0));
                            cppClassStr += String.Format("{0}{1} = value[\"{1}\"].GetString();\r\n", GetTabString(tabDepth + 0), column.Name);
                            break;
                        case CastleType.TileLayer:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                        case CastleType.TilePos:
                            errors.Add(String.Format("Sheet {0}, type {1} unsupported", column.Name, column.TypeID.ToString()));
                            break;
                    }
                }

                classStr += string.Format("\r\n{0}/// Destruct.\r\n{0}virtual ~{1}();\r\n", GetTabString(tabDepth + 0), sheetName);

                classStr += string.Format("\r\n{0}/// Loads the data from a JSON value.\r\n{0}void Load(JSONValue& value);\r\n", GetTabString(tabDepth + 0));
                classStr += string.Format("\r\n{0}/// Resolves references to other records by Key string.\r\n{0}void ResolveReferences({1}* database);\r\n", GetTabString(tabDepth + 0), dbName);

                classStr += CPPClassEnd;
                cppClassStr += "}\r\n";
                headerText += classStr;
                sourceText += cppClassStr;
            }

            foreach (CastleSheet sheet in database.Sheets)
            {
                sourceText += string.Format("\r\n{0}::~{0}() {{\r\n", sheet.Name.Replace("@","_"));
                foreach (CastleColumn col in sheet.Columns)
                {
                    if (col.TypeID == CastleType.Ref)
                        sourceText += string.Format("{0}{1} = 0x0;\r\n", GetTabString(tabDepth + 0), col.Name);
                    else if (col.TypeID == CastleType.List)
                        sourceText += string.Format("{0}for (unsigned i = 0; i < {1}.Size(); ++i)\r\n{2}delete {1}[i];\r\n{0}{1}.Clear();\r\n", GetTabString(tabDepth + 0), col.Name, GetTabString(tabDepth + 1));
                }
                sourceText += "}\r\n";

                sourceText += string.Format("\r\nvoid {1}::ResolveReferences({2}* db) {{\r\n", GetTabString(tabDepth + 0), sheet.Name.Replace("@","_"), dbName);
                foreach (CastleColumn col in sheet.Columns)
                {
                    if (col.TypeID == CastleType.Ref)
                    {
                        sourceText += string.Format("{0}for (unsigned i = 0; i < db->{1}List.Size(); ++i) {{\r\n", GetTabString(tabDepth + 0), col.Key);
                        sourceText += string.Format("{0}if (db->{1}List[i]->{2} == {3}) {{\r\n", GetTabString(tabDepth + 1), col.Key, database.Sheets.FirstOrDefault(s => s.Name.Equals(col.Key)).GetKeyName(), String.Format("{0}Key", col.Name));
                        sourceText += string.Format("{0}{1} = db->{2}List[i];\r\n", GetTabString(tabDepth + 2), col.Name, col.Key);
                        sourceText += string.Format("{0}break;\r\n", GetTabString(tabDepth + 2));
                        sourceText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 1));
                        sourceText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 0));
                    }
                }
                sourceText += "}\r\n";
            }

            headerText += string.Format("\r\nclass {0} {{\r\npublic:\r\n{1}/// Destruct.\r\n{1}virtual ~{0}();\r\n\r\n", dbName, GetTabString(tabDepth + 0));
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.Name.Contains("@"))
                    continue;
                headerText += string.Format("{0}Vector<{1}*> {1}List;\r\n", GetTabString(tabDepth + 0), sheet.Name);
            }
            headerText += string.Format("\r\n{0}/// Load from JSON file.\r\n{0}void Load(JSONFile* file);\r\n", GetTabString(tabDepth + 0));
            headerText += "};\r\n\r\n";

            sourceText += string.Format("\r\n{0}::~{0}() {{\r\n", dbName);
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.Name.Contains("@"))
                    continue;
                sourceText += string.Format("{0}for (unsigned i = 0; i < {1}List.Size(); ++i)\r\n{2}delete {1}List[i];\r\n", GetTabString(tabDepth + 0), sheet.Name.Replace("@","_"), GetTabString(tabDepth + 1));
                sourceText += string.Format("{0}{1}List.Clear();\r\n", GetTabString(tabDepth + 0), sheet.Name.Replace("@","_"));
            }
            sourceText += "}\r\n";

            sourceText += string.Format("\r\n{0}void {1}::Load(JSONFile* file) {{\r\n", "", dbName);
            sourceText += string.Format("{0}JSONValue& sheetsElem = file->GetRoot()[\"sheets\"];\r\n", GetTabString(tabDepth + 0));
            sourceText += string.Format("{0}for (unsigned i = 0; i < sheetsElem.Size(); ++i) {{\r\n", GetTabString(tabDepth + 0));
            sourceText += string.Format("{0}JSONValue& sheet = sheetsElem[i];\r\n{0}String sheetName = sheet[\"name\"].GetString();\r\n", GetTabString(tabDepth + 1));
            bool first = true;
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.Name.Contains("@"))
                    continue;
                sourceText += string.Format("{0}{2} (sheetName == \"{1}\") {{\r\n", GetTabString(tabDepth + 1), sheet.Name, first ? "if" : "else if");
                sourceText += string.Format("{0}JSONValue& linesElem = sheet[\"lines\"];\r\n", GetTabString(tabDepth + 2));
                sourceText += string.Format("{0}for (unsigned j = 0; j < linesElem.Size(); ++j) {{\r\n", GetTabString(tabDepth + 2));
                sourceText += string.Format("{0}{1}* val = new {1}();\r\n{0}val->Load(linesElem[j]);\r\n{0}{1}List.Push(val);\r\n", GetTabString(tabDepth + 3), sheet.Name);
                sourceText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 2));
                sourceText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 1));
                first = false;
            }
            sourceText += string.Format("{0}}}\r\n", GetTabString(tabDepth + 0));
            // Write reference resolving code
            foreach (CastleSheet sheet in database.Sheets)
            {
                if (sheet.HasReferences())
                {
                    sourceText += string.Format("{0}for (unsigned i = 0; i < {1}List.Size(); ++i)\r\n", GetTabString(tabDepth + 0), sheet.Name);
                    sourceText += string.Format("{0}{1}List[i]->ResolveReferences(this);\r\n", GetTabString(tabDepth + 1), sheet.Name);
                }
            }
            sourceText += "}\r\n";

            if (switches.ContainsKey("ns"))
            {
                headerText += "\r\n}\t\n";
                sourceText += "\r\n}\r\n";
            }

            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(fileBase, ".h"), headerText);
            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(fileBase, ".cpp"), sourceText);
        }
    }
}
