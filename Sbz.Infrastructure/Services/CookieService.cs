using System;
using System.Collections.Generic;
using System.Text;
using Sbz.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Sbz.Infrastructure.Services
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Get(string key)
        {
            return _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(key, out string value) ? value : null;
        }
        public TValue Get<TValue>(string key) where TValue : class, new()
        {
            string value = Get(key);
            return !string.IsNullOrEmpty(value) ?
                JsonConvert.DeserializeObject<TValue>(value) : null;
        }
        public void Set(string key, string value, int expireTime)
        {
            CookieOptions option = new CookieOptions();
            option.SameSite = SameSiteMode.Strict;
            option.Secure = true;
            option.Expires = DateTime.Now.AddMinutes(expireTime);

            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, option);
        }
        public void Set(string key, object value, int expireTime, JsonSerializerSettings settings = null)
        {
            string valueSerialize;
            if (settings == null)
                valueSerialize = JsonConvert.SerializeObject(value, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                });
            else
                valueSerialize = JsonConvert.SerializeObject(value, settings);

            Set(key, valueSerialize, expireTime);
        }
        public void Remove(string key)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }




    }
}
