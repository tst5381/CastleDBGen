﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleDBGen
{
    internal class ASBindingWriter : BaseDBWriter
    {
        public override void WriteClassDefinitions(CastleDB database, string fileBase, string sourceFileName, Dictionary<string, string> switches, List<string> errors)
        {
            int tabDepth = 0;
            string headerCode = string.Format("// AUTOGENERATED SOURCE CODE FROM {0}\r\n", sourceFileName);
            headerCode += "#pragma once\r\n";
            string sourceCode = string.Format("// AUTOGENERATED SOURCE CODE FROM {0}\r\n", sourceFileName);

            // Include headers
            sourceCode += "#include <AngelScript/angelscript.h>\r\n";
            sourceCode += "#include <Urho3D/AngelScript/Addons.h>\r\n";
            sourceCode += "#include <Urho3D/AngelScript/APITemplates.h>\r\n";

            string inherits = "";
            if (switches.ContainsKey("inherits"))
                inherits = switches["inherits"];
            if (!inherits.Equals("RefCounted"))
            {
                errors.Add("Can only generate AS bindings for RefCounted derived types");
                return;
            }
            
            string dbName = "GameDatabase";
            if (switches.ContainsKey("db"))
                dbName = switches["db"];

            bool integerIDs = false;
            if (switches.ContainsKey("id"))
                integerIDs = switches["id"].Equals("int");

            bool binIO = false;
            bool jsonOff = false;
            if (switches.ContainsKey("bin"))
            {
                binIO = switches["bin"].Equals("on") || switches["bin"].Equals("only");
                jsonOff = switches["bin"].Equals("only");
            }

        // Forward function declarations
            foreach (CastleSheet sheet in database.Sheets)
            {
                string sheetName = sheet.Name.Replace("@", "_");
                // Factory constructor
                sourceCode += string.Format("static {0}* Construct{0}() {{\r\n", sheetName);
                sourceCode += string.Format("{0}return new {1}();\r\n}}", GetTabstring(tabDepth + 0), sheetName);

                foreach (CastleColumn col in sheet.Columns)
                {
                    // implement setter/getters for string types
                    if (col.TypeID == CastleType.List)
                    {
                        sourceCode += string.Format("static CScriptArray* {0}Get{1}({0}& obj) {{\r\n", sheetName, col.Name);
                        sourceCode += string.Format("{0}return VectorToArray<{1}>(obj.{2}, \"Array<{1}@+>\");\r\n}}", GetTabstring(tabDepth + 0), col.Key, col.Name);
                    }
                }
            }
        // Database constructor
            sourceCode += string.Format("static {0}* Construct{0}() {{\r\n", dbName);
            sourceCode += string.Format("{0}return new {1}();\r\n}}", GetTabstring(tabDepth + 0), dbName);

            headerCode += "class asIScriptEngine;\r\n";
            headerCode += string.Format("void Register{0}(asIScriptEngine* engine);\r\n", dbName);
            sourceCode += string.Format("void Register{0}(asIScriptEngine* engine) {{\r\n", dbName);
            foreach (CastleSheet sheet in database.Sheets)
            {
                foreach (CastleColumn col in sheet.Columns)
                {
                    if (col.TypeID == CastleType.Enum)
                    {
                        sourceCode += string.Format("    engine->RegisterEnum(\"{0}\");\r\n", "E_" + col.Name.ToUpper());
                        for (int i = 0; i < col.Enumerations.Count; ++i)
                            sourceCode += string.Format("    engine->RegisterEnum(\"{0}\", {1});\r\n", col.Enumerations[i].ToUpper(), i);
                    }
                }
            }

            database.Sheets.Sort(new DependencySort());
            // Bind types and constructors
            foreach (CastleSheet sheet in database.Sheets)
            {
                string sheetName = sheet.Name.Replace("@","_");
                sourceCode += string.Format("    RegisterRefCounted<{0}>(engine, \"{0}\");\r\n", sheetName);
                sourceCode += string.Format("    engine->RegisterObjectBehaviour(\"{0}\", asBEHAVE_FACTORY, \"{0}@+ f()\", asFUNCTION(Construct{0}), asCALL_CDECL);\r\n", sheetName);
            }

            // Bind methods
            foreach (CastleSheet sheet in database.Sheets)
            {
                string sheetName = sheet.Name.Replace("@", "_");
                foreach (CastleColumn col in sheet.Columns)
                {
                    switch (col.TypeID)
                    {
                        case CastleType.UniqueIdentifier:
                        case CastleType.File:
                        case CastleType.Image:
                        case CastleType.Text:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "String", col.Name);
                            break;
                        case CastleType.Integer:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "int", col.Name);
                            break;
                        case CastleType.Enum:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "int", col.Name);
                            break;
                        case CastleType.Flags:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "uint", col.Name);
                            break;
                        case CastleType.Float:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "float", col.Name);
                            break;
                        case CastleType.Boolean:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "bool", col.Name);
                            break;
                        case CastleType.Color:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "Color", col.Name);
                            break;
                        case CastleType.Ref:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1}@+ {2}\", offsetof({0}, {2}));\r\n", sheetName, col.Key, col.Name);
                            break;
                        case CastleType.List:
                            sourceCode += string.Format("    engine->RegisterObjectMethod(\"{0}\", \"Array<{1}@+>@ get_{2}()\", asFUNCTION({0}Get{2}), asCALL_CDECL_OBJLAST);\r\n", sheetName, col.Key, col.Name);
                            break;
                        case CastleType.Dynamic:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "JSONValue", col.Name);
                            break;
                        case CastleType.TileLayer:
                            sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, "CastleTileLayer@+", col.Name);
                            break;
                        case CastleType.Custom:
                            CastleCustom custom = database.CustomTypes.FirstOrDefault(c => c.Name.Equals(col.Key));
                            if (custom != null)
                            {
                                if (!custom.Constructors[0].returnType.Equals("void"))
                                    sourceCode += string.Format("    engine->RegisterObjectProperty(\"{0}\", \"{1} {2}\", offsetof({0}, {2}));\r\n", sheetName, custom.Constructors[0].returnType.Replace("*", "@+"), col.Name);
                            }
                            break;
                    }
                }
                sourceCode += "\r\n";
            }

        // Register database
            sourceCode += string.Format("    RegisterRefCounted<{0}>(engine, \"{0}\");\r\n", dbName);
            sourceCode += string.Format("    engine->RegisterObjectBehaviour(\"{0}\", asBEHAVE_FACTORY, \"{0}@+ f()\", asFUNCTION(Construct{0}), asCALL_CDECL);\r\n", dbName);
            if (!jsonOff)
                sourceCode += string.Format("    engine->RegisterObjectMethod(\"{0}\", \"void Load(JSONFile@+)\", asMETHODPR({0}, Load, (JSONFile*), void), asCALL_THISCALL);\r\n", dbName);
            if (binIO)
            {
                sourceCode += string.Format("    engine->RegisterObjectMethod(\"{0}\", \"void Load(Deserializer&)\", asMETHODPR({0}, Load, (Deserializer&), void), asCALL_THISCALL);\r\n", dbName);
                sourceCode += string.Format("    engine->RegisterObjectMethod(\"{0}\", \"void Save(Serializer&)\", asMETHODPR({0}, Load, (Serializer&), void), asCALL_THISCALL);\r\n", dbName);
            }

            sourceCode += "}\r\n";

            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(fileBase, ".h"), headerCode);
            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(fileBase, ".cpp"), sourceCode);
        }
    }
}
