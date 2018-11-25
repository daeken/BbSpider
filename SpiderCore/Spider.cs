using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MoreLinq;
using PrettyPrinter;
using Requests;

namespace SpiderCore {
	public class Spider {
		public readonly Queue<Request> Queue = new Queue<Request>();
		public readonly HashSet<(string Url, ImmutableList<string> QueryParameters)> Seen =
			new HashSet<(string Url, ImmutableList<string> QueryParameters)>();
		public readonly List<Regex> Scope = new List<Regex>();
		public readonly List<Regex> Excluded = new List<Regex>();

		public event Action<Spider, string> Enqueued;
		public event Action<Spider, string> Requesting;
		public event Action<Spider, string, bool> Done;
		public event Action<Spider, string> NeedAuth;
		public event Action<Spider, Request, Response> Found;

		public readonly CookieJar Cookies = new CookieJar();

		public void Add(Request request) {
			if(!request.Url.StartsWith("http")) return;
			var domain = request.Url.Split("://", 2)[1].Split(':', 2)[0].Split('/', 2)[0];
			if(!Scope.Any(x => x.IsMatch(request.FullUrl))) return;
			if(Excluded.Any(x => x.IsMatch(request.FullUrl))) return;
			var key = (request.Url, request.Params.Select(x => x.Key).ToImmutableList());
			lock(this)
				if(!Seen.Contains(key)) {
					Queue.Enqueue(request);
					Seen.Add(key);
					Enqueued?.Invoke(this, request.FullUrl);
				}
		}

		public void Add(string url) =>
			Add(BuildRequest(url));

		public void AddScope(string scope) => AddScope(new Regex(scope));
		public void AddScope(Regex scope) => Scope.Add(scope);
		public void Exclude(Regex regex) => Excluded.Add(regex);

		Request BuildRequest(string url) {
			var seg = url.Split('#', 2)[0].Split('?', 2);
			var request = Request.Get(seg[0], Cookies);
			if(seg.Length == 2 && seg[1].Length != 0)
				foreach(var _kv in seg[1].Split('&')) {
					var kv = _kv.Split('=', 2);
					request = request.AddParam(Uri.UnescapeDataString(kv[0]), kv.Length == 1 ? "" : Uri.UnescapeDataString(kv[1]));
				}
			return request;
		}

		public void Go() {
			while(Queue.Count != 0) {
				var temp = new Request[Queue.Count];
				for(var i = 0; i < temp.Length; ++i)
					temp[i] = Queue.Dequeue();
				Parallel.ForEach(temp, request => {
					Requesting?.Invoke(this, request.FullUrl);
					var resp = request.Send();
					if(resp == null) {
						Done?.Invoke(this, request.FullUrl, false);
						return;
					}
					if(resp.StatusCode != 404)
						Found?.Invoke(this, request, resp);
					switch((resp.ContentType ?? "unknown").ToLower()) {
						case "text/html":
							ParseHtml(request.Url, resp.Text);
							break;
					}
					Done?.Invoke(this, request.FullUrl, true);
				});
			}
		}

		string MakeAbsolute(string cur, string next) {
			var nl = next.ToLower();
			if(nl.StartsWith("http://") || nl.StartsWith("https://") || nl.StartsWith("javascript:") || nl.StartsWith("tel:") || nl.StartsWith("whatsapp:")) return next;
			if(next.StartsWith("//")) return cur.Split("://", 2)[0] + ":" + next;
			if(next.StartsWith("/")) {
				var @base = cur.IndexOf('/', 8);
				if(@base == -1)
					return (cur.EndsWith("/") ? cur.Substring(0, cur.Length - 1) : cur) + next;
				return @base + cur.Substring(0, @base) + next;
			}
			return cur.Substring(0, cur.LastIndexOf('/') + 1) + next;
		}

		void ParseHtml(string url, string html) {
			void Parse(HtmlNode node) {
				switch(node.NodeType) {
					case HtmlNodeType.Element:
						switch(node.Name.ToLower()) {
							case "a" when node.Attributes.Contains("href"):
								Add(MakeAbsolute(url, node.Attributes["href"].Value));
								break;
							case "form" when node.Attributes.Contains("action"):
								Add(MakeAbsolute(url, node.Attributes["action"].Value));
								break;
						}
						
						break;
				}
				node.ChildNodes.ForEach(Parse);
			}
			
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			Parse(doc.DocumentNode);
		}
	}
}