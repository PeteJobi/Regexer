using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using static RegexerV2.SyntaxStructure;

namespace RegexerV2
{
    internal class ReferenceResolvingNodeDeserializer(INodeDeserializer innerDeserializer, DeserializeData deserializeData) : INodeDeserializer
    {
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value,
            ObjectDeserializer rootDeserializer)
        {
            deserializeData.CurrentAnchor = reader.Current is MappingStart { Anchor.IsEmpty: false } mappingStart ? mappingStart.Anchor.Value : null;

            if (expectedType != typeof(CyclicRef) || !reader.TryConsume(out mappingStart))
                return innerDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer);

            reader.Consume<Scalar>(); 
            reader.Consume<Scalar>();
            reader.Consume<Scalar>();
            var referenceName = reader.Consume<Scalar>().Value;
            value = deserializeData.Anchors[referenceName];
            return true;
        }
    }

    internal class ReferenceCachingObjectFactory(IObjectFactory fallback, DeserializeData deserializeData) : IObjectFactory
    {
        public object Create(Type type)
        {
            var obj = fallback.Create(type);
            if(deserializeData.CurrentAnchor != null) deserializeData.Anchors.Add(deserializeData.CurrentAnchor, obj);
            return obj;
        }

        public object? CreatePrimitive(Type type)
        {
            return fallback.CreatePrimitive(type);
        }

        public bool GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments)
        {
            return fallback.GetDictionary(descriptor, out dictionary, out genericArguments);
        }

        public Type GetValueType(Type type)
        {
            return fallback.GetValueType(type);
        }

        public void ExecuteOnDeserializing(object value)
        {
            fallback.ExecuteOnDeserializing(value);
        }

        public void ExecuteOnDeserialized(object value)
        {
            fallback.ExecuteOnDeserialized(value);
        }

        public void ExecuteOnSerializing(object value)
        {
            fallback.ExecuteOnSerializing(value);
        }

        public void ExecuteOnSerialized(object value)
        {
            fallback.ExecuteOnSerialized(value);
        }
    }
}
