using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using uhttpsharp;
using uhttpsharp.Handlers;

namespace receive_http_request_from_monkey_script
{
    class RestController : IRestController<string>
    {
        Action<String> changeURL;
        public RestController(Action<String> a)
        {
            changeURL = a;
        }

        public Task<string> Create(IHttpRequest request)
        {
            return Task.FromResult("");
        }

        public Task<IEnumerable<string>> Get(IHttpRequest request)
        {
            return Task.FromResult<IEnumerable<string>>(new[] { "" });
        }
        public Task<string> GetItem(IHttpRequest request)
        {
            return Task.FromResult("");
        }
        public Task<string> Upsert(IHttpRequest request)
        {
            string result = System.Text.Encoding.UTF8.GetString(request.Post.Raw);
            Console.WriteLine(result);

            changeURL.Invoke(result);

            return Task.FromResult(result);
        }
        public Task<string> Delete(IHttpRequest request)
        {
            return Task.FromResult("");
        }
    }
}