using System.Collections.Generic;
using System.Linq;

namespace Requests {
	public class CookieJar {
		readonly Dictionary<string, Dictionary<string, string>> Domains =
			new Dictionary<string, Dictionary<string, string>>();

		public string this[string key] {
			get {
				lock(this)
					return Domains.Values.Where(x => x.ContainsKey(key)).Select(x => x[key]).FirstOrDefault();
			}
			set => Set("", key, value);
		}

		public CookieJar Set(string domain, string key, string value) {
			lock(this) {
				if(!Domains.ContainsKey(domain)) Domains[domain] = new Dictionary<string, string>();
				Domains[domain][key] = value;
				return this;
			}
		}

		public IEnumerable<(string Key, string Value)> Get(string domain) {
			var dlist = domain.Split('.');
			var seen = new HashSet<string>();
			for(var i = 0; i <= dlist.Length; ++i) {
				var sub = string.Join('.', dlist.Skip(i));
				lock(this)
					if(Domains.ContainsKey(sub))
						foreach(var (k, v) in Domains[sub])
							if(!seen.Contains(k)) {
								seen.Add(k);
								yield return (k, v);
							}
			}
		}
	}
}