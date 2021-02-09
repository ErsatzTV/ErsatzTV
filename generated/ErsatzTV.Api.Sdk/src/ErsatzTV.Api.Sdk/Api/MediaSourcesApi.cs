/*
 * ErsatzTV API
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: v1
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Mime;
using ErsatzTV.Api.Sdk.Client;
using ErsatzTV.Api.Sdk.Model;

namespace ErsatzTV.Api.Sdk.Api
{

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IMediaSourcesApiSync : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <returns>List&lt;MediaSourceViewModel&gt;</returns>
        List<MediaSourceViewModel> ApiMediaSourcesGet();

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <returns>ApiResponse of List&lt;MediaSourceViewModel&gt;</returns>
        ApiResponse<List<MediaSourceViewModel>> ApiMediaSourcesGetWithHttpInfo();
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <returns>MediaSourceViewModel</returns>
        MediaSourceViewModel ApiMediaSourcesIdGet(int id);

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <returns>ApiResponse of MediaSourceViewModel</returns>
        ApiResponse<MediaSourceViewModel> ApiMediaSourcesIdGetWithHttpInfo(int id);
        #endregion Synchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IMediaSourcesApiAsync : IApiAccessor
    {
        #region Asynchronous Operations
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;MediaSourceViewModel&gt;</returns>
        System.Threading.Tasks.Task<List<MediaSourceViewModel>> ApiMediaSourcesGetAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;MediaSourceViewModel&gt;)</returns>
        System.Threading.Tasks.Task<ApiResponse<List<MediaSourceViewModel>>> ApiMediaSourcesGetWithHttpInfoAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of MediaSourceViewModel</returns>
        System.Threading.Tasks.Task<MediaSourceViewModel> ApiMediaSourcesIdGetAsync(int id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (MediaSourceViewModel)</returns>
        System.Threading.Tasks.Task<ApiResponse<MediaSourceViewModel>> ApiMediaSourcesIdGetWithHttpInfoAsync(int id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IMediaSourcesApi : IMediaSourcesApiSync, IMediaSourcesApiAsync
    {

    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class MediaSourcesApi : IMediaSourcesApi
    {
        private ErsatzTV.Api.Sdk.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSourcesApi"/> class.
        /// </summary>
        /// <returns></returns>
        public MediaSourcesApi() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSourcesApi"/> class.
        /// </summary>
        /// <returns></returns>
        public MediaSourcesApi(String basePath)
        {
            this.Configuration = ErsatzTV.Api.Sdk.Client.Configuration.MergeConfigurations(
                ErsatzTV.Api.Sdk.Client.GlobalConfiguration.Instance,
                new ErsatzTV.Api.Sdk.Client.Configuration { BasePath = basePath }
            );
            this.Client = new ErsatzTV.Api.Sdk.Client.ApiClient(this.Configuration.BasePath);
            this.AsynchronousClient = new ErsatzTV.Api.Sdk.Client.ApiClient(this.Configuration.BasePath);
            this.ExceptionFactory = ErsatzTV.Api.Sdk.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSourcesApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="configuration">An instance of Configuration</param>
        /// <returns></returns>
        public MediaSourcesApi(ErsatzTV.Api.Sdk.Client.Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Configuration = ErsatzTV.Api.Sdk.Client.Configuration.MergeConfigurations(
                ErsatzTV.Api.Sdk.Client.GlobalConfiguration.Instance,
                configuration
            );
            this.Client = new ErsatzTV.Api.Sdk.Client.ApiClient(this.Configuration.BasePath);
            this.AsynchronousClient = new ErsatzTV.Api.Sdk.Client.ApiClient(this.Configuration.BasePath);
            ExceptionFactory = ErsatzTV.Api.Sdk.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSourcesApi"/> class
        /// using a Configuration object and client instance.
        /// </summary>
        /// <param name="client">The client interface for synchronous API access.</param>
        /// <param name="asyncClient">The client interface for asynchronous API access.</param>
        /// <param name="configuration">The configuration object.</param>
        public MediaSourcesApi(ErsatzTV.Api.Sdk.Client.ISynchronousClient client, ErsatzTV.Api.Sdk.Client.IAsynchronousClient asyncClient, ErsatzTV.Api.Sdk.Client.IReadableConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (asyncClient == null) throw new ArgumentNullException("asyncClient");
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Client = client;
            this.AsynchronousClient = asyncClient;
            this.Configuration = configuration;
            this.ExceptionFactory = ErsatzTV.Api.Sdk.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// The client for accessing this underlying API asynchronously.
        /// </summary>
        public ErsatzTV.Api.Sdk.Client.IAsynchronousClient AsynchronousClient { get; set; }

        /// <summary>
        /// The client for accessing this underlying API synchronously.
        /// </summary>
        public ErsatzTV.Api.Sdk.Client.ISynchronousClient Client { get; set; }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public String GetBasePath()
        {
            return this.Configuration.BasePath;
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public ErsatzTV.Api.Sdk.Client.IReadableConfiguration Configuration { get; set; }

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public ErsatzTV.Api.Sdk.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <returns>List&lt;MediaSourceViewModel&gt;</returns>
        public List<MediaSourceViewModel> ApiMediaSourcesGet()
        {
            ErsatzTV.Api.Sdk.Client.ApiResponse<List<MediaSourceViewModel>> localVarResponse = ApiMediaSourcesGetWithHttpInfo();
            return localVarResponse.Data;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <returns>ApiResponse of List&lt;MediaSourceViewModel&gt;</returns>
        public ErsatzTV.Api.Sdk.Client.ApiResponse<List<MediaSourceViewModel>> ApiMediaSourcesGetWithHttpInfo()
        {
            ErsatzTV.Api.Sdk.Client.RequestOptions localVarRequestOptions = new ErsatzTV.Api.Sdk.Client.RequestOptions();

            String[] _contentTypes = new String[] {
            };

            // to determine the Accept header
            String[] _accepts = new String[] {
                "application/json"
            };

            var localVarContentType = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);



            // make the HTTP request
            var localVarResponse = this.Client.Get<List<MediaSourceViewModel>>("/api/media/sources", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ApiMediaSourcesGet", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;MediaSourceViewModel&gt;</returns>
        public async System.Threading.Tasks.Task<List<MediaSourceViewModel>> ApiMediaSourcesGetAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            ErsatzTV.Api.Sdk.Client.ApiResponse<List<MediaSourceViewModel>> localVarResponse = await ApiMediaSourcesGetWithHttpInfoAsync(cancellationToken).ConfigureAwait(false);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;MediaSourceViewModel&gt;)</returns>
        public async System.Threading.Tasks.Task<ErsatzTV.Api.Sdk.Client.ApiResponse<List<MediaSourceViewModel>>> ApiMediaSourcesGetWithHttpInfoAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {

            ErsatzTV.Api.Sdk.Client.RequestOptions localVarRequestOptions = new ErsatzTV.Api.Sdk.Client.RequestOptions();

            String[] _contentTypes = new String[] {
            };

            // to determine the Accept header
            String[] _accepts = new String[] {
                "application/json"
            };


            var localVarContentType = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);



            // make the HTTP request

            var localVarResponse = await this.AsynchronousClient.GetAsync<List<MediaSourceViewModel>>("/api/media/sources", localVarRequestOptions, this.Configuration, cancellationToken).ConfigureAwait(false);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ApiMediaSourcesGet", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <returns>MediaSourceViewModel</returns>
        public MediaSourceViewModel ApiMediaSourcesIdGet(int id)
        {
            ErsatzTV.Api.Sdk.Client.ApiResponse<MediaSourceViewModel> localVarResponse = ApiMediaSourcesIdGetWithHttpInfo(id);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <returns>ApiResponse of MediaSourceViewModel</returns>
        public ErsatzTV.Api.Sdk.Client.ApiResponse<MediaSourceViewModel> ApiMediaSourcesIdGetWithHttpInfo(int id)
        {
            ErsatzTV.Api.Sdk.Client.RequestOptions localVarRequestOptions = new ErsatzTV.Api.Sdk.Client.RequestOptions();

            String[] _contentTypes = new String[] {
            };

            // to determine the Accept header
            String[] _accepts = new String[] {
                "application/json"
            };

            var localVarContentType = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("id", ErsatzTV.Api.Sdk.Client.ClientUtils.ParameterToString(id)); // path parameter


            // make the HTTP request
            var localVarResponse = this.Client.Get<MediaSourceViewModel>("/api/media/sources/{id}", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ApiMediaSourcesIdGet", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of MediaSourceViewModel</returns>
        public async System.Threading.Tasks.Task<MediaSourceViewModel> ApiMediaSourcesIdGetAsync(int id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            ErsatzTV.Api.Sdk.Client.ApiResponse<MediaSourceViewModel> localVarResponse = await ApiMediaSourcesIdGetWithHttpInfoAsync(id, cancellationToken).ConfigureAwait(false);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="ErsatzTV.Api.Sdk.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (MediaSourceViewModel)</returns>
        public async System.Threading.Tasks.Task<ErsatzTV.Api.Sdk.Client.ApiResponse<MediaSourceViewModel>> ApiMediaSourcesIdGetWithHttpInfoAsync(int id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {

            ErsatzTV.Api.Sdk.Client.RequestOptions localVarRequestOptions = new ErsatzTV.Api.Sdk.Client.RequestOptions();

            String[] _contentTypes = new String[] {
            };

            // to determine the Accept header
            String[] _accepts = new String[] {
                "application/json"
            };


            var localVarContentType = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = ErsatzTV.Api.Sdk.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("id", ErsatzTV.Api.Sdk.Client.ClientUtils.ParameterToString(id)); // path parameter


            // make the HTTP request

            var localVarResponse = await this.AsynchronousClient.GetAsync<MediaSourceViewModel>("/api/media/sources/{id}", localVarRequestOptions, this.Configuration, cancellationToken).ConfigureAwait(false);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ApiMediaSourcesIdGet", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

    }
}
