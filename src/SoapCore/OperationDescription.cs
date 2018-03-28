using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

namespace SoapCore
{
	public interface IBodySerializer
	{
		object Deserialize(XmlDictionaryReader reader);
	}
	class XmlBodySerializer : IBodySerializer
	{
		XmlSerializer m_Serializer;
		public XmlBodySerializer(XmlSerializer serializer)
		{
			m_Serializer = serializer;
		}
		public object Deserialize(XmlDictionaryReader reader)
		{
			return m_Serializer.Deserialize(reader);
		}
	}
	class DataContractBodySerializer : IBodySerializer
	{
		DataContractSerializer m_Serializer;
		public DataContractBodySerializer(DataContractSerializer serializer)
		{
			m_Serializer = serializer;
		}

		public object Deserialize(XmlDictionaryReader reader)
		{
			return m_Serializer.ReadObject(reader, true);
		}
	}
	public class BodySerializerFactory
	{
		ConcurrentDictionary<string, IBodySerializer> Serializers = new ConcurrentDictionary<string, IBodySerializer>();
		public IBodySerializer Get(SoapSerializer serializerKind, string name, string ns, Type t)
		{
			var key = $"{ns}_{name}_{(int)serializerKind}";
			return Serializers.GetOrAdd(key, k =>
			{
				switch ((int)serializerKind)
				{
					case (int)SoapSerializer.DataContractSerializer:
						return new DataContractBodySerializer(new DataContractSerializer(t, name, ns));
					case (int)SoapSerializer.XmlSerializer:
						return new XmlBodySerializer(new XmlSerializer(t, null, new Type[0], new XmlRootAttribute(name), ns));
					default:
						throw new InvalidOperationException($"unknown serializer kind({serializerKind})");
				}
			});
		}
	}
	public class OperationDescription
	{
		public ContractDescription Contract { get; private set; }
		public string SoapAction { get; private set; }
		public string ReplyAction { get; private set; }
		public string Name { get; private set; }
		public MethodInfo DispatchMethod { get; private set; }
		public bool IsOneWay { get; private set; }
		public bool IsReturnTask { get; private set; }
		public PropertyInfo TaskResultPropertyInfo { get; private set; }
		public PropertyInfo HeaderProperty { get; private set; }
		public ParameterInfo[] Parameters { get; private set; }
		public ParameterInfo[] OutParameters { get; private set; }

		public BodySerializerFactory SoapSerializerFactory { get; private set; }

		public OperationDescription(ContractDescription contract, MethodInfo operationMethod, OperationContractAttribute contractAttribute)
		{
			Contract = contract;
			Name = contractAttribute.Name ?? operationMethod.Name;
			SoapAction = contractAttribute.Action ?? $"{contract.Namespace.TrimEnd('/')}/{contract.Name}/{Name}";
			IsOneWay = contractAttribute.IsOneWay;
			ReplyAction = contractAttribute.ReplyAction;
			DispatchMethod = operationMethod;
			if (DispatchMethod.ReturnType.IsConstructedGenericType && DispatchMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				IsReturnTask = true;
			}
			TaskResultPropertyInfo = DispatchMethod.ReturnType.GetProperty("Result");
			Parameters = DispatchMethod.GetParameters().Where(x => !x.IsOut && !x.ParameterType.IsByRef).ToArray();
			OutParameters = DispatchMethod.GetParameters().Where(x => x.IsOut || x.ParameterType.IsByRef).ToArray();
			SoapSerializerFactory = new BodySerializerFactory();
		}
	}
}
