using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;

namespace Hikaria.Core.WebAPI.Extensions
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder ConfigureNewtonsoftJson(this IMvcBuilder builder)
        {
            JsonConvert.DefaultSettings = new (() =>
            {
                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.None,
                    ContractResolver = new DefaultContractResolver()
                };

                settings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
                settings.DateFormatString = "yyyy-MM-dd HH:mm:ss"; 
                settings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
                settings.Converters.Add(new StringEnumConverter());
                settings.ContractResolver = new DefaultContractResolver();
                settings.NullValueHandling = NullValueHandling.Include;

                return settings;
            });
            builder.AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
                options.SerializerSettings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            return builder;
        }
    }
}
