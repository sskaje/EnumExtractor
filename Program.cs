using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace EnumExtractor
{
    class Program
    {

        static void Usage()
        {
            Console.Error.WriteLine("Usage: {0} DLL_FILE [ENUM_FILTER]", System.AppDomain.CurrentDomain.FriendlyName);
            System.Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            string fileName = null;
            string enumFilter = ".";

            if (args.Length > 0)
            {
                if (Array.IndexOf(args, "-h") > -1 || Array.IndexOf(args, "--help") > -1)
                {
                    Usage();
                }

                if (File.Exists(args[0]))
                {
                    fileName = args[0];
                }

                if (args.Length > 1)
                {
                    enumFilter = args[1];
                }
            }
            else
            {
                Console.Error.Write("Enter External Assembly:");
                string tmpFileName = Console.ReadLine();
                if (File.Exists(tmpFileName))
                {
                    fileName = tmpFileName;
                }
            }

            if (String.IsNullOrEmpty(fileName))
            {
                Console.Error.WriteLine("Error: Invalid File");
                Usage();
            }

            try
            {
                Assembly asm = Assembly.LoadFrom(fileName);
                DumpEnums(asm, enumFilter);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Can't Load Assembly " + e.GetType() + " message=" + e.Message);
            }


        }

        static void DumpEnums(Assembly a, string enumFilter)
        {
            Console.Error.WriteLine("*******Contents in Assembly*********");
            Console.Error.WriteLine("Information:{0}", a.FullName);
            
            Module[] mod = a.GetModules();
            foreach (Module m in mod)
            {
                Console.Error.WriteLine("Module: {0}", m);
            }

            List<DumpedEnum> dumpedEnums = new List<DumpedEnum>();

            Type[] types;
            try
            {
                types = a.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            foreach (Type type in types)
            {
                if (type != null)
                {
                    try
                    {
                        if (type.FullName.Contains(enumFilter) && type.IsEnum) {
                            Console.Error.WriteLine("Namespace:{0}, Type: {1}, BaseType: {2}", type.Namespace, type.FullName, type.BaseType);

                            DumpedEnum dumpedEnum = new DumpedEnum();
                            dumpedEnum.Name = type.Name;
                            dumpedEnum.Values = new List<DumpedEnumValue>();

                            MemberInfo[] memberInfos = type.GetMembers();
                            Array values = type.GetEnumValues();
                            Console.Error.WriteLine("Values={0}", JsonSerializer.Serialize(values));

                            foreach (MemberInfo memberInfo in memberInfos)
                            {
                                if (memberInfo.Name == "value__" || memberInfo.MemberType != MemberTypes.Field)
                                {
                                    continue;
                                }

                                int idx = Array.IndexOf(type.GetEnumNames(), memberInfo.Name);

                                Console.Error.WriteLine("\t[{0}] {1}.{2} = {3}", idx, type.Name, memberInfo.Name, Convert.ChangeType(values.GetValue(idx), typeof(ulong)));

                                dumpedEnum.Values.Add(new DumpedEnumValue(memberInfo.Name, (ulong) Convert.ChangeType(values.GetValue(idx), typeof(ulong))));
                            }

                            dumpedEnums.Add(dumpedEnum);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Caught Exception: {0}, message={1}", e.GetType(), e.Message);
                    }
                }
            }

            Console.WriteLine(JsonSerializer.Serialize(dumpedEnums));
        }

    }

    class DumpedEnum
    {
        public string Name { get; set; }

        public List<DumpedEnumValue> Values { get; set; }
    // public DumpedEnumValue[] Values = new DumpedEnumValue[] {};
}

    class DumpedEnumValue
    {
        public string Key { get; set; }
        public ulong Value { get; set; }

        public DumpedEnumValue(string key, ulong value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
