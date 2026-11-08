using System.Text.Json.Serialization;
using Jinxxy.Models;

namespace Jinxxy;

/// <summary>Source-generated serializer metadata (trimming/AOT safe; required for iOS and WebAssembly).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(ApiError))]
[JsonSerializable(typeof(CurrentUser))]
[JsonSerializable(typeof(PagedResult<ProductSummary>))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(PagedResult<OrderSummary>))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(PagedResult<LicenseSummary>))]
[JsonSerializable(typeof(License))]
[JsonSerializable(typeof(LicenseActivationPage))]
[JsonSerializable(typeof(LicenseActivation))]
[JsonSerializable(typeof(CreateLicenseActivationRequest))]
[JsonSerializable(typeof(DeleteActivationResult))]
[JsonSerializable(typeof(PagedResult<Customer>))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(PagedResult<DiscountCode>))]
[JsonSerializable(typeof(DiscountCode))]
[JsonSerializable(typeof(CreateDiscountCodeRequest))]
[JsonSerializable(typeof(UpdateDiscountCodeRequest))]
[JsonSerializable(typeof(DeleteDiscountCodeResult))]
public sealed partial class JinxxyJsonContext : JsonSerializerContext;
