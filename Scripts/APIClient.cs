﻿using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;
using WWWForm = UnityEngine.WWWForm;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;

using ModIO.API;

namespace ModIO
{
    /// <summary>
    /// This class provides a native wrapper for each of the endpoints available via the mod.io web
    /// API.
    /// </summary>
    public static class APIClient
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>
        /// Denotes which version of the mod.io web API that this class is compatible with.
        /// </summary>
        /// <remarks>
        /// This value forms part of the web API URL and should not be changed.
        /// </remarks>
        public const string API_VERSION = "v1";

        /// <summary>
        /// The base URL for the web API
        /// </summary>
        #if DEBUG
        public static readonly string API_URL = (GlobalSettings.USE_TEST_SERVER
                                                 ? "https://api.test.mod.io/"
                                                 : "https://api.mod.io/") + API_VERSION;
        #else
        public const string API_URL = "https://api.mod.io/" + API_VERSION;
        #endif

        /// <summary>
        /// Collection of the HTTP request header keys used by Unity
        /// </summary>
        /// <remarks>
        /// Used almost exclusively for debugging requests.
        /// </remarks>
        public static readonly string[] UNITY_REQUEST_HEADER_KEYS = new string[]
        {
            // - UNIVERSAL -
            "accept-charset",
            "access-control-request-headers",
            "access-control-request-method",
            "connection",
            "cookie",
            "cookie2",
            "date",
            "dnt",
            "expect",
            "host",
            "keep-alive",
            "origin",
            "referer",
            "te",
            "trailer",
            "transfer-encoding",
            "upgrade",
            "via",
            // - UNITY -
            "accept-encoding",
            "Content-Type",
            "content-length",
            "x-unity-version",
            "user-agent",
        };
        /// <summary>
        /// Collection of the HTTP request header keys used by mod.io
        /// </summary>
        /// <remarks>
        /// Used almost exclusively for debugging requests.
        /// </remarks>
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "Authorization",
            "Accept-Language",
        };

        // ---------[ MEMBERS ]---------
        /// <summary>
        /// Game ID that the APIClient should use when contacting the API
        /// </summary>
        public static int gameId = GlobalSettings.GAME_ID;

        /// <summary>
        /// Game API that the APIClient should use when contacting the API
        /// </summary>
        public static string gameAPIKey = GlobalSettings.GAME_APIKEY;

        /// <summary>
        /// The user's OAuthToken that the APIClient should include when contacting the API
        /// </summary>
        public static string userAuthorizationToken = null;

        /// <summary>
        /// The language code that designates requested language for the API response messages
        /// </summary>
        /// <remarks>
        /// Currently supported languages and codes are listed in the mod.io documentation under
        /// <a href="https://docs.mod.io/#localization">Localization</a>.
        /// </remarks>
        public static string languageCode = "en";

        // ---------[ DEBUG ASSERTS ]---------
        /// <summary>
        /// Asserts that the required authorization data for making API requests is set.
        /// </summary>
        /// <remarks>
        /// Only asserts that the values have been set, but **does not check the correctness** of
        /// those values.
        /// </remarks>
        /// <param name="isUserTokenRequired">
        /// Whether to assert that [[ModIO.APIClient.userAuthorizationToken]] is set.
        /// </param>
        /// <returns>
        /// True if the authorization details for an API request have been set.
        /// </returns>
        public static bool AssertAuthorizationDetails(bool isUserTokenRequired)
        {
            if(APIClient.gameId <= 0
               || String.IsNullOrEmpty(APIClient.gameAPIKey))
            {
                Debug.LogError("[mod.io] No API requests can be executed without a"
                               + " valid Game Id and Game API Key. These need to be"
                               + " saved in ModIO.GlobalSettings or"
                               + " set directly on the ModIO.APIClient before"
                               + " any requests can be sent to the API.");
                return false;
            }

            if(isUserTokenRequired
               && String.IsNullOrEmpty(APIClient.userAuthorizationToken))
            {
                Debug.LogError("[mod.io] API request to modification or User-specific"
                               + " endpoints cannot be made without first setting the"
                               + " User Authorization Token on the ModIO.APIClient.");
                return false;
            }

            return true;
        }

        // ---------[ REQUEST HANDLING ]---------
        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint request that requires no user authentication
        /// </summary>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="filterString">Filter string to be appended to the endpoint URL</param>
        /// <param name="pagination">Pagination data for the request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    string filterString,
                                                    PaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(false);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string queryURL = (endpointURL
                               + "?" + filterString
                               + paginationString);

            if(APIClient.userAuthorizationToken == null)
            {
                queryURL += "&api_key=" + APIClient.gameAPIKey;
            }

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);
            if(APIClient.userAuthorizationToken != null)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            }
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                Debug.Log("GENERATING QUERY"
                          + "\nEndpoint: " + queryURL
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'GET' request
        /// </summary>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="filterString">Filter string to be appended to the endpoint URL</param>
        /// <param name="pagination">Pagination data for the request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateGetRequest(string endpointURL,
                                                         string filterString,
                                                         PaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(true);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string constructedURL = (endpointURL
                                     + "?" + filterString
                                     + paginationString);

            UnityWebRequest webRequest = UnityWebRequest.Get(constructedURL);
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                Debug.Log("GENERATING GET REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'PUT' request
        /// </summary>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the PUT request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GeneratePutRequest(string endpointURL,
                                                         StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbPUT;
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                foreach(StringValueParameter svf in valueFields)
                {
                    formFields += "\n" + svf.key + "=" + svf.value;
                }

                Debug.Log("GENERATING PUT REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'POST' request
        /// </summary>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the POST request</param>
        /// <param name="dataFields">The binary data to be submitted with the POST request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GeneratePostRequest(string endpointURL,
                                                          StringValueParameter[] valueFields,
                                                          BinaryDataParameter[] dataFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }
            if(dataFields != null)
            {
                foreach(BinaryDataParameter dataField in dataFields)
                {
                    form.AddBinaryData(dataField.key, dataField.contents, dataField.fileName, dataField.mimeType);
                }
            }


            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                if(valueFields != null)
                {
                    foreach(StringValueParameter valueField in valueFields)
                    {
                        formFields += "\n" + valueField.key + "=" + valueField.value;
                    }

                }
                if(dataFields != null)
                {
                    foreach(BinaryDataParameter dataField in dataFields)
                    {
                        formFields += "\n" + dataField.key + "= [BINARY DATA]: "
                                    + dataField.fileName + "("
                                    + (dataField.contents.Length/1000f).ToString("0.00") + "KB)\n";
                    }
                }

                Debug.Log("GENERATING POST REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'DELETE' request
        /// </summary>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the DELETE request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateDeleteRequest(string endpointURL,
                                                            StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbDELETE;
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                if(valueFields != null)
                {
                    foreach(StringValueParameter kvp in valueFields)
                    {
                        formFields += "\n" + kvp.key + "=" + kvp.value;
                    }
                }
                // foreach(KeyValuePair<string, Request.BinaryData> kvp in dataFields)
                // {
                //     formFields += "\n" + kvp.Key + "= [BINARY DATA] " + kvp.Value.fileName + "\n";
                // }

                Debug.Log("GENERATING DELETE REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>
        /// Sends the request and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">
        /// UnityWebRequestAsyncOperation</a> that is created.
        /// </summary>
        /// <param name="webRequest">The request to send and attach the callback functions to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SendRequest(UnityWebRequest webRequest,
                                       Action successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            // - Start Request -
            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log(webRequest.method.ToUpper() + " REQUEST RESPONSE"
                              + "\nResponse received at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + " [" + responseTimeStamp + "]"
                              + "\nURL: " + webRequest.url
                              + "\nResponse Code: " + webRequest.responseCode
                              + "\nResponse Error: " + webRequest.error
                              + "\nResponse: " + webRequest.downloadHandler.text
                              + "\n");
                }
                #endif

                if(webRequest.isNetworkError || webRequest.isHttpError)
                {
                    if(errorCallback != null)
                    {
                        errorCallback(WebRequestError.GenerateFromWebRequest(webRequest));
                    }
                }
                else
                {
                    if(successCallback != null) { successCallback(); }
                }
            };
        }

        /// <summary>
        /// Sends the request and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">
        /// UnityWebRequestAsyncOperation</a> that is created, and attempts to parse the response.
        /// </summary>
        /// <param name="webRequest">The request to send and attach the callback functions to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        /// <remarks>See also: [[ModIO.APIClient.SendRequest]]</remarks>
        public static void SendRequest<T>(UnityWebRequest webRequest,
                                          Action<T> successCallback,
                                          Action<WebRequestError> errorCallback)
        {
            Action processResponse = () =>
            {
                if(successCallback != null)
                {
                    try
                    {
                        T response = default(T);
                        response = JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                        successCallback(response);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("[mod.io] Failed to convert response into " + typeof(T).ToString() + " representation\n\n"
                                       + Utility.GenerateExceptionDebugString(e));

                        // TODO(@jackson): Error!
                    }
                }
            };

            APIClient.SendRequest(webRequest,
                                  processResponse,
                                  errorCallback);
        }


        // ---------[ AUTHENTICATION ]---------
        /// <summary>
        /// Requests a login code be sent to the user's email address.
        /// </summary>
        /// <remarks>
        /// For further information see the <a href="https://docs.mod.io/#authentication"> mod.io
        /// filtering documentation</a>.
        /// See also: [[ModIO.APIClient.GetOAuthToken]]
        /// </remarks>
        /// <param name="emailAddress">The user's email address to receive the security code</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SendSecurityCode(string emailAddress,
                                            Action<APIMessage> successCallback,
                                            Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/oauth/emailrequest";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", APIClient.gameAPIKey),
                StringValueParameter.Create("email", emailAddress),
            };

            // NOTE(@jackson): APIClient post requests _always_ require
            // the userAuthorizationToken to be set, and so we just use
            // a dummy value here.
            string oldToken = APIClient.userAuthorizationToken;
            APIClient.userAuthorizationToken = "NONE";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       valueFields,
                                                                       null);

            APIClient.userAuthorizationToken = oldToken;

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Wrapper object for [[ModIO.APIClient.GetOAuthToken]] requests
        /// </summary>
        [System.Serializable]
        private struct AccessTokenObject { public string access_token; }

        /// <summary>
        /// Requests the user's application OAuthToken that matches the single use security code.
        /// </summary>
        /// <remarks>
        /// For further information see the <a href="https://docs.mod.io/#authentication"> mod.io
        /// filtering documentation</a>.
        /// See also: [[ModIO.APIClient.SendSecurityCode]]
        /// </remarks>
        /// <param name="securityCode">The security code sent to the user's email address.</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetOAuthToken(string securityCode,
                                         Action<string> successCallback,
                                         Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/oauth/emailexchange";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", APIClient.gameAPIKey),
                StringValueParameter.Create("security_code", securityCode),
            };

            // NOTE(@jackson): APIClient post requests _always_ require
            // the userAuthorizationToken to be set, and so we just use
            // a dummy value here.
            string oldToken = APIClient.userAuthorizationToken;
            APIClient.userAuthorizationToken = "NONE";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       valueFields,
                                                                       null);

            APIClient.userAuthorizationToken = oldToken;

            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        /// <summary>
        /// Fetchs all games profiles from the mod.io servers matching the filter and pagination
        /// parameters.
        /// </summary>
        /// <remarks>
        /// Successful request will return an [[ModIO.API.ResponseArray]]
        /// of [[ModIO.GameProfile]]. We recommended reading the
        /// <a href="https://docs.mod.io/#filtering">filtering documentation</a> to return only the
        /// records you want.
        /// </remarks>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllGames(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<GameProfile>> successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Gets the game profile matching the value stored in [[ModIO.APIClient.gameId]].
        /// </summary>
        /// <remarks>
        /// Successful request will return a single [[ModIO.GameProfile]].
        /// </remarks>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetGame(Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Update the game profile for the game id stored in [[ModIO.APIClient.gameId]].
        /// </summary>
        /// <remarks>
        /// <para>This function only supports the game profile fields listed in the
        /// [[ModIO.API.EditGameParameters]] class.  To update the icon, logo or header fields use
        /// [[ModIO.APIClient.AddGameMedia]]. A successful request will return updated
        /// [[ModIO.GameProfile]].</para>
        /// <para>**NOTE:** You can also edit a game profile directly using the on the mod.io
        /// web interface. This is the recommended approach.</para>
        /// </remarks>
        /// <param name="parameters">The updated values for the game profile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void EditGame(EditGameParameters parameters,
                                    Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        /// <summary>
        /// Get all mods for the corresponding game. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModProfile]]. We recommended
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetAllMods(RequestFilter filter, PaginationParameters pagination,
                                      Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get a mod. Successful request will return a single [[ModIO.ModProfile]].
        /// </summary>
        public static void GetMod(int modId,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Add a mod. Successful request will return the newly created
        /// [[ModIO.ModProfile]]. By publishing your mod on mod.io, you are agreeing to
        /// the mod.io distribution agreement.
        /// </summary>
        /// <remarks>
        /// By default new mods are [[ModIO.ModStatus.NotAccepted]] and
        /// [[ModIO.ModVisibility.Public]]. They can only be
        /// [[ModIO.ModStatus.Accepted]] and made available via the API once a
        /// [[ModIO.Modfile]] has been uploaded. Media, Metadata Key Value Pairs and
        /// Dependencies can also be added after a mod profile is created.
        /// </remarks>
        public static void AddMod(AddModParameters parameters,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Edit details for a mod. If you want to update the logo or media associated with this
        /// mod, you need to use [[ModIO.APIClient.AddModMedia]]. The same applies to
        /// Mod Files, Metadata Key Value Pairs and Dependencies which are all managed via other
        /// endpoints. Successful request will return the updated [[ModIO.ModProfile]].
        /// </summary>
        public static void EditMod(int modId,
                                   EditModParameters parameters,
                                   Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete a mod profile. Successful request will return 204 No Content and create a
        /// [[ModIO.ModEvent]] with the type
        /// [[ModIO.ModEventType.ModUnavailable]].
        /// </summary>
        /// <remarks>
        /// This will close the mod profile which means it cannot be viewed or retrieved via API
        /// requests but will still exist in-case you choose to restore it at a later date. A mod
        /// can be permanently deleted via the website interface.
        /// </remarks>
        public static void DeleteMod(int modId,
                                     Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        /// <summary>
        /// Get all files that are published for the corresponding mod. Successful request will
        /// return a [[ModIO.API.ResponseArray]] of [[ModIO.Modfile]]. We
        /// recommended reading the <a href="https://docs.mod.io/#filtering">filtering documentation
        /// </a> to return only the records you want.
        /// </summary>
        /// <remarks>
        /// If the game requires mod downloads to be initiated via the API, the
        /// [[ModIO.ModfileLocator.binaryURL]] returned will contain a verification hash.
        /// This hash must be supplied to get the modfile, and will expire at the time contained in
        /// [[ModIO.ModfileLocator.dateExpires]]. Saving and reusing the
        /// [[ModIO.ModfileLocator.binaryURL]] won't work in this situation given its
        /// dynamic nature.
        /// </remarks>
        public static void GetAllModfiles(int modId,
                                          RequestFilter filter, PaginationParameters pagination,
                                          Action<ResponseArray<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get a file. Successful request will return a single [[ModIO.Modfile]].
        /// <remarks>
        /// If the game requires mod downloads to be initiated via the API, the
        /// [[ModIO.ModfileLocator.binaryURL]] returned will contain a verification hash.
        /// This hash must be supplied to get the modfile, and will expire at the time contained in
        /// [[ModIO.ModfileLocator.dateExpires]]. Saving and reusing the
        /// [[ModIO.ModfileLocator.binaryURL]] won't work in this situation given its
        /// dynamic nature.
        /// </remarks>
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Upload a file for the corresponding mod. Successful request will return the newly
        /// created [[ModIO.Modfile]]. Ensure that the release you are uploading is stable
        /// and free from any critical issues. Files are scanned upon upload, any users who upload
        /// malicious files will have their accounts closed promptly.
        /// </summary>
        public static void AddModfile(int modId,
                                      AddModfileParameters parameters,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Edit the details of a published file. If you want to update fields other than the
        /// changelog, version and active status, you should add a new file instead. Successful
        /// request will return updated [[ModIO.Modfile]].
        /// </summary>
        public static void EditModfile(int modId, int modfileId,
                                       EditModfileParameters parameters,
                                       Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        /// <summary>
        /// Upload new media to a game. Successful request will return an
        /// [[ModIO.APIMessage]].
        /// <remarks>
        /// You can also add media to your games profile on the mod.io website. This is the
        /// recommended approach.
        /// </remarks>
        public static void AddGameMedia(AddGameMediaParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// This endpoint is very flexible and will add any images posted to the mods gallery
        /// regardless of their body name providing they are a valid image. Successful request will
        /// return an [[ModIO.APIMessage]].
        public static void AddModMedia(int modId,
                                       AddModMediaParameters parameters,
                                       Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete images, sketchfab or youtube links from a mod profile. Successful request will
        /// return 204 No Content.
        /// </summary>
        public static void DeleteModMedia(int modId,
                                          DeleteModMediaParameters parameters,
                                          Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/media";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        /// <summary>
        /// Subscribe the authenticated user to a corresponding mod. No body parameters are required
        /// for this action. Successful request will return the [[ModIO.ModProfile]] of
        /// the newly subscribed mod.
        /// </summary>
        /// <remarks>
        /// Users can subscribe to mods via the mod.io web interface. Thus we recommend you poll
        /// [[ModIO.APIClient.GetUserEvents]] to keep a user's mods collection up to date.
        /// </remarks>
        public static void SubscribeToMod(int modId,
                                          Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    null,
                                                                    null);


            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Unsubscribe the authenticated user from the corresponding mod. No body parameters are
        /// required for this action. Successful request will return 204 No Content.
        /// </summary>
        /// <remarks>
        /// Users can unsubscribe from mods via the mod.io web interface. Thus we recommend you poll
        /// [[ModIO.APIClient.GetUserEvents]] to keep a user's mods collection up to date.
        /// </remarks>
        public static void UnsubscribeFromMod(int modId,
                                              Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        /// <summary>
        /// Get the event log for a mod, showing changes made sorted by latest event first.
        /// Successful request will return a [[ModIO.API.ResponseArray]] of
        /// [[ModIO.ModEvent]]. We recommended reading the
        /// <a href="https://docs.mod.io/#filtering">filtering documentation</a> to return only the
        /// records you want.
        /// </summary>
        public static void GetModEvents(int modId,
                                        RequestFilter filter, PaginationParameters pagination,
                                        Action<ResponseArray<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Get all mods events for the corresponding game sorted by latest event first.
        /// Successful request will return a [[ModIO.API.ResponseArray]] of
        /// [[ModIO.ModEvent]].
        /// <remarks>
        /// We recommend you poll this endpoint to keep mods up-to-date. If polling this endpoint
        /// for updates you should store the id or date_updated of the latest event, and on
        /// subsequent requests use that information in the filter, to return only newer events to
        /// process.
        /// </remarks>
        public static void GetAllModEvents(RequestFilter filter, PaginationParameters pagination,
                                           Action<ResponseArray<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        /// <summary>
        /// Get all tags for the corresponding game, that can be applied to any of its mods.
        /// Successful request will return a [[ModIO.API.ResponseArray]] of
        /// [[ModIO.ModTagCategory]].
        public static void GetAllGameTagOptions(Action<ResponseArray<ModTagCategory>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Add tags which mods can apply to their profiles. Successful request will return an
        /// [[ModIO.APIMessage]]. Tagging is a critical feature that powers the searching
        /// and filtering of mods for your game, as well as allowing you to control how mods are
        /// installed and played. For example you might enforce mods to be a particular type (map,
        /// model, script, save, effects, blueprint), which dictates how you install it. You may use
        /// tags to specify what the mod replaces (building, prop, car, boat, character). Or perhaps
        /// the tags describe the theme of the mod (fun, scenic, realism). The implementation is up
        /// to you, but the more detail you support the better filtering and searching becomes. If
        /// you need to store more advanced information, you can also use
        /// [[ModIO.ModProfile.metadataKVPs]].
        /// </summary>
        /// <remarks>
        /// You can also manage tags via the mod.io web interface. This is the recommended approach.
        /// </remarks>
        public static void AddGameTagOption(AddGameTagOptionParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete an entire group of tags or individual tags. Successful request will return
        /// 204 No Content.
        /// </summary>
        /// <remarks>
        /// You can also manage tags by editing your games profile via the mod.io web interface.
        /// This is the recommended approach.
        /// </remarks>
        public static void DeleteGameTagOption(DeleteGameTagOptionParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all tags for the corresponding mod. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModTag]]. We recommended
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetModTags(int modId,
                                      RequestFilter filter, PaginationParameters pagination,
                                      Action<ResponseArray<ModTag>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Add tags to a mod's profile. You can only add tags allowed by the parent game, which are
        /// listed under [[ModIO.GameProfile.tagCategories]]. Successful request will
        /// return an [[ModIO.APIMessage]].
        /// </summary>
        public static void AddModTags(int modId, AddModTagsParameters parameters,
                                      Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete tags from a mod's profile. Deleting tags is identical to adding tags except the
        /// request method is DELETE instead of POST. Successful request will return 204 No Content.
        /// </summary>
        public static void DeleteModTags(int modId,
                                         DeleteModTagsParameters parameters,
                                         Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        /// <summary>
        /// Submit a positive or negative rating for a mod. Each user can supply only one rating for
        /// a mod, subsequent ratings will override the old value. Successful request will return an
        /// [[ModIO.APIMessage]].
        /// <remarks>
        /// You can order mods by their rating, and view their rating in the
        /// [[ModIO.ModProfile]].
        /// </remarks>
        public static void AddModRating(int modId, AddModRatingParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/ratings";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        /// <summary>
        /// Get all metadata stored by the game developer for this mod as searchable key value
        /// pairs. Successful request will return a [[ModIO.API.ResponseArray]] of
        /// [[ModIO.MetadataKVP]].
        /// <remarks>
        /// Metadata can also be stored to [[ModIO.ModProfile.metadataBlob]].
        /// </remarks>
        public static void GetAllModKVPMetadata(int modId,
                                                PaginationParameters pagination,
                                                Action<ResponseArray<MetadataKVP>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Add metadata for this mod as searchable key value pairs. Metadata is useful to define
        /// how a mod works, or other information you need to display and manage the mod. Successful
        /// request will return an [[ModIO.APIMessage]].
        /// </summary>
        /// <example>
        /// A mod might change gravity and the rate of fire of weapons, you could define these
        /// properties as key value pairs.
        /// </example>
        /// <remarks>
        /// We recommend the mod upload tool you create defines and submits metadata behind the
        /// scenes, because if these settings affect gameplay, invalid information may cause
        /// problems.
        /// </remarks>
        public static void AddModKVPMetadata(int modId, AddModKVPMetadataParameters parameters,
                                             Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete key value pairs metadata defined for this mod. Successful request will return
        /// 204 No Content.
        /// </summary>
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public static void GetAllModDependencies(int modId,
                                                 RequestFilter filter, PaginationParameters pagination,
                                                 Action<ResponseArray<ModDependency>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Dependencies
        public static void AddModDependencies(int modId, AddModDependenciesParameters parameters,
                                              Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Dependencies
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        /// <summary>
        /// Get all users that are part of a mod team. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModTeamMember]]. We
        /// recommend reading the <a href="https://docs.mod.io/#filtering">filtering documentation
        /// </a> to return only the records you want.
        /// </summary>
        public static void GetAllModTeamMembers(int modId,
                                                RequestFilter filter, PaginationParameters pagination,
                                                Action<ResponseArray<ModTeamMember>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Add a user to a mod team. Successful request will return an
        /// [[ModIO.APIMessage]] and fire a
        /// [[ModIO.ModEventType.ModTeamChanged]] [[ModIO.ModEvent]].
        /// </summary>
        public static void AddModTeamMember(int modId, AddModTeamMemberParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Update a mod team members details. Successful request will return an
        /// [[ModIO.APIMessage]].
        /// </summary>
        public static void UpdateModTeamMember(int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                   parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete a user from a mod team. This will revoke their access rights if they are not the
        /// original creator of the resource. Successful request will return 204 No Content and fire
        /// a [[ModIO.ModEventType.ModTeamChanged]] [[ModIO.ModEvent]].
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        public static void GetAllModComments(int modId,
                                             RequestFilter filter, PaginationParameters pagination,
                                             Action<ResponseArray<ModComment>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Mod Comment
        public static void GetModComment(int modId, int commentId,
                                         Action<ModComment> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        /// <summary>
        /// Get the user that is the original submitter of a resource. Successful request will
        /// return a single [[ModIO.UserProfile]].
        /// </summary>
        /// <remarks>
        /// Mods and games can be managed by teams of users, for the most accurate information you
        /// should use the Team endpoints.
        /// </remarks>
        public static void GetResourceOwner(APIResourceType resourceType, int resourceID,
                                            Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/general/owner";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueParameter.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    valueFields,
                                                                    null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all users registered on mod.io. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.UserProfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetAllUsers(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<UserProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get a user. Successful request will return a single [[ModIO.UserProfile]].
        /// </summary>
        public static void GetUser(int userID,
                                   Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users/" + userID;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public static void SubmitReport(SubmitReportParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/report";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ ME ENDPOINTS ]---------
        /// <summary>
        /// Get the authenticated user details. Successful request will return a single
        /// [[ModIO.UserProfile]].
        /// </summary>
        public static void GetAuthenticatedUser(Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all mod's the authenticated user is subscribed to. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModProfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetUserSubscriptions(RequestFilter filter, PaginationParameters pagination,
                                                Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get events that have been fired specific to the user. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.UserEvent]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetUserEvents(RequestFilter filter, PaginationParameters pagination,
                                         Action<ResponseArray<UserEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/events";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all games the authenticated user added or is a team member of. Successful request
        /// will return a [[ModIO.API.ResponseArray]] of [[ModIO.GameProfile]].
        /// We recommend reading the <a href="https://docs.mod.io/#filtering">filtering
        /// documentation</a> to return only the records you want.
        /// </summary>
        public static void GetUserGames(Action<ResponseArray<GameProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all mods the authenticated user added or is a team member of. Successful request
        /// will return a [[ModIO.API.ResponseArray]] of [[ModIO.ModProfile]].
        /// We recommended reading the <a href="https://docs.mod.io/#filtering">filtering
        /// documentation</a> to return only the records you want.
        /// </summary>
        public static void GetUserMods(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all modfiles the authenticated user uploaded. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.Modfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        /// <param name="filter">The filter to be applied to the request</param>
        public static void GetUserModfiles(RequestFilter filter, PaginationParameters pagination,
                                           Action<ResponseArray<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
    }
}
