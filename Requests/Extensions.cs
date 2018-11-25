using System.Threading.Tasks;

namespace Requests {
	public static class Extensions {
		public static T Complete<T>(this Task<T> task) {
			task.Wait();
			return task.Result;
		}
	}
}