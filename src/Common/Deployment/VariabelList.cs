using Ricotta.Common.Expressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace Common.Deployment
{
    public class VariableList
    {
        private class VariableValue
        {
            public string value { get; set; }
            public string env { get; set; }
            public string role { get; set; }
            public string step_id { get; set; }
            public string step_tag { get; set; }
        }

        private string _environment;

        private Dictionary<string, List<VariableValue>> _variables = new Dictionary<string, List<VariableValue>>();

        public VariableList(string environment)
        {
            _environment = environment;
        }

        public void ReadFromYamlFile(string fileName)
        {
            var yamlContent = File.ReadAllText(fileName);
            var deserializer = new DeserializerBuilder().Build();
            var variables = deserializer.Deserialize<Dictionary<string, List<VariableValue>>>(yamlContent);

            foreach (var variable in variables)
            {
                var variableValueList = new List<VariableValue>();
                foreach (var variableValue in variable.Value)
                {
                    // TODO: env in environment specific files like vars.prod.yml ideally should not have expressions like: dev | prod
                    if (variableValue.env == null || new EnvironmentExpression().Evaluate(_environment, variableValue.env))
                    {
                        variableValue.env = null;   // Remove env
                        variableValueList.Add(variableValue);
                    }
                }
                if (_variables.ContainsKey(variable.Key))
                {
                    _variables[variable.Key].AddRange(variableValueList);
                }
                else
                {
                    _variables[variable.Key] = variableValueList;
                }
            }
        }

        public string ToYaml()
        {
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(_variables);
        }

        public string ToYaml(TargetExpression target)
        {
            var filteredVariables = new Dictionary<string, List<VariableValue>>();
            foreach (var variable in _variables)
            {
                var variableValueList = new List<VariableValue>();
                foreach (var variableValue in variable.Value)
                {
                    if (variableValue.role == null || target.Evaluate(_environment, variableValue.role))
                    {
                        //variableValue.role = null; Note: Don't remove role like environment as it is necessary to calculate weight
                        variableValueList.Add(variableValue);
                    }
                }
                if (filteredVariables.ContainsKey(variable.Key))
                {
                    filteredVariables[variable.Key].AddRange(variableValueList);
                }
                else
                {
                    filteredVariables[variable.Key] = variableValueList;
                }
            }
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(filteredVariables);
        }

        //// Parameter target is for checking if role matches agent
        //public string GetValue(string name, Step step)
        //{
        //    if (_variables.ContainsKey(name))
        //    {
        //        return GetValue(_variables[name], step);
        //    }
        //    return null;
        //}

        //private string GetValue(List<VariableValue> values, Step step)
        //{
        //}

    }

}
