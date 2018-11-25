using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using PrettyPrinter;

namespace Requests {
	public class Request {
		public string Method, Url;
		public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
		public readonly Dictionary<string, List<string>> Params = new Dictionary<string, List<string>>();
		public readonly Dictionary<string, List<string>> BodyParams = new Dictionary<string, List<string>>();
		public CookieJar CookieJar = new CookieJar();

		Response _Response;
		public Response Response => _Response ?? (_Response = Send());

		public string Text => Response.Text;

		public string FullUrl =>
			Params.Count != 0
				? Url + "?" + string.Join('&',
					  Params.Select(xs => string.Join('&',
						  xs.Value.Select(x => $"{Uri.EscapeUriString(xs.Key)}={Uri.EscapeUriString(x)}"))))
				: Url;

		public Request(string method, string url, CookieJar cookies=null) {
			Method = method;
			Url = url;
			if(cookies != null) CookieJar = cookies;
		}
		
		public static Request Get(string url, CookieJar cookies=null) => new Request("GET", url, cookies);
		public static Request Post(string url, CookieJar cookies=null) => new Request("POST", url, cookies);

		public Response Send() {
			var domain = Url.Split("://", 2)[1].Split(':', 2)[0].Split('/', 2)[0];
			var client = new HttpClient();
			var req = new HttpRequestMessage(new HttpMethod(Method), FullUrl);
			foreach(var (k, v) in Headers)
				req.Headers.Add(k, v);
			if(CookieJar != null)
				req.Headers.Add("Cookie", string.Join("; ", CookieJar.Get(domain).Select(x => $"{x.Key}={x.Value}")));
			try {
				return new Response(domain, client.SendAsync(req).Complete(), CookieJar);
			} catch(SocketException) {
				return null;
			}
		}

		public Request AddHeader(string key, string value) {
			Headers[key] = value;
			return this;
		}

		public Request AddParam(string key, string value) {
			if(!Params.ContainsKey(key)) Params[key] = new List<string>();
			Params[key].Add(value);
			return this;
		}

		public Request AddBodyParam(string key, string value) {
			if(!BodyParams.ContainsKey(key)) BodyParams[key] = new List<string>();
			BodyParams[key].Add(value);
			return this;
		}

		public Request AddCookieJar(CookieJar cookieJar) {
			CookieJar = cookieJar;
			return this;
		}
	}
}