using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace com.tikumo.regdiff
{
    public enum Presence
    {
        Optional = 0,
        Required
    }

    public enum InputArgType
    {
        Flag = 0,
        Parameter,
        ExistingDirectory,
        RemainingParameters,
        StringList,
        Enum,
        SizeInBytes
    }

    public class InputArgs
    {
        internal class InputArg
        {
            public readonly InputArgType Type;
            public readonly string Name;
            public readonly Presence Presence;
            public readonly string HelpString;
            public bool HasBeenSeen;
            public object Value;

            internal InputArg(InputArgType type, string name, object defaultValue, Presence presence, string helpString)
            {
                HasBeenSeen = false;
                Type = type;
                Name = name;
                Value = defaultValue;
                Presence = presence;
                HelpString = helpString;
                HasBeenSeen = false;                
            }
        }

        private List<InputArg> Options = new List<InputArg>();
        private string Caption;
        private string AppName;
        private StringComparison SCType;
        private ProcessArg CurrentArgMethod;
        private InputArg ExpectedArg;
        private InputArg RemainingArgs;

        public InputArgs(string appName, string caption)
        {
            AppName = appName;
            Caption = caption;
            SCType = StringComparison.OrdinalIgnoreCase;
        }

        public void Add(InputArgType type, string name, object defaultvalue, Presence presence, string helpString)
        {
            Options.Add(new InputArg(type, name, defaultvalue, presence, helpString));
        }

        public bool Process(string[] args)
        {
            Console.WriteLine("{0} - {1}\r\n", AppName, Caption);
            
            RemainingArgs = ExpectRemainingParameters();
            CurrentArgMethod = DefaultProcessFunc;
            ExpectedArg = null;


            foreach (string arg in args)
            {
                if (!CurrentArgMethod(arg))
                    return false;
            }

            if (!AreAllRequiredArgumentsPresent())
                return false;

            return true;
        }

        private delegate bool ProcessArg(string arg);

        private bool MatchesOption(string arg, InputArg option)
        {
            return option.Name.Equals(arg, SCType);
        }

        private bool HandleRemainingParameters(string arg)
        {
            if (RemainingArgs != null)
            {
                RemainingArgs.HasBeenSeen = true;
                if (RemainingArgs.Value == null)
                {
                    RemainingArgs.Value = new List<string>();
                }
                (RemainingArgs.Value as List<string>).Add(arg);
            }
            return true;
        }

        private bool DefaultProcessFunc(string arg)
        {
            if (arg.StartsWith("/"))
            {
                arg = arg.Substring(1);
            }
            else if (arg.StartsWith("--"))
            {
                arg = arg.Substring(2);
            }
            else if (RemainingArgs != null)
            {
                return HandleRemainingParameters(arg);
            }

            if( arg.Equals("?") || arg.Equals("help", SCType) )
                return Help();

            foreach (InputArg option in Options)
            {
                if (MatchesOption(arg, option))
                {
                    return OnProcessOption(option, arg);
                }
            }
            Console.WriteLine("Error, argument '{0}' is invalid.", arg);
            return false;
        }

        private bool ExpectParameter(string arg)
        {
            ExpectedArg.Value = arg;
            ExpectedArg.HasBeenSeen = true;
            CurrentArgMethod = DefaultProcessFunc;
            return true;
        }

        private bool ExpectStringList(string arg)
        {
            List<string> items = new List<string>();
            foreach (string s in arg.Split(';'))
            {
                items.Add(s);
            }
            ExpectedArg.Value = items;
            ExpectedArg.HasBeenSeen = true;
            CurrentArgMethod = DefaultProcessFunc;
            return true;
        }

        private bool ExpectNewDirectory(string arg)
        {
            ExpectedArg.Value = arg;
            ExpectedArg.HasBeenSeen = true;
            if (!Directory.Exists(arg))
            {
                Console.WriteLine("ERROR, argument {0} must specify an existing directory, '{1}' does not exist.", ExpectedArg.Name, arg);
                return false;
            }
            CurrentArgMethod = DefaultProcessFunc;
            return true;
        }

        private bool ExpectSizeInBytes(string arg)
        {
            decimal sizeAsDecimal = 0.0m;
            string digits = "0123456789";
            bool recording_fraction = false;
            decimal fraction_divisor = 0.0m;
            bool expectByte = false;

            for (int i = 0; i < arg.Length; ++i)
            {
                char c = arg[i];
                int digit = digits.IndexOf(c);

                if (expectByte)
                {
                    if ((c == 'b') || (c == 'B'))
                    {
                        expectByte = true;
                    }
                    else
                    {
                        Console.WriteLine("ERROR, '{0}' is not a valid size indicator", arg);
                        return false;
                    }
                }
                else if (digit >= 0)
                {
                    if (recording_fraction)
                    {
                        sizeAsDecimal += ((decimal)digit) / fraction_divisor;
                        fraction_divisor *= 10.0m;
                    }
                    sizeAsDecimal *= 10.0m;
                    sizeAsDecimal += digit;
                }
                else if( c == '.' )
                {
                    recording_fraction = true;
                    fraction_divisor = 10.0m;
                }
                else if ((c == 'k') || (c == 'K'))
                {
                    sizeAsDecimal *= 1024;
                    expectByte = true;
                }
                else if ((c == 'm') || (c == 'm'))
                {
                    sizeAsDecimal *= 1024 * 1024;
                    expectByte = true;
                }
                else if ((c == 'g') || (c == 'g'))
                {
                    sizeAsDecimal *= 1024 * 1024 * 1024;
                    expectByte = true;
                }
                else if (c != ' ')
                {
                    Console.WriteLine("ERROR, '{0}' is not a valid size indicator", arg);
                    return false;
                }
            }
            long sizeInBytes = (long)sizeAsDecimal;
            ExpectedArg.Value = sizeInBytes;
            ExpectedArg.HasBeenSeen = true;
            CurrentArgMethod = DefaultProcessFunc;
            return true;
        }

        internal virtual bool OnProcessOption(InputArg option, string arg)
        {
            switch (option.Type)
            {
                case InputArgType.ExistingDirectory:
                    ExpectedArg = option;
                    CurrentArgMethod = ExpectNewDirectory;
                    return true;

                case InputArgType.SizeInBytes:
                    ExpectedArg = option;
                    CurrentArgMethod = ExpectSizeInBytes;
                    return true;

                case InputArgType.Parameter:
                    ExpectedArg = option;
                    CurrentArgMethod = ExpectParameter;
                    return true;

                case InputArgType.StringList:
                    ExpectedArg = option;
                    CurrentArgMethod = ExpectStringList;
                    return true;

                case InputArgType.Flag:
                    option.Value = true;
                    option.HasBeenSeen = true;
                    CurrentArgMethod = DefaultProcessFunc;
                    return true;

                default:
                    Console.WriteLine("Error, argument type {0} not implemented yet.", option.Type);
                    return false;
            }
        }

        private bool AreAllRequiredArgumentsPresent()
        {
            bool success = true;
            foreach (InputArg arg in Options)
            {
                if ((Presence.Required == arg.Presence) && !arg.HasBeenSeen)
                {
                    Console.WriteLine("Error, required argument {0} missing.", arg.Name);
                    success = false;
                }
            }
            return success;
        }

        private InputArg ExpectRemainingParameters()
        {
            foreach (InputArg arg in Options)
            {
                if (arg.Type == InputArgType.RemainingParameters)
                {
                    return arg;
                }
            }
            return null;
        }

        private bool Help()
        {
            InputArg remainingParameters = ExpectRemainingParameters();
            if( remainingParameters == null )
                Console.WriteLine("USAGE: {0}.EXE [OPTIONS]", AppName);
            else
                Console.WriteLine("USAGE: {0}.EXE [OPTIONS] {1}.", AppName, remainingParameters.Name);
            Console.WriteLine("OPTIONS:");
            foreach (InputArg arg in Options)
            {
                if( arg != remainingParameters )
                    Console.WriteLine("{0,14}: {1}", "/" + arg.Name.ToUpper(), string.Format(arg.HelpString, arg.Value));
            }
            if( remainingParameters != null )
                Console.WriteLine("{0,14}: {1}", remainingParameters.Name, remainingParameters.HelpString);

            return false;
        }

        private InputArg GetOptionByName(string argName)
        {
            foreach (InputArg option in Options)
            {
                if (MatchesOption(argName, option))
                {
                    return option;
                }
            }
            throw new KeyNotFoundException(string.Format("Argument '{0}' is undefined", argName));
        }

        public object GetValue(string argName)
        {
            return GetOptionByName(argName).Value;
        }

        public string GetString(string argName)
        {
            return (string) GetValue(argName);
        }

        public bool GetFlag(string argName)
        {
            return (bool)GetValue(argName);
        }

        public long GetSizeInBytes(string argName)
        {
            object result = GetValue(argName);
            if( result is int )
                return (long)(int)result;

            return (long)result;
        }

        public List<string> FindOrCreateStringList(string argName)
        {
            InputArg option = GetOptionByName(argName);
            if (option.Value == null)
                option.Value = new List<string>();
            return (List<string>)option.Value;
        }

        public List<string> GetStringList(string argName)
        {
            object temp = GetValue(argName);
            if( temp is List<string> )
                return temp as List<string>;

            if( temp is string )
            {
                List<string> result = new List<string>();
                result.Add(temp as string);
                return result;
            }
            return null;
        }
        
    }
}
