﻿using System;
using System.IO;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Newtonsoft.Json;

namespace Nest
{
	using MultiSearchCreator = Func<IApiCallDetails, Stream, MultiSearchResponse>;

	public partial interface IElasticClient
	{
		/// <summary>
		/// The multi search API allows to execute several search requests within the same API.
		/// <para> </para>http://www.elasticsearch.org/guide/en/elasticsearch/reference/current/search-multi-search.html
		/// </summary>
		/// <param name="multiSearchSelector">A descriptor that describes the search operations on the multi search api</param>
		IMultiSearchResponse MultiSearch(Func<MultiSearchDescriptor, IMultiSearchRequest> multiSearchSelector);

		/// <inheritdoc/>
		IMultiSearchResponse MultiSearch(IMultiSearchRequest multiSearchRequest);

		/// <inheritdoc/>
		Task<IMultiSearchResponse> MultiSearchAsync(Func<MultiSearchDescriptor, IMultiSearchRequest> multiSearchSelector);

		/// <inheritdoc/>
		Task<IMultiSearchResponse> MultiSearchAsync(IMultiSearchRequest multiSearchRequest);
	}

	public partial class ElasticClient
	{
		/// <inheritdoc />
		public IMultiSearchResponse MultiSearch(Func<MultiSearchDescriptor, IMultiSearchRequest> multiSearchSelector) =>
			this.MultiSearch(multiSearchSelector?.Invoke(new MultiSearchDescriptor()));

		/// <inheritdoc />
		public IMultiSearchResponse MultiSearch(IMultiSearchRequest multiSearchRequest)
		{
			return this.Dispatcher.Dispatch<IMultiSearchRequest, MultiSearchRequestParameters, MultiSearchResponse>(
				multiSearchRequest,
				(p, d) =>
				{
					var converter = CreateMultiSearchDeserializer(multiSearchRequest);
					var serializer = new NestSerializer(this.ConnectionSettings, converter);
					var creator = new MultiSearchCreator((r, s) => serializer.Deserialize<MultiSearchResponse>(s));
					multiSearchRequest.RequestParameters.DeserializationOverride(creator);
					return this.LowLevelDispatch.MsearchDispatch<MultiSearchResponse>(p, d);
				}
			);
		}

		/// <inheritdoc />
		public Task<IMultiSearchResponse> MultiSearchAsync(Func<MultiSearchDescriptor, IMultiSearchRequest> multiSearchSelector) =>
			this.MultiSearchAsync(multiSearchSelector?.Invoke(new MultiSearchDescriptor()));


		/// <inheritdoc />
		public Task<IMultiSearchResponse> MultiSearchAsync(IMultiSearchRequest multiSearchRequest)
		{
			return this.Dispatcher.DispatchAsync<IMultiSearchRequest, MultiSearchRequestParameters, MultiSearchResponse, IMultiSearchResponse>(
				multiSearchRequest,
				(p, d) =>
				{
					var converter = CreateMultiSearchDeserializer(multiSearchRequest);
					var serializer = new NestSerializer(this.ConnectionSettings, converter);
					var creator = new MultiSearchCreator((r, s) => serializer.Deserialize<MultiSearchResponse>(s));
					multiSearchRequest.RequestParameters.DeserializationOverride(creator);
					return this.LowLevelDispatch.MsearchDispatchAsync<MultiSearchResponse>(p, d);
				}
			);
		}

		private JsonConverter CreateMultiSearchDeserializer(IMultiSearchRequest request)
		{
			if (request.Operations != null)
			{
				foreach (var operation in request.Operations.Values)
					CovariantSearch.CloseOverAutomagicCovariantResultSelector(this.Infer, operation);
			}

			return new MultiSearchResponseJsonConverter(this.ConnectionSettings, request);
		}
	}
}