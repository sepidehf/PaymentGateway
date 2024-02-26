using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BankGateway.Domain.Helpers;
using BankGateway.Domain.Infrastracture;
using BankGateway.Domain.Models.DTO.BaamDTO;
using BankGateway.Domain.Models.Enum;
using Newtonsoft.Json;
using BankGateway.Domain.Services.Interface;
using Core.CrossCutting.Infrustructure.Logger;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services
{
    public class BaamService : IBankService
    {
        private HttpClient _bamClient;
        private static readonly string _bamAuthenticationServiceAddress;
        private static readonly string _bamUserName;
        private static readonly string _bamPassword;
        private static BaamToken _bamToken;
        private static readonly string _bamBatchTransactionServicesAddress;
        private static IHttpClientFactory _clientFactory;
        private static readonly ILogger _baamCommonLogger;
        private static readonly ILogger _baamServiceErrorModelLogger;

        /// <summary>
        /// Initializes static members of the <see cref="BaamService"/> class.
        /// </summary>
        static BaamService()
        {
            _bamAuthenticationServiceAddress = ConfigurationManager.AppSettings["BamAuthenticationServiceAddress"];
            _bamUserName = ConfigurationManager.AppSettings["BamUserName"];
            _bamPassword = ConfigurationManager.AppSettings["BamPassword"];
            _bamBatchTransactionServicesAddress = ConfigurationManager.AppSettings["BamBatchTransactionServicesAddress"];
            _clientFactory = new HttpClientFactory();
            _baamCommonLogger = new Logger("BaamServiceLogger");
            _baamServiceErrorModelLogger = new Logger("BaamServiceerrorModelLogger");
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        }


        /// <summary>
        /// Authentication
        /// </summary>
        /// <returns>token</returns>
        private async Task<bool> Login(BaamScope scope = BaamScope.AllScope)
        {
            try
            {
                if (_bamToken != null && _bamToken.IsValidAndNotExpiring)
                    return true;

                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamAuthenticationServiceAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                });

                _bamClient.DefaultRequestHeaders.Clear();
                var clientCredential = Encoding.UTF8.GetBytes($"{_bamUserName}:{_bamPassword}");
                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(clientCredential));

                var postMessage = new Dictionary<string, string>
                    {
                        {"grant_type", "client_credentials"},
                        { "scope", $"{EnumHelper<BaamScope>.GetEnumDescription(scope.ToString())}"}
                    };

                var content = new FormUrlEncodedContent(postMessage);
                //LOGERR
                _baamCommonLogger.Info("Login Called");

                var response = await _bamClient.PostAsync("token", content);

                if (response.StatusCode != HttpStatusCode.OK) return false;

                var json = await response.Content.ReadAsStringAsync();
                var bamToken = JsonConvert.DeserializeObject<BaamToken>(json);
                bamToken.ExpiresAt = DateTime.UtcNow.AddSeconds(bamToken.ExpiresIn);
                _bamToken = bamToken;

                _bamClient.DefaultRequestHeaders.Clear();
                _bamClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //_bamClient.DefaultRequestHeaders.Add("token", _bamToken.AccessToken);
                return true;
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On Login",exception);
                throw;
            }
        }
        /// <summary>
        /// ایجاد یک دستور پرداخت جدید
        /// SADAD method signature: POST---- /orders
        /// </summary>
        /// <param name="paymentOrderRegister">The payment order register.</param>
        /// <returns>Task&lt;PaymentOrderRegisterOutput&gt;.</returns>
        public async Task<PaymentOrderRegisterOutput> RegisterOrder(PaymentOrderRegisterInput paymentOrderRegister)
        {

            try
            {
                if (!await Login(BaamScope.MoneyTransfer))
                {
                    //LOGERR
                    _baamCommonLogger.Info($"login Failed for register order {paymentOrderRegister.OrderId} request");
                    return new PaymentOrderRegisterOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });



                //_bamClient.DefaultRequestHeaders.ConnectionClose = true;

                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                var postMessage = JsonConvert.SerializeObject(paymentOrderRegister);
                var content = new StringContent(postMessage, Encoding.UTF8, "application/json");
                //LOGGER
                _baamCommonLogger.Info($"Sending for registering order with Id : {paymentOrderRegister.OrderId},{content}");
                var response = await _bamClient.PostAsync("payment/v1/orders", content);
                var jsonResult = await response.Content.ReadAsStringAsync();
                _baamCommonLogger.Info($"Bank Response for registering order with Id : {paymentOrderRegister.OrderId},{jsonResult}");
               

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        _baamCommonLogger.Info($"Request with order number : {paymentOrderRegister.OrderId} created");
                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<PaymentOrderRegisterOutput>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "عملیات با موفقیت انجام شد";
                        return paymentOrderRegisterOutput;
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Forbidden:
                        var badRequestErrorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        //LOGERR
                        _baamCommonLogger.Info("Bank Service Error on registerOrder", new Exception(JsonConvert.SerializeObject(badRequestErrorModel)));
                        var errorResult = new PaymentOrderRegisterOutput()
                        {
                            IsSuccess = false,
                            Message = badRequestErrorModel.ErrorSummary
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                badRequestErrorModel.ErrorCode) continue;
                            errorResult.ErrorCode = suit;
                            break;
                        }
                        if(errorResult.ErrorCode==0)
                            errorResult.ErrorCode=ErrorCode.UnmappedErrors;

                        return errorResult;

                    case HttpStatusCode.Conflict:
                        //todo json response is incorrect 
                        var conflictErrorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        //LOGERR
                        _baamCommonLogger.Info("Bank Service Error on registerOrder", new Exception(JsonConvert.SerializeObject(conflictErrorModel)));
                        var conflictResult = new PaymentOrderRegisterOutput()
                        {
                            IsSuccess = false,
                            Message = conflictErrorModel.ErrorSummary,
                           
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                conflictErrorModel.ErrorCode) continue;
                            conflictResult.ErrorCode = suit;
                            break;
                        }
                        if (conflictResult.ErrorCode == 0)
                            conflictResult.ErrorCode = ErrorCode.UnmappedErrors;

                        return conflictResult;
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        //LOGERR
                        _baamCommonLogger.Info("Bank Service Error on registerOrder", new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result = new PaymentOrderRegisterOutput()
                        {
                            IsSuccess = false,
                            Message = errorModel.ErrorSummary,
                          
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;

                        return result;
                }
            }
            catch (Exception ex)
            {
                _baamCommonLogger.Error("Bank Service Error on registerOrder"+ex.Message);

                return new PaymentOrderRegisterOutput()
                {
                    IsSuccess=false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,

                };
            }
        }


        /// <summary>
        /// استعلام یک دستور پرداخت مشخص
        /// SADAD Method signature : Get , /orders/{orderId}
        /// </summary>
        /// <param name="orderId">شناسه دستور پرداخت</param>
        /// <returns>Task&lt;PaymentOrderRegisterOutput&gt;.</returns>
        public async Task<PaymentOrderRegisterOutput> PaymentOrderInquiry(string orderId)
        {
            //Method: Payment GET
            try
            {
                if (!await Login(BaamScope.MoneyTransfer))
                {
                    _baamCommonLogger.Info($"login Failed for PaymentOrderInquiry with : {orderId}");
                    return new PaymentOrderRegisterOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });

                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);
                //LOGGER
                _baamCommonLogger.Info($"Calling PaymentOrderInquiry Bank Service with order Id : {orderId}");
                var response = _bamClient.GetAsync("payment/v1/orders/" + orderId).Result;
                var jsonResult = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<PaymentOrderRegisterOutput>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        //LOGERR
                        _baamCommonLogger.Debug("Bank Service http request Error on PaymentOrderInquiry", new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result = new PaymentOrderRegisterOutput()
                        {
                            IsSuccess = false,
                            Message = errorModel.ErrorSummary
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;

                        return result;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On PaymentOrderInquiry",exception);
                return new PaymentOrderRegisterOutput()
                {
                    IsSuccess=false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }



        /// <summary>
        /// اعلام تکمیل شدن فایل‌های یک دستور پرداخت مشخص
        /// SADAD method signature: PATCH , /orders/{orderId}
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <returns>Task&lt;PaymentOrderRegisterOutput&gt;.</returns>
        public async Task<PaymentOrderRegisterOutput> CompletePaymentOrder(string orderId)
        {
            //Method: Payment PATCH
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info($"login Failed for CompletePaymentOrder with : {orderId} ,request");
                    return new PaymentOrderRegisterOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });

                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                string postMessage = @"{""payable"":true}";
                HttpContent content = new StringContent(postMessage, Encoding.UTF8, "application/json");

                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = new HttpMethod("PATCH"),
                    RequestUri = new Uri(_bamBatchTransactionServicesAddress + "payment/v1/orders/" + orderId),
                    Content = content
                };

                _baamCommonLogger.Info($"Calling CompletePaymentOrder Bank Service with order Id : {orderId}");
                var response = _bamClient.SendAsync(request).Result;
                var jsonResult = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:

                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<PaymentOrderRegisterOutput>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service http request Error on CompletePaymentOrder",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result= new PaymentOrderRegisterOutput
                        {
                            Message =errorModel.ErrorSummary,
                            IsSuccess = false,     
                        };
                    
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {                    
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        return result;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On CompletePaymentOrder",exception);
                return new PaymentOrderRegisterOutput()
                {
                    IsSuccess=false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }

        /// <summary>
        ///  ارسال یک بسته اطلاعاتی مشخص
        ///  SADAD method signature ---- Put---- /orders/{orderId}/details/{detailId}  
        /// </summary>
        /// <param name="batchTransaction"></param>
        /// <returns>BaamBatchTransactionOutput.</returns>
        public async Task<BaamBatchTransactionOutput> SendTransactionInformation(BaamBatchTransactionInput batchTransaction)
        {

            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info($"login Failed for SendTransactionInformation with : {batchTransaction.OrderId} ,request");
                    return new BaamBatchTransactionOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }

                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"},

                });

                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                var postMessage = JsonConvert.SerializeObject(batchTransaction.TransferInfos);
                var content = new StringContent(postMessage, Encoding.UTF8, "application/json");

                _baamCommonLogger.Info($"Calling SendTransactionInformation Bank Service with order Id : {batchTransaction.OrderId} and package:{batchTransaction.PackageId}");

                var response = _bamClient.PutAsync("payment/v1/orders/" + batchTransaction.OrderId + "/details/" + batchTransaction.PackageId, content).Result;

                var jsonResult = await response.Content.ReadAsStringAsync();
                _baamCommonLogger.Info($"Response SendTransactionInformation Bank Service with order Id : {batchTransaction.OrderId},and {jsonResult}");
                _baamCommonLogger.Info($"Response SendTransactionInformation Bank Service with order Id : {batchTransaction.OrderId},and {jsonResult} is:{response.StatusCode} ");
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        
                        var baamBatchTransactionOutput = JsonConvert.DeserializeObject<BaamBatchTransactionOutput>(jsonResult);
                        baamBatchTransactionOutput.IsSuccess = true;
                        baamBatchTransactionOutput.Message = "";
                        return baamBatchTransactionOutput;
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Forbidden:
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service http request Error on SendTransactionInformation",
                            new Exception(JsonConvert.SerializeObject(errorModel)));

                        var result= new BaamBatchTransactionOutput
                        {
                            Message =errorModel.ErrorSummary+errorModel.ErrorCauses.FirstOrDefault().Cause,
                            IsSuccess = false
                        };

                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;
                        return result;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On SendTransactionInformation",exception);
                return new BaamBatchTransactionOutput()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }



        /// <summary>
        /// استعلام یک بسته اطلاعاتی مشخص
        /// SADAD method signature: GET, /orders/{orderId}/details/{detailId}
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>Task&lt;BaamBatchTransactionOutput&gt;.</returns>
        public async Task<BaamBatchTransactionOutput> PackageTransactionInquiry(int orderId, int packageId)
        {

            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for PackageTransactionInquiry");
                    return new BaamBatchTransactionOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });


                _baamCommonLogger.Info($"Calling PackageTransactionInquiry Bank Service with order Id : {orderId} " +
                                        $"and package Id {packageId}");
                var response = _bamClient.GetAsync($"orders/{orderId}/details/{packageId}").Result;
                var jsonResult = await response.Content.ReadAsStringAsync();

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:

                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<BaamBatchTransactionOutput>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service http request Error on PackageTransactionInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));

                        var result= new BaamBatchTransactionOutput
                        {
                            Message = errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;
                        return result;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On PackageTransactionInquiry",exception);
                return new BaamBatchTransactionOutput()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }

        /// <summary>
        /// استعلام یک رکورد مشخص
        /// SADAD method signature: GET,/orders/{orderId}/details/{detailId}/records/{recordId}
        /// </summary>
        /// <param name="recordInquiry">The record inquiry.</param>
        /// <returns>RecordInquiryOutput.</returns>
        public async Task<RecordInquiryResponseModel> RecordInquiry(RecordInqueryInput recordInquiry)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for RecordInquiry");
                    return new RecordInquiryResponseModel
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress),
                    new Dictionary<string, string>
                    {
                        {"Accept", "application/json"}
                        //{ "token",_bamToken.AccessToken}
                    });
                _baamCommonLogger.Debug($"Calling RecordInquiry Bank Service with order Id : {recordInquiry.OrderId}" +
                                        $" and package Id {recordInquiry.PackageId} and record {recordInquiry.RecordId}");

                var response = _bamClient.GetAsync($"/payment/v1/orders/{recordInquiry.OrderId}/details/{recordInquiry.PackageId}/records/{recordInquiry.RecordId}").Result;
                var jsonResult = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:

                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<RecordInquiryResponseModel>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;
                    default:
                        var errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service http request Error on PackageTransactionInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result= new RecordInquiryResponseModel
                        {
                            Message =errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;
                        return result;
                }
            }
            catch (Exception e)
            {
                _baamCommonLogger.Error("Unknown Exception On RecordInquiry",e);
                return new RecordInquiryResponseModel()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }


        /// <summary>
        /// دریافت لیست تراکنش‌ها براساس شناسه یکتای کلاینت، شناسه یکتای شرکت، نوع تراکنش، شناسه یکتای تراکنش والد و وضعیت انجام آنها
        /// /transactions
        /// SADAD method signature: GET , /transactions
        /// </summary>
        /// <param name="transactionInquiryInput">The transaction inquiry input.</param>
        /// <returns>TransactionsInquiryOutput.</returns>
        public async Task<TransactionsInquiryOutput> TransactionsInquiry(TransactionsInquiryInput transactionInquiryInput)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for TransactionsInquiry");
                    return new TransactionsInquiryOutput
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }

                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress),
                    new Dictionary<string, string>
                    {
                        {"Accept", "application/json"}
                        //{ "token",_bamToken.AccessToken}
                    });

                var builder = new UriBuilder() {Host= "bamapi.bmi.ir",Scheme = "https",Path="/payment/v1/transactions"};
                
                var query = HttpUtility.ParseQueryString(builder.Query);
                //query["clientId"] = transactionInquiryInput.ClientId;
                //query["corporateId"] = transactionInquiryInput.CorporateId;
               // query["transactionType"] = transactionInquiryInput.TransactionType.ToString();
                query["parentTransactionId"] = transactionInquiryInput.ParentTransactionId;
               // query["status"] = transactionInquiryInput.Status.ToString();
                builder.Query = query.ToString();
                var requestUrl = builder.ToString();

                _baamCommonLogger.Debug($"Calling TransactionsInquiry Bank Service with requestUrl {requestUrl}");

                var response = _bamClient.GetAsync(requestUrl).Result;
                string jsonResult = await response.Content.ReadAsStringAsync();
                BaamErrorModel errorModel;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var paymentOrderRegisterOutput = JsonConvert.DeserializeObject<TransactionsInquiryOutput>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;


                    case HttpStatusCode.BadRequest:

                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service BadRequest on TransactionsInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result = new TransactionsInquiryOutput()
                        {
                            IsSuccess = false,
                            Message = JsonConvert.SerializeObject(errorModel)
                        };
                        return result;


                    default:
                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamCommonLogger.Debug("Bank Service http request Error on TransactionsInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));

                        return new TransactionsInquiryOutput
                        {
                            Message = "Error: status code is " + response.StatusCode,
                            IsSuccess = false
                        };
                }
            }
            catch (Exception ex)
            {
                _baamCommonLogger.Error($"Unknown Exception On {System.Reflection.MethodBase.GetCurrentMethod().Name}", ex);
                return new TransactionsInquiryOutput()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }


        /// <summary>
        /// استعلام یک تراکنش مشخص
        ///  SADAD method signature: GET , /transactions/{transactionId}
        /// </summary>
        /// <param name="recordId">The record identifier = transactionId in SADAD request parameter</param>
        /// <returns>TransactionsInquiryOutput.</returns>
        public async Task<TransactionInquiryResponseModel> TransactionInquiry(Guid recordId)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for TransactionsInquiry");
                    return new TransactionInquiryResponseModel
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress),
                    new Dictionary<string, string>
                    {
                        {"Accept", "application/json"}
                    });
                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                _baamCommonLogger.Debug($"Calling TransactionInquiry Bank Service with record Id {recordId}");
                var response = _bamClient.GetAsync($"payment/v1/transactions/{recordId}").Result;
                string jsonResult = await response.Content.ReadAsStringAsync();
                BaamErrorModel errorModel;
                switch (response.StatusCode)
                {
                    //todo chack errorcod statusecode
                    case HttpStatusCode.OK:
                        var transactionInquiryResponseModel = JsonConvert.DeserializeObject<TransactionInquiryResponseModel>(jsonResult);
                        
                        transactionInquiryResponseModel.IsSuccess = true;
                        transactionInquiryResponseModel.Message = "";
                        return transactionInquiryResponseModel;


                    case HttpStatusCode.BadRequest:
                        {
                            errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);

                            _baamServiceErrorModelLogger.Debug("Bank Service BadRequest on TransactionInquiry",
                                new Exception(JsonConvert.SerializeObject(errorModel)));

                            var result = new TransactionInquiryResponseModel()
                            {
                                IsSuccess = false,
                                Message = errorModel.ErrorSummary
                            };
                            foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                            {
                                if (suit.ToString() !=
                                    errorModel.ErrorCode) continue;
                                result.ErrorCode = suit;
                                break;
                            }
                            if (result.ErrorCode == 0)
                                result.ErrorCode = ErrorCode.UnmappedErrors;
                            return result;
                        }

                    default:
                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamServiceErrorModelLogger.Debug("Bank Service Error on TransactionInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var defaultresult= new TransactionInquiryResponseModel
                        {
                            Message = errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            defaultresult.ErrorCode = suit;
                            break;
                        }
                        if (defaultresult.ErrorCode == 0)
                            defaultresult.ErrorCode = ErrorCode.UnmappedErrors;
                        return defaultresult;
                }
            }
            catch (Exception ex)
            {
          
                _baamCommonLogger.Error($"Exception On TransactionInquiry for record '{recordId}'",ex);
                return new TransactionInquiryResponseModel()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }


        /// <summary>
        /// استعلام تاییدیه پرداخت یک تراکنش مشخص
        /// SADAD method signature: GET, /transactions/{transactionId}/confirmation
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>Task&lt;TransactionConfirmationResponseModel&gt;.</returns>
        public async Task<TransactionConfirmationBindingModel> TransactionConfirmationInquiry(Guid recordId)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for TransactionConfirmationInquiry");
                    return new TransactionConfirmationBindingModel
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }

                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress),
                    new Dictionary<string, string>
                    {
                        {"Accept", "application/json"}
                        //{ "token",_bamToken.AccessToken}
                    });

                _baamCommonLogger.Debug($"Sending TransactionConfirmationInquiry for record id : {recordId}");

                var response = _bamClient.GetAsync($"transactions/{recordId}").Result;
                string jsonResult = await response.Content.ReadAsStringAsync(); ;
                BaamErrorModel errorModel;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK://todo chack errorcod statusecode
                        var paymentOrderRegisterOutput =
                            JsonConvert.DeserializeObject<TransactionConfirmationBindingModel>(jsonResult);
                        paymentOrderRegisterOutput.IsSuccess = true;
                        paymentOrderRegisterOutput.Message = "";
                        return paymentOrderRegisterOutput;


                    case HttpStatusCode.BadRequest:
                        {
                            errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);

                            _baamServiceErrorModelLogger.Debug("Bank Service BadRequest on TransactionConfirmationInquiry",
                                new Exception(JsonConvert.SerializeObject(errorModel)));

                            var result = new TransactionConfirmationBindingModel()
                            {
                                IsSuccess = false,
                                Message =errorModel.ErrorSummary
                            };
                            foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                            {
                                if (suit.ToString() !=
                                    errorModel.ErrorCode) continue;
                                result.ErrorCode = suit;
                                break;
                            }
                            if (result.ErrorCode == 0)
                                result.ErrorCode = ErrorCode.UnmappedErrors;
                            return result;
                        }

                    default:
                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamServiceErrorModelLogger.Debug("Bank Service http request Error on TransactionConfirmationInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var defaultresult= new TransactionConfirmationBindingModel
                        {
                            Message =errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            defaultresult.ErrorCode = suit;
                            break;
                        }
                        if (defaultresult.ErrorCode == 0)
                            defaultresult.ErrorCode = ErrorCode.UnmappedErrors;
                        return defaultresult;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error($"Exception On TransactionConfirmationInquiry for transactionId {recordId}",exception);
                return new TransactionConfirmationBindingModel()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }

        }


        /// <summary>
        /// صدور تاییدیه پرداخت برای یک تراکنش مشخص
        /// SADAD method signature: POST, /transactions/{transactionId}/confirmation
        /// </summary>
        /// <param name="recordId">The record identifier that same transactionId in SADAD request Parameter</param>
        /// <param name="requestBindingModel">The request binding model.</param>
        /// <returns>Task&lt;TransactionConfirmationBindingModel&gt;.</returns>
        public async Task<TransactionConfirmationBindingModel> TransactionConfirmationRequest(Guid orderId,
            TransactionConfirmationBindingModel requestBindingModel)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {

                    _baamCommonLogger.Info("login Failed for TransactionConfirmationRequest");

                    return new TransactionConfirmationBindingModel
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };

                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });

                //_bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                //var postMessage = JsonConvert.SerializeObject(requestBindingModel);
                //var content = new StringContent(postMessage, Encoding.UTF8, "application/json");
                ////LOGERR
                //_baamCommonLogger.Debug($"Sending TransactionConfirmationRequest for record id : " +
                //                        $"{orderId} and request parameter is : {postMessage}");


                //var response = _bamClient.PutAsync($"payment/v1/transactions/{orderId}/confirmation", content).Result;
                //BaamErrorModel errorModel;
                //string jsonResult = await response.Content.ReadAsStringAsync();

                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress), new Dictionary<string, string>
                {
                    { "Accept","application/json"}
                    //{ "token",_bamToken.AccessToken}
                });

                _bamClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bamToken.AccessToken);

                var postMessage = JsonConvert.SerializeObject(requestBindingModel);
                var content = new StringContent(postMessage, Encoding.UTF8, "application/json");

                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = new HttpMethod("POST"),
                    RequestUri = new Uri(_bamBatchTransactionServicesAddress + "payment/v1/transactions/" + orderId + "/confirmation"),
                    Content = content
                };
                //LOGERR
                _baamCommonLogger.Debug($"Sending TransactionConfirmationRequest for record id : " +
                                        $"{orderId} and request parameter is : {postMessage}");


                var response = _bamClient.SendAsync(request).Result;
                BaamErrorModel errorModel;
                string jsonResult = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK://todo chack errorcod statusecode
                        var baamBatchTransactionOutput = JsonConvert.DeserializeObject<TransactionConfirmationBindingModel>(jsonResult);
                        baamBatchTransactionOutput.IsSuccess = true;
                        baamBatchTransactionOutput.Message = "";
                        return baamBatchTransactionOutput;
                    default:
                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamServiceErrorModelLogger.Debug("Bank Service http request Error on TransactionConfirmationRequest",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var result= new TransactionConfirmationBindingModel
                        {
                            Message = errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            result.ErrorCode = suit;
                            break;
                        }
                        if (result.ErrorCode == 0)
                            result.ErrorCode = ErrorCode.UnmappedErrors;
                        return result;
                }
            }
            catch (Exception exception)
            {
                _baamCommonLogger.Error("Unknown Exception On TransactionConfirmationRequest",exception);
                return new TransactionConfirmationBindingModel()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }


        /// <summary>
        /// استعلام یک رکورد- تراکنش مشخص
        ///  SADAD method signature: GET ,payment/v1/orders/orderId/details/packageId/records/recordId
        /// </summary>
        /// <param name="recordId">The record identifier = transactionId in SADAD request parameter</param>
        /// <returns>TransactionsInquiryOutput.</returns>
        public async Task<TransactionInquiryResponseModel> RecordTransactionInquiry(RecordInqueryInput recordInquery)
        {
            try
            {
                if (!Login(BaamScope.MoneyTransfer).Result)
                {
                    _baamCommonLogger.Info("login Failed for TransactionsInquiry");
                    return new TransactionInquiryResponseModel
                    {
                        Message = "Authentication failed...",
                        IsSuccess = false
                    };
                }


                _bamClient = _clientFactory.GetOrCreate(new Uri(_bamBatchTransactionServicesAddress),
                    new Dictionary<string, string>
                    {
                        {"Accept", "application/json"}
                    });

                _baamCommonLogger.Debug($"Calling TransactionInquiry Bank Service with record Id {recordInquery.RecordId}");
                var response = _bamClient.GetAsync($"payment/v1/orders/{recordInquery.OrderId}/details/{recordInquery.PackageId}/records/{recordInquery.RecordId}").Result;
                string jsonResult = await response.Content.ReadAsStringAsync();
                BaamErrorModel errorModel;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var transactionInquiryResponseModel = JsonConvert.DeserializeObject<TransactionInquiryResponseModel>(jsonResult);
                        transactionInquiryResponseModel.IsSuccess = true;
                        transactionInquiryResponseModel.Message = "";
                        return transactionInquiryResponseModel;


                    case HttpStatusCode.BadRequest:
                        {
                            errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);

                            _baamServiceErrorModelLogger.Debug("Bank Service BadRequest on TransactionInquiry",
                                new Exception(JsonConvert.SerializeObject(errorModel)));

                            var result = new TransactionInquiryResponseModel()
                            {
                                IsSuccess = false,
                                Message = errorModel.ErrorSummary
                            };
                            foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                            {
                                if (suit.ToString() !=
                                    errorModel.ErrorCode) continue;
                                result.ErrorCode = suit;
                                break;
                            }
                            if (result.ErrorCode == 0)
                                result.ErrorCode = ErrorCode.UnmappedErrors;

                            return result;
                        }

                    default:
                        errorModel = JsonConvert.DeserializeObject<BaamErrorModel>(jsonResult);
                        _baamServiceErrorModelLogger.Debug("Bank Service Error on TransactionInquiry",
                            new Exception(JsonConvert.SerializeObject(errorModel)));
                        var defaultresult= new TransactionInquiryResponseModel
                        {
                            Message = errorModel.ErrorSummary,
                            IsSuccess = false
                        };
                        foreach (ErrorCode suit in Enum.GetValues(typeof(ErrorCode)))
                        {
                            if (suit.ToString() !=
                                errorModel.ErrorCode) continue;
                            defaultresult.ErrorCode = suit;
                            break;
                        }
                        if (defaultresult.ErrorCode == 0)
                            defaultresult.ErrorCode = ErrorCode.UnmappedErrors;
                        return defaultresult;
                }
            }
            catch (Exception exception)
            {
               
                _baamCommonLogger.Error($"Exception On TransactionInquiry for record '{recordInquery.RecordId}'",exception);
                return new TransactionInquiryResponseModel()
                {
                    IsSuccess=false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError,
                };
            }
        }
    }
}
