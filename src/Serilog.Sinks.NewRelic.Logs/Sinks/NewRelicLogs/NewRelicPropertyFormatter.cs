﻿using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Serilog.Sinks.NewRelic.Logs
{
    public static class NewRelicPropertyFormatter
    {
        private static readonly HashSet<Type> LogScalars = new HashSet<Type>
        {
            typeof (bool),
            typeof (byte),
            typeof (short),
            typeof (ushort),
            typeof (int),
            typeof (uint),
            typeof (long),
            typeof (ulong),
            typeof (float),
            typeof (double),
            typeof (decimal),
            typeof (byte[])
        };

        public static object Simplify(LogEventPropertyValue value)
        {
            switch (value)
            {
                case ScalarValue scalar:
                    return SimplifyScalar(scalar.Value);

                case DictionaryValue dictionary:
                {
                    var result = new Dictionary<object, object>();
                    foreach (var element in dictionary.Elements)
                    {
                        var key = SimplifyScalar(element.Key.Value);
                        if (result.ContainsKey(key))
                        {
                            Trace.WriteLine($"The key {element.Key} is not unique in the provided dictionary after simplification to {key}.");

                            return dictionary.Elements.Select(e => new Dictionary<string, object>
                            {
                                { "Key", SimplifyScalar(e.Key.Value) },
                                { "Value", Simplify(e.Value) }
                            }).ToArray();
                        }
                        result.Add(key, Simplify(element.Value));
                    }

                    return result;
                }
                case SequenceValue sequence:
                    return sequence.Elements.Select(Simplify).ToArray();

                case StructureValue structure:
                {
                    var props = structure.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));
                    if (structure.TypeTag != null)
                    {
                        props["$typeTag"] = structure.TypeTag;
                    }
                    return props;
                }

                default:
                    return null;
            }
        }

        private static object SimplifyScalar(object value)
        {
            if (value == null)
            {
                return null;
            }

            var valueType = value.GetType();
            return LogScalars.Contains(valueType) ? value : value.ToString();
        }
    }
}