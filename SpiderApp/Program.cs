using SpiderCore;
using System;
using System.Text.RegularExpressions;
using PrettyPrinter;
using Requests;

namespace SpiderApp {
	class Program {
		static void Main(string[] args) {
			var spider = new Spider();
			spider.Found += (_, req, resp) => Console.WriteLine(req.FullUrl);
			spider.AddScope(@"github");
			spider.Add("https://github.com/");
			spider.Go();
		}
	}
}