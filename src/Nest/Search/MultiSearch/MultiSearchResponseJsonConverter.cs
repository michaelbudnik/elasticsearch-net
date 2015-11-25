﻿using Elasticsearch.Net.Serialization;
using Nest.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nest
{
	internal class MultiSearchResponseJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => objectType == typeof(MultiSearchResponse);
		public override bool CanWrite => false;
		public override bool CanRead => true;

		private readonly IMultiSearchRequest _request;

		private static MethodInfo MakeDelegateMethodInfo = typeof(MultiSearchResponseJsonConverter).GetMethod("CreateMultiHit", BindingFlags.Static | BindingFlags.NonPublic);
		private readonly IConnectionSettingsValues _settings;
		internal MultiSearchResponseJsonConverter()
		{

		}
		public MultiSearchResponseJsonConverter(IConnectionSettingsValues settings, IMultiSearchRequest request)
		{
			this._settings = settings;
			_request = request;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (this._settings == null)
			{
				var realConverter = (
					(serializer.ContractResolver as SettingsContractResolver)
					?.PiggyBackState?.ActualJsonConverter as MultiSearchResponseJsonConverter
				);
				if (realConverter == null)
					throw new DslException("could not find a stateful multi search converter");

				var mr = realConverter.ReadJson(reader, objectType, existingValue, serializer) as MultiSearchResponse;
				return mr;
			}


			var response = new MultiSearchResponse();
			var jsonObject = JObject.Load(reader);

			var docsJarray = (JArray)jsonObject["responses"];
			if (docsJarray == null)
				return response;
			var multiSearchDescriptor = this._request;
			if (this._request == null)
				return multiSearchDescriptor;

			var withMeta = docsJarray.Zip(this._request.Operations, (doc, desc) => new MultiHitTuple { Hit = doc, Descriptor = desc });
			var originalResolver = serializer.ContractResolver;
			foreach (var m in withMeta)
			{
				var descriptor = m.Descriptor.Value;
				var concreteTypeSelector = descriptor.TypeSelector;
				var baseType = m.Descriptor.Value.ClrType ?? typeof(object);
				
				var generic = MakeDelegateMethodInfo.MakeGenericMethod(baseType);

				if (concreteTypeSelector != null)
				{
					var elasticSerializer = new NestSerializer(this._settings);
					var state = typeof(ConcreteTypeConverter<>).CreateGenericInstance(baseType, concreteTypeSelector) as JsonConverter;
					if (state != null)
					{
						var settings = elasticSerializer.CreateSettings(SerializationFormatting.None, piggyBackJsonConverter: state);

						var jsonSerializer = new JsonSerializer()
						{
							NullValueHandling = settings.NullValueHandling,
							DefaultValueHandling = settings.DefaultValueHandling,
							ContractResolver = settings.ContractResolver,
						};
						foreach (var converter in settings.Converters.EmptyIfNull())
							jsonSerializer.Converters.Add(converter);
						generic.Invoke(null, new object[] { m, jsonSerializer, response._Responses, this._settings });
						continue;
					}
				}
				generic.Invoke(null, new object[] { m, serializer, response._Responses, this._settings });
			}
			
			return response;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}

		private class MultiHitTuple
		{
			public JToken Hit { get; set; }
			public KeyValuePair<string, ISearchRequest> Descriptor { get; set; }
		}

		private static void CreateMultiHit<T>(
			MultiHitTuple tuple, 
			JsonSerializer serializer, 
			IDictionary<string, object> collection, 
			IConnectionSettingsValues settings
		)
			where T : class
		{
			var hit = new SearchResponse<T>();
			var reader = tuple.Hit.CreateReader();
			serializer.Populate(reader, hit);

			var errorProperty = tuple.Hit.Children<JProperty>().FirstOrDefault(c=>c.Name == "error");
			if (errorProperty != null)
			{
				// TODO: set error data
				// can't set hit.ApiCall, because it will get overwritten in MultiSearchResponse.GetResponses<T>
				// can't set IsValid to false, because it depends on ApiCall
				// the same applies to ServerError
				// hit.IsValid = false;
				// hit.ApiCall = ???
				// hit.ServerError = ???
			}

			collection.Add(tuple.Descriptor.Key, hit);
		}
	}
}
