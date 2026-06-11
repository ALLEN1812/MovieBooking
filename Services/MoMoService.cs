using System.Security.Cryptography;
using System.Text;

namespace OnlineMovieBooking.Services;

public interface IMoMoService
{
    MoMoResponse ValidateReturn(IQueryCollection query);
}

public class MoMoResponse
{
    public bool IsSuccess { get; set; }
    public int BookingId { get; set; }
    public string ResultCode { get; set; } = "";
    public string TransactionId { get; set; } = "";
}

public class MoMoService : IMoMoService
{
    private readonly IConfiguration _config;

    public MoMoService(IConfiguration config) => _config = config;

    public MoMoResponse ValidateReturn(IQueryCollection query)
    {
        var secretKey    = _config["MoMo:SecretKey"]!;
        var accessKey    = _config["MoMo:AccessKey"]!;
        var partnerCode  = query["partnerCode"].ToString();
        var orderId      = query["orderId"].ToString();
        var requestId    = query["requestId"].ToString();
        var amount       = query["amount"].ToString();
        var orderInfo    = query["orderInfo"].ToString();
        var orderType    = query["orderType"].ToString();
        var transId      = query["transId"].ToString();
        var resultCode   = query["resultCode"].ToString();
        var message      = query["message"].ToString();
        var payType      = query["payType"].ToString();
        var responseTime = query["responseTime"].ToString();
        var extraData    = query["extraData"].ToString();
        var receivedSig  = query["signature"].ToString();

        var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}" +
                      $"&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}" +
                      $"&partnerCode={partnerCode}&payType={payType}&requestId={requestId}" +
                      $"&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

        var computedSig = HmacSha256(secretKey, rawHash);
        var bookingId = int.TryParse(orderId.Split('_')[0], out var bid) ? bid : 0;

        return new MoMoResponse
        {
            IsSuccess = string.Equals(computedSig, receivedSig, StringComparison.OrdinalIgnoreCase)
                        && resultCode == "0",
            BookingId = bookingId,
            ResultCode = resultCode,
            TransactionId = transId
        };
    }

    private static string HmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}
