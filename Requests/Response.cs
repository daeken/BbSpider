using System.Net.Http;
using System.Text;
using MoreLinq.Extensions;
using PrettyPrinter;

namespace Requests {
	public class Response {
		public readonly int StatusCode;
		public readonly byte[] Data;
		public readonly string ContentType;
		public CookieJar CookieJar;

		public readonly Encoding Encoding = Encoding.UTF8;

		public string Text => Encoding.GetString(Data);
		
		public Response(string domain, HttpResponseMessage response, CookieJar cookieJar) {
			CookieJar = cookieJar;
			StatusCode = (int) response.StatusCode;
			Data = response.Content.ReadAsByteArrayAsync().Complete();
			response.Content.Headers.TryGetValues("Content-Type", out var ctval);
			if(ctval != null)
				foreach(var ct in ctval) {
					ContentType = ct.Split(';', 2)[0];
					if(ct.ToLower().Contains("charset="))
						switch(ct.Split("charset=", 2)[1].ToLower()) {
							case "utf-8": Encoding = Encoding.UTF8; break;
							case "iso-8859-1": Encoding = Encoding.ASCII; break;
						}
				}

			if(CookieJar != null) {
				response.Headers.TryGetValues("Set-Cookie", out var cval);
				if(cval != null)
					foreach(var c in cval) {
						var v = c.Split(';', 2)[0].Split('=', 2);
						CookieJar.Set(domain, v[0], v[1]);
					}
			}
		}
	}
}