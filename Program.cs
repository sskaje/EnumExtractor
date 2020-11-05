using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace EnumExtractor
{
    class Program
    {

        static void Usage()
        {
            Console.WriteLine("Usage: {0} DLL_FILE ENUM_FILTER", Environment.GetCommandLineArgs()[0]);
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
                Console.Write("Enter External Assembly:");
                string tmpFileName = Console.ReadLine();
                if (File.Exists(tmpFileName))
                {
                    fileName = tmpFileName;
                }
            }

            if (String.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Error: Invalid File");
                Usage();
            }




            try
            {
                Assembly asm = Assembly.LoadFrom(fileName);
                DispalyAssembly(asm, enumFilter);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Load Assembly " + e.GetType() + " message=" + e.Message);
            }


        }

        static void DispalyAssembly(Assembly a, string enumFilter)
        {
            Console.WriteLine("*******Contents in Assembly*********");
            Console.WriteLine("Information:{0}", a.FullName);
            
            Module[] mod = a.GetModules();
            foreach (Module m in mod)
            {
                Console.WriteLine("Module: {0}", m);
            }

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
                            Console.WriteLine("Namespace:{0}, Type: {1}, BaseType: {2}", type.Namespace, type.FullName, type.BaseType);

                            MemberInfo[] memberInfos = type.GetMembers();
                            Array values = type.GetEnumValues();
                            Console.WriteLine("Values={0}", JsonSerializer.Serialize(values));

                            foreach (MemberInfo memberInfo in memberInfos)
                            {
                                if (memberInfo.Name == "value__" || memberInfo.MemberType != MemberTypes.Field)
                                {
                                    continue;
                                }

                                int idx = Array.IndexOf(type.GetEnumNames(), memberInfo.Name);

                                Console.WriteLine("\t[{0}] {1}.{2} = {3}", idx, type.Name, memberInfo.Name, Convert.ChangeType(values.GetValue(idx), typeof(ulong)));

                            }

                            /*
                            var arrayFields = System.Type.GetType(type.FullName).GetFields(BindingFlags.Public | BindingFlags.Static);
                            foreach (var field in arrayFields)
                            {
                                Console.WriteLine("\t{0} = {1}", field.Name, field.GetValue(null));
                            }
                            */

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Caught Exception: {0}, message={1}", e.GetType(), e.Message);
                    }
                }
            }
        }
    }
}
